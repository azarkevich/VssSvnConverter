using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using VssSvnConverter.Core;
using System.Diagnostics;
using SourceSafeTypeLib;

namespace VssSvnConverter
{
	class CommitsBuilder
	{
		const string DataFileName = "5-commits-list.txt";

		Options _opts;
		HashSet<string> _notMappedAuthors = new HashSet<string>();

		public List<Commit> Load()
		{
			Commit commit = null;
			var commits = new List<Commit>();
			var commitRx = new Regex(@"^Commit:(?<at>[0-9]+)\t\tUser:(?<user>.+)\t\tComment:(?<comment>.*)$");
			using(var r = File.OpenText(DataFileName))
			{
				string line;
				while((line = r.ReadLine())!=null)
				{
					if(line.StartsWith("	"))
					{
						var arr = line.Substring(1).Split(':');

						Debug.Assert(arr.Length == 3);
						Debug.Assert(commit != null);

						commit.AddRevision(new FileRevisionLite {
							FileSpec = arr[2],
							VssVersion = Int32.Parse(arr[0]),
							At = new DateTime(Int64.Parse(arr[1]), DateTimeKind.Utc)
						});
					}
					else
					{
						var m = commitRx.Match(line);
						if(m.Success)
						{
							commit = new Commit {
								At = new DateTime(long.Parse(m.Groups["at"].Value), DateTimeKind.Utc),
								User = m.Groups["user"].Value,
								Comment = DeserializeMultilineText(m.Groups["comment"].Value)
							};
							commits.Add(commit);
						}
					}
				}
			}
			return commits;
		}

		public void Build(Options opts, List<FileRevision> versions)
		{
			_opts = opts;

			_notMappedAuthors.Clear();

			if (File.Exists(DataFileName))
				File.Delete(DataFileName);

			if (opts.UnimportantCheckinCommentRx.Length > 0)
			{
				// find unimportant revisons
				var unimportant = versions
					.Where(r => opts.UnimportantCheckinCommentRx.Any(rx => rx.IsMatch(r.Comment)))
					.ToList()
				;

				// if unimportant revision is most recent - keep it
				var keep = new List<FileRevision>();
				foreach (var g in unimportant.ToList().GroupBy(r => r.FileSpec))
				{
					var maxRev = versions.Where(r => r.FileSpec == g.Key).Max(r => r.VssVersion);

					// try find unimportant with most recent revision. this revision should be kept
					var keepIt = g.FirstOrDefault(r => r.VssVersion == maxRev);
					if (keepIt != null)
						keep.Add(keepIt);
				}

				keep.ForEach(r => unimportant.Remove(r));

				// remove unimportant
				foreach (var r in unimportant)
				{
					versions.Remove(r);
				}
			}

			// perform mapping vss user -> author
			MapAuthors(versions);

			var orderedRevisions = versions
				.OrderBy(r => r.At)
				.ThenBy(r => r.VssVersion)
				.ToList()
			;

			var commits = SliceToCommits(orderedRevisions).ToList();

			// save
			using(var wr = File.CreateText(DataFileName))
			{
				foreach (var c in commits)
				{
					wr.WriteLine("Commit:{0}		User:{1}		Comment:{2}", c.At.Ticks, c.User, SerializeMultilineText(c.Comment));
					c.Files.ToList().ForEach(f => {
						Debug.Assert(f.At.Kind == DateTimeKind.Utc);
						wr.WriteLine("	{0}:{1}:{2}", f.VssVersion, f.At.Ticks, f.FileSpec);
					});
				}
			}

			Console.WriteLine("{0} commits produced.", commits.Count);
			Console.WriteLine("Build commits list complete. Check " + DataFileName);

			if(_notMappedAuthors.Count > 0)
			{
				Console.WriteLine("Not mapped users:");
				_notMappedAuthors.ToList().ForEach(u => Console.WriteLine($"{u} = ?"));

				if(_opts.UserMappingStrict)
				{
					throw new ApplicationException("Stop execution.");
				}
			}
		}

		void MapAuthors(IEnumerable<FileRevision> revs)
		{
			var mapping = LoadUserMappings("authors") ?? new Dictionary<string, string>();

			foreach (var rev in revs)
			{
				if (mapping.TryGetValue(rev.User.ToLowerInvariant(), out string author))
				{
					rev.OriginalUser = rev.User;
					rev.User = author;
				}
				else
				{
					_notMappedAuthors.Add(rev.User.ToLowerInvariant());
				}
			}
		}

		Dictionary<string, string> LoadUserMappings(string configKey)
		{
			Dictionary<string, string> mapping = null;

			foreach (var mappingFile in _opts.Config[configKey])
			{
				mapping = mapping ?? new Dictionary<string, string>();
				foreach (var line in File.ReadAllLines(mappingFile).Where(l => !string.IsNullOrWhiteSpace(l)))
				{
					var ind = line.IndexOf('=');

					if (ind == -1)
						throw new Exception("Invalid user mapping file: " + mappingFile);

					var from = line.Substring(0, ind).Trim().ToLowerInvariant();
					var to = line.Substring(ind + 1).Trim();

					if (mapping.ContainsKey(@from))
						throw new Exception("Invalid user mapping file: " + mappingFile + "; Duplicate entry: " + @from);

					mapping[@from] = to;
				}
			}
			return mapping;
		}

		IEnumerable<Commit> SliceToCommits(List<FileRevision> revs)
		{
			var oldUsers = new HashSet<string>();

			foreach (var file in _opts.Config["old-authors"])
			{
				File.ReadAllLines(file)
					.Where(l => !string.IsNullOrWhiteSpace(l))
					.ToList()
					.ForEach(l => oldUsers.Add(l.ToLowerInvariant()))
				;
			}

			var current = new List<FileRevision>();

			for (var i = 0; ; i++)
			{
				current.Clear();
				for (; i < revs.Count; i++)
				{
					var rev = revs[i];

					// first revison always can be added
					if (current.Count == 0)
					{
						current.Add(rev);
						continue;
					}

					// chnages too far in time
					if (rev.At - current.Last().At > _opts.SilencioSpan)
					{
						i--;
						break;
					}

					// if author changed and one of authors not 'old' - stop current commit
					if(current.Last().User != rev.User)
					{
						if(!oldUsers.Contains(rev.User) || !oldUsers.Contains(current.Last().User))
						{
							i--;
							break;
						}
					}

					// if merge not allowed - check file already in one of commit
					if (!_opts.MergeSameFileChanges)
					{
						if (current.Any(r => StringComparer.OrdinalIgnoreCase.Compare(r.FileSpec, rev.FileSpec) == 0))
						{
							i--;
							break;
						}
					}

					current.Add(rev);
				}

				if (current.Count == 0)
				{
					Trace.Assert(i >= revs.Count);
					break;
				}

				// build commit
				var c = new Commit { At = current.First().At };

				// add revisions
				current.ForEach(r => c.AddRevision(new FileRevisionLite { At = r.At, FileSpec = r.FileSpec, VssVersion = r.VssVersion }));

				// calculate author
				var allAuthors = current.Select(r => r.User).Distinct().ToList();
				if(allAuthors.Count == 1)
				{
					c.User = allAuthors[0];

					BuildCommitComment(
						c,
						current.Select(r => r.Comment).Where(cc => !string.IsNullOrWhiteSpace(cc)).Distinct()
					);
				}
				else
				{
					if (_opts.CombinedAuthor == null)
						throw new Exception("'combined-author' config paramter not specified.");

					c.User = _opts.CombinedAuthor;

					BuildCombinedCommitComment(
						c,
						current
					);
				}

				yield return c;
			}
		}

		void BuildCombinedCommitComment(Commit cmt, IEnumerable<FileRevision> revs)
		{
			var sb = new StringBuilder();

			foreach (var g in revs.GroupBy(r => r.OriginalUser))
			{
				if (sb.Length > 0)
					sb.AppendLine("===");

				sb.AppendLine($"@{g.Key}:");
				foreach (var rev in g)
				{
					sb.AppendLine($"{rev.At}  {rev.FileSpec}@{rev.VssVersion}");
					if (!string.IsNullOrWhiteSpace(rev.Comment))
						sb.AppendLine($"\t{rev.Comment}");
				}
			}

			cmt.Comment = sb.ToString();
		}

		void BuildCommitComment(Commit cmt, IEnumerable<string> comments)
		{
			var sb = new StringBuilder();

			if(comments != null)
			{
				foreach (var c in comments)
				{
					if (sb.Length > 0)
						sb.AppendLine("---");
					sb.AppendLine(c);
				}
			}

			if (_opts.CommentAddVssFilesInfo && cmt.Files.Count() > 1)
			{
				if (sb.Length > 0)
					sb.AppendLine("===");
				sb.AppendFormat("@Files:");
				foreach (var file in cmt.Files)
					sb.AppendFormat("\n\t{0}  {1}@{2}", file.At, file.FileSpec, file.VssVersion);
			}

			if(_opts.CommentAddUserTime)
			{
				var commitInfo = string.Format("{{{1} by {0}}}", cmt.User, cmt.At.ToString("yyyy-MMM-dd HH:ss:mm", CultureInfo.InvariantCulture));

				if (sb.Length > 0)
				{
					if (sb.ToString().Trim().IndexOf('\n') != -1)
						sb.Insert(0, "\n");
					else
						sb.Insert(0, " ");

					sb.Insert(0, ":");
				}

				sb.Insert(0, commitInfo);
			}

			cmt.Comment = sb.ToString().Trim();
		}

		static string SerializeMultilineText(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				return string.Empty;

			return text.Replace('\n', '\x01').Replace("\r", "");
		}

		static string DeserializeMultilineText(string line)
		{
			return line.Replace('\x01', '\n');
		}
	}
}
