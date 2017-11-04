using System;
using System.Collections.Generic;

namespace VssSvnConverter.Core
{
	class Commit
	{
		readonly Dictionary<string, FileRevisionLite> _filesMap = new Dictionary<string, FileRevisionLite>();

		public DateTime At;
		public string Author;
		public string Comment;

		public IEnumerable<FileRevisionLite> Files => _filesMap.Values;

		public void AddRevision(FileRevisionLite rev)
		{
			FileRevisionLite existing;
			if (!_filesMap.TryGetValue(rev.FileSpec, out existing) || existing.VssVersion < rev.VssVersion)
			{
				// add or update
				_filesMap[rev.FileSpec] = rev;
			}
		}
	}
}