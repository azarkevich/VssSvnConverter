using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace VssSvnConverter
{
	public class FileCache : IDisposable
	{
		public class CacheEntryData
		{
			// specify if cahce entry contains file path or error
			public bool Errorneous;
			// path to content or error string
			public string Content;
		}

		class CacheEntry
		{
			// cache entry key
			public readonly string Key;

			public readonly string NormVssDbPath;
			public readonly string NormFileSpec;
			public readonly int FileVersion;
			public CacheEntryData Data;

			public CacheEntry(string vssDb, string fileSpec, int ver, CacheEntryData content)
			{
				NormVssDbPath = vssDb.Replace('/', '\\').Trim().TrimEnd('\\').ToLowerInvariant();
				NormFileSpec = fileSpec.Replace('\\', '/').Trim().TrimEnd('/').ToLowerInvariant();
				FileVersion = ver;
				Data = content;

				Key = NormVssDbPath + "#" + NormFileSpec + "@" + FileVersion;
			}
		}

		readonly FileStream _indexStream;
		readonly StreamWriter _indexWriter;
		readonly Dictionary<string, CacheEntry> _cacheIndex = new Dictionary<string, CacheEntry>();
		readonly string _cacheBaseDir;
		readonly string _vssIdentity;

		public FileCache(string vssIdentity, string baseDir)
		{
			_cacheBaseDir = baseDir;

			_vssIdentity = vssIdentity;

			if(!Directory.Exists(baseDir))
				Directory.CreateDirectory(baseDir);

			var indexFile = Path.Combine(baseDir, ".index");
			_indexStream = new FileStream(indexFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

			ReadIndex();

			_indexWriter = new StreamWriter(_indexStream, Encoding.UTF8) { AutoFlush = true };
		}

		void ReadIndex()
		{
			var reader = new StreamReader(_indexStream, Encoding.UTF8);
			string line;
			while ((line = reader.ReadLine()) != null)
			{
				var arr = line.Split('\t');
				if (arr.Length != 5)
				{
					Console.Error.WriteLine("Bad cache line: " + line);
					Environment.Exit(-1);
				}

				var data = new CacheEntryData { Errorneous = Boolean.Parse(arr[3]), Content = arr[4] };

				var ce = new CacheEntry(arr[0], arr[1], Int32.Parse(arr[2]), data);

				_cacheIndex[ce.Key] = ce;
			}
		}

		public bool AddError(string fileSpec, int ver, string errorMessage)
		{
			lock(this)
			{
				var ce = new CacheEntry(_vssIdentity, fileSpec, ver, null);
				if(_cacheIndex.ContainsKey(ce.Key))
					return false;

				ce.Data = new CacheEntryData { Errorneous = true, Content = errorMessage };

				_cacheIndex[ce.Key] = ce;

				WriteEntry(ce);

				return true;
			}
		}

		void WriteEntry(CacheEntry ce)
		{
			_indexWriter.WriteLine("{0}	{1}	{2}	{3}	{4}", ce.NormVssDbPath, ce.NormFileSpec, ce.FileVersion, ce.Data.Errorneous, ce.Data.Content);
		}

		public bool AddFile(string fileSpec, int ver, string path, bool copy)
		{
			lock(this)
			{
				var ce = new CacheEntry(_vssIdentity, fileSpec, ver, null);
				CacheEntry existing;
				if(_cacheIndex.TryGetValue(ce.Key, out existing))
				{
					if(!existing.Data.Errorneous)
						return false;

					// remvoe errorneous and place file
					_cacheIndex.Remove(ce.Key);
				}

				var indexNumber = _cacheIndex.Count;

				var store = Path.Combine(_cacheBaseDir, (indexNumber / 1000).ToString(CultureInfo.InvariantCulture));

				if(!Directory.Exists(store))
					Directory.CreateDirectory(store);

				var filePath = Path.Combine(store, string.Format("{0:X8}-{1}@{2}", indexNumber, Path.GetFileName(fileSpec), ver));
				ce.Data = new CacheEntryData { Content = filePath };

				if(File.Exists(ce.Data.Content))
					File.Delete(ce.Data.Content);

				if(copy)
					File.Copy(path, ce.Data.Content);
				else
					File.Move(path, ce.Data.Content);

				_cacheIndex[ce.Key] = ce;

				WriteEntry(ce);

				return true;
			}
		}

		public CacheEntryData GetFileInfo(string fileSpec, int ver)
		{
			lock(this)
			{
				CacheEntry ret;
				if(_cacheIndex.TryGetValue(new CacheEntry(_vssIdentity, fileSpec, ver, null).Key, out ret))
					return ret.Data;

				return null;
			}
		}

		public string GetFilePath(string fileSpec, int ver)
		{
			var c = GetFileInfo(fileSpec, ver);
			if(c == null || c.Errorneous)
				return null;

			return c.Content;
		}

		public string GetFileError(string fileSpec, int ver)
		{
			var c = GetFileInfo(fileSpec, ver);
			if(c == null || !c.Errorneous)
				return null;

			return c.Content;
		}

		public void Dispose()
		{
			_indexStream.Close();
		}
	}
}
