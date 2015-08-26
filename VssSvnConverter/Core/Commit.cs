using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VssSvnConverter.Core
{
	class Commit
	{
		public DateTime At;
		public DateTime LastChangeAt;
		public string User;
	
		// File revisions
		public void AddRevision(FileRevisionLite rev, string comment)
		{
			FileRevisionLite existing;
			if(!_filesMap.TryGetValue(rev.FileSpec, out existing) || existing.VssVersion < rev.VssVersion)
			{
				// add or update
				_filesMap[rev.FileSpec] = rev;
				
				LastChangeAt = rev.At;
			}

			comment = (comment ?? "").Trim();
			if (comment != "" && !_comments.Contains(comment))
				_comments.Add(comment);
		}

		public bool ContainsFile(string fileSpec)
		{
			return _filesMap.ContainsKey(fileSpec);
		}

		public IEnumerable<FileRevisionLite> Files
		{
			get
			{
				return _filesMap.Values;
			}
		}

		readonly Dictionary<string, FileRevisionLite> _filesMap = new Dictionary<string, FileRevisionLite>();

		// comments
		public string SerialziedComments
		{
			get
			{
				return Comment.Replace('\n', '\x01').Replace("\r", "");
			}
			set
			{
				_comments.Clear();
				Comment = value.Replace('\x01', '\n');
			}
		}

		public string Comment
		{
			get
			{
				if(_comment != null)
					return _comment;

				var sb = new StringBuilder();

				foreach (var c in _comments)
				{
					if(sb.Length > 0)
						sb.AppendLine("---");
					sb.AppendLine(c);
				}

				if (Files.Count() > 1)
				{
					if (sb.Length > 0)
						sb.AppendLine("===");
					sb.AppendFormat("@Files:\n");
					foreach (var file in Files)
					{
						sb.AppendFormat("\t{0}  {1}@{2}\n", file.At, file.FileSpec, file.VssVersion);
					}
				}

				return sb.ToString().Trim();
			}
			set
			{
				_comment = value;
			}
		}
		string _comment;

		public IEnumerable<string> Comments
		{
			get
			{
				return _comments;
			}
		}
		readonly List<string> _comments = new List<string>();
	}
}