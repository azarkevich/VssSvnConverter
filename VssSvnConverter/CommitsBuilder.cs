using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

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
			var commitRx = new Regex(@"^Commit:(?<at>[0-9]+)\s+User:(?<user>[^\s]+)\s+Comment:(?<comment>.*)$");
			using(var r = File.OpenText(DataFileName))
			{
				string line;
				while((line = r.ReadLine())!=null)
				{
					if(line.StartsWith("	"))
					{
						line = line.Substring(1);
						var pos = line.IndexOf(':');
						if(pos != -1)
						{
							commit.AddRevision(new FileRevisionLite {
								FileSpec = line.Substring(pos + 1),
								VssVersion = Int32.Parse(line.Substring(0, pos))
							}, "");
						}
					}
					else
					{
						var m = commitRx.Match(line);
						if(m.Success)
						{
							commit = new Commit {
								At = new DateTime(long.Parse(m.Groups["at"].Value), DateTimeKind.Utc),
								User = m.Groups["user"].Value,
								SerialziedComments = m.Groups["comment"].Value
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

			var orderedRevisions = versions
				.OrderBy(r => r.At)
				.ThenBy(r => r.VssVersion)
			;

			var commits = SliceToCommits(orderedRevisions).ToList();

			// save
			using(var wr = File.CreateText(DataFileName))
			{
				foreach (var c in commits)
				{
					wr.WriteLine("Commit:{0} User:{1} Comment:{2}", c.At.Ticks, c.User, c.SerialziedComments);
					c.Files.ToList().ForEach(f => wr.WriteLine("	{0}:{1}", f.VssVersion, f.FileSpec));
				}
			}

			Console.WriteLine("{0} commits produced.", commits.Count);
			Console.WriteLine("Build commits list complete. Check 3-commits-list.txt");
		}

		IEnumerable<Commit> SliceToCommits(IEnumerable<FileRevision> revs)
		{
			var commits = new Dictionary<string, Commit>();

			foreach (var rev in revs)
			{
				// commits flushing
				var forRemove = new List<string>();
				foreach (var kvp in commits)
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
				forRemove.ForEach(k => commits.Remove(k));

				// get/create current commit
				Commit cmt;
				if(!commits.TryGetValue(rev.User, out cmt))
				{
					cmt = new Commit {
						At = rev.At,
						User = rev.User
					};
					commits.Add(rev.User, cmt);

					yield return cmt;
				}

				// add file revision
				cmt.AddRevision(new FileRevisionLite { FileSpec = rev.FileSpec, VssVersion = rev.VssVersion, At = rev.At }, rev.Comment);
			}
		}
	}
}
