using System;
using System.Collections.Generic;
using System.Linq;
using vcslib;

namespace vsslib
{
	public class VssFileCache : IDisposable
	{
		readonly FileCache _cache;

		readonly string _vssDbPath;

		public VssFileCache(string tempDir, string vssDbPath)
		{
			_vssDbPath = vssDbPath.Replace('/', '\\').Trim().TrimEnd('\\').ToLowerInvariant();
			_cache = new FileCache(tempDir);
		}

		public string GetFilePath(string spec, int ver)
		{
			return _cache.GetFilePath(MakeKey(spec, ver));
		}

		public string GetFilePath(string spec, int ver, long timeStamp)
		{
			return _cache.GetFilePath(MakeKey(spec, ver, timeStamp));
		}

		public string GetFileError(string spec, int ver)
		{
			return _cache.GetFileNotes(MakeKey(spec, ver));
		}

		public void AddFile(string spec, int ver, long timestamp, string path, bool copy)
		{
			_cache.AddFile(MakeKey(spec, ver, timestamp), path, copy);
		}

		public void AddError(string spec, int ver, string err)
		{
			_cache.AddNotes(MakeKey(spec, ver), err);
		}

		public FileCache.CacheEntry GetFileInfo(string spec, int ver)
		{
			return _cache.GetFileInfo(MakeKey(spec, ver));
		}

		public void DropAllErrors()
		{
			var forRemove = _cache
				.AllEntries()
				.Where(e => e.Notes != null && e.Notes != "not-retained")
				.ToList()
			;
			_cache.RemoveEntries(forRemove);
		}

		public void RemoveEntries(IEnumerable<FileCache.CacheEntry> entries)
		{
			_cache.RemoveEntries(entries);
		}

		string MakeKey(string spec, int ver)
		{
			spec = spec.Replace('\\', '/').Trim().TrimEnd('/').ToLowerInvariant();

			return _vssDbPath + "#" + spec + "@" + ver;
		}

		string MakeKey(string spec, int ver, long timeStamp)
		{
			spec = spec.Replace('\\', '/').Trim().TrimEnd('/').ToLowerInvariant();

			return _vssDbPath + "#" + spec + "@" + ver + "!" + timeStamp;
		}

		public void Dispose()
		{
			_cache.Dispose();
		}
	}
}
