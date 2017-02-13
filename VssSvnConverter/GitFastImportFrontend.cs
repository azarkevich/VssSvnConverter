using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.Mail;
using vsslib;
using VssSvnConverter.Core;

namespace VssSvnConverter
{
	// generate data file in format described in https://git-scm.com/docs/git-fast-import
	class GitFastImportFrontend
	{
		public const string DataFileName = "6-git-fast-import.dat";

		VssFileCache _cache;
		Options _opts;
		Stream _out;
		string _branchName;

		readonly Encoding _utf8 = new UTF8Encoding(false);
		readonly byte[] _lf = { (byte)'\n' };

		public void Create(Options opts, List<Commit> commits)
		{
			_opts = opts;

			if(opts.Config["unimportant-diff"].Any())
				throw new Exception("git-fast-import builder does not support 'unimportant-diff'");

			if(opts.Config["censore-group"].Any())
				throw new Exception("git-fast-import builder does not support 'censore-group'");

			// check all authors in map
			var badUsers = new HashSet<string>();
			for (var i = 0; i < commits.Count; i++)
			{
				if(commits[i].User.Contains("  "))
					throw new Exception("User name should not has 2 spaces in row");

				try
				{
					var ma = new MailAddress(commits[i].User);
					if (string.IsNullOrWhiteSpace(ma.DisplayName))
						badUsers.Add(commits[i].User);
					if (string.IsNullOrWhiteSpace(ma.Address))
						badUsers.Add(commits[i].User);
				}
				catch
				{
					badUsers.Add(commits[i].User);
				}
			}

			if (badUsers.Count > 0)
			{
				foreach (var au in badUsers)
				{
					Console.WriteLine(au);
				}
				throw new Exception("Some authors not mapped or has bad format. Should be 'Display Name <email@address.org>'");
			}

			// recreate file
			if (File.Exists(DataFileName))
				File.Delete(DataFileName);

			_branchName = "refs/heads/import-vss/" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

			using (_out = File.Create(DataFileName))
			using (_cache = new VssFileCache(opts.CacheDir, _opts.SourceSafeIni))
			{
				for (var i = 0; i < commits.Count; i++)
				{
					Console.WriteLine($"Commit {i}/{commits.Count} ...");
					WriteString($"progress {i}/{commits.Count}\n");
					WriteCommit(commits[i]);
				}
			}

			Console.WriteLine($"fast-import created in file {DataFileName}");
			Console.WriteLine("Import:");
			Console.WriteLine($"\tgit fast-import < \"{Path.GetFullPath(DataFileName)}\"");
			Console.WriteLine("Merge into current branch:");
			Console.WriteLine($"\tgit merge --allow-unrelated-histories {_branchName}");
		}

		void WriteCommit(Commit commit)
		{
			WriteString($"commit {_branchName}\n");
			WriteString($"author {commit.User} {Utils.GetUnixTimestamp(commit.At)} +0300\n");
			WriteString($"committer {commit.User} {Utils.GetUnixTimestamp(commit.At)} +0300\n");
			WriteStringData(commit.Comment);
			foreach (var file in commit.Files)
			{
				var filePath = _cache.GetFilePath(file.FileSpec, file.VssVersion);
				if (filePath == null)
				{
					throw new Exception($"File {file.FileSpec}@{file.VssVersion} absent in cache. Rerun 'VssSvnConverter build-cache'");
				}

				var relPath = file.FileSpec.TrimStart('$', '/', '\\');

				// mangle path
				if (_opts.MangleImportPath.Count > 0)
				{
					foreach (var manglePair in _opts.MangleImportPath)
					{
						relPath = manglePair.Item1.Replace(relPath, manglePair.Item2);
					}
				}

				WriteFileModification(relPath, filePath);
			}
			WriteLF();
		}

		void WriteStringData(string s)
		{
			var b = _utf8.GetBytes(s);
			WriteString($"data {b.Length}\n");
			_out.Write(b, 0, b.Length);
			WriteLF();
		}

		void WriteFileModification(string relPath, string filePath)
		{
			WriteString($"M 644 inline {relPath}\ndata {new FileInfo(filePath).Length}\n");
			using (var s = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Delete))
			{
				s.CopyTo(_out);
			}
			WriteLF();
		}

		void WriteString(string s)
		{
			var b = _utf8.GetBytes(s);
			_out.Write(b, 0, b.Length);
		}

		void WriteLF()
		{
			_out.Write(_lf, 0, 1);
		}
	}
}
