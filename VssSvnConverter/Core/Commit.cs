using System;
using System.Collections.Generic;

namespace VssSvnConverter.Core
{
	class Commit
	{
		public DateTime At;
		public DateTime LastChangeAt;
		public string User;
	
		// File revisions
		public void AddRevision(FileRevisionLite rev)
		{
			FileRevisionLite existing;
			if (!_filesMap.TryGetValue(rev.FileSpec, out existing) || existing.VssVersion < rev.VssVersion)
			{
				// add or update
				_filesMap[rev.FileSpec] = rev;

				LastChangeAt = rev.At;
			}
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

		public string Comment;
	}
}