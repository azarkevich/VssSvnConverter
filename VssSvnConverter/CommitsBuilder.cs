using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using VssSvnConverter.Core;
using System.Diagnostics;

namespace VssSvnConverter
{
	class CommitsBuilder
	{
		const string DataFileName = "5-commits-list.txt";

		Options _opts;

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

			if(File.Exists(DataFileName))
				File.Delete(DataFileName);

			var orderedRevisions = versions
				.OrderBy(r => r.At)
				.ThenBy(r => r.VssVersion)
			;

			// perform mapping vss user -> author
			MapUsers(orderedRevisions);

			var commits = SliceToCommits(orderedRevisions);

			// perfrom author -> commiter mapping
			MapUsers(commits);

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
		}

		void MapUsers(IEnumerable<Commit> commits)
		{
			var mapping = LoadUserMappings("user-mapping-author2commiter");

			if (mapping == null)
				return;

			var notMapped = new HashSet<string>();
			var notMappedL = new List<string>();
			foreach (var c in commits)
			{
				string commiter;
				if (!mapping.TryGetValue(c.User.ToLowerInvariant(), out commiter) &&
					!mapping.TryGetValue("*", out commiter))
				{
					if (_opts.UserMappingStrict && !notMapped.Contains(c.User.ToLowerInvariant()))
					{
						notMapped.Add(c.User.ToLowerInvariant());
						notMappedL.Add(c.User);
					}
				}
				else if (commiter != "*")
					c.User = commiter;
			}

			if (_opts.UserMappingStrict && notMapped.Count > 0)
			{
				Console.WriteLine("Insufficiend mapping for author -> commiter:");
				foreach (var user in notMappedL)
					Console.WriteLine("{0} = ?", user);
				throw new ApplicationException("Stop execution.");
			}
		}

		void MapUsers(IEnumerable<FileRevision> orderedRevisions)
		{
			var mapping = LoadUserMappings("user-mapping-vss2author");

			if (mapping == null)
				return;

			var notMapped = new HashSet<string>();
			var notMappedL = new List<string>();
			foreach (var rev in orderedRevisions)
			{
				string author;
				if (!mapping.TryGetValue(rev.User.ToLowerInvariant(), out author) &&
					!mapping.TryGetValue("*", out author))
				{
					if (_opts.UserMappingStrict && !notMapped.Contains(rev.User.ToLowerInvariant()))
					{
						notMapped.Add(rev.User.ToLowerInvariant());
						notMappedL.Add(rev.User);
					}
				}
				else if (author != "*")
					rev.User = author;
			}

			if (_opts.UserMappingStrict && notMapped.Count > 0)
			{
				Console.WriteLine("Insufficiend mapping for vss author -> author:");
				foreach (var user in notMappedL)
					Console.WriteLine("{0} = ?", user);
				throw new ApplicationException("Stop execution.");
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

					mapping[@from] = to;
				}
			}
			return mapping;
		}

		List<Commit> SliceToCommits(IEnumerable<FileRevision> revs)
		{
			var currentUserCommits = new Dictionary<string, Commit>();

			var returnCommits = new List<Commit>();
			var commitComments = new Dictionary<Commit, List<string>>();

			foreach (var rev in revs)
			{
				// commits flushing
				var forRemove = new List<string>();
				foreach (var kvp in currentUserCommits)
				{
					var commit = kvp.Value;

					// out of silence period ?
					if((rev.At - commit.LastChangeAt) > _opts.SilentSpan)
					{
						forRemove.Add(kvp.Key);
						continue;
					}

					// file already in one of commit
					if(commit.ContainsFile(rev.FileSpec))
					{
						// flush commit if merge changes disallowd or if file participate in other user commit
						if(!_opts.MergeChanges || commit.User != rev.User)
						{
							forRemove.Add(kvp.Key);
							continue;
						}
					}

					// if overlapping not allowed - flush all users, except current revision user
					if(!_opts.OverlapCommits && commit.User != rev.User)
					{
						forRemove.Add(kvp.Key);
						continue;
					}
				}
				forRemove.ForEach(k => currentUserCommits.Remove(k));

				// get/create current commit
				Commit cmt;
				if(!currentUserCommits.TryGetValue(rev.User, out cmt))
				{
					cmt = new Commit {
						At = rev.At,
						User = rev.User
					};
					currentUserCommits.Add(rev.User, cmt);

					returnCommits.Add(cmt);
				}

				// add file revision
				cmt.AddRevision(new FileRevisionLite { FileSpec = rev.FileSpec, VssVersion = rev.VssVersion, At = rev.At });

				if(!string.IsNullOrWhiteSpace(rev.Comment))
				{
					List<string> comments;
					if (!commitComments.TryGetValue(cmt, out comments))
					{
						commitComments[cmt] = comments = new List<string>();
					}

					var comment = rev.Comment.Trim();
					if (!comments.Contains(comment))
						comments.Add(comment);
				}
			}

			// build commit comments
			for (int i = 0; i < returnCommits.Count; i++)
			{
				var cmt = returnCommits[i];
				List<string> comments;
				commitComments.TryGetValue(cmt, out comments);
				BuildCommitComment(i, cmt, comments);
			}

			return returnCommits;
		}

		void BuildCommitComment(int commitIndex, Commit cmt, IEnumerable<string> comments)
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
				var commitInfo = string.Format("{{{0} at {1}}}", cmt.User, cmt.At.ToString("g"));

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

			if(_opts.CommentAddCommitNumber)
			{
				sb.Insert(0, string.Format("Commit#{0}\n", commitIndex + 1));
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
