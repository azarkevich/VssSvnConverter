using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace vcslib
{
	public class FileCache
	{
		public class CacheEntry
		{
			public readonly string Key;

			// Some notes
			public readonly string Notes;

			// path to content
			public readonly string ContentPath;

			// Sha1 hash
			public readonly string Sha1Hash;

			public CacheEntry(string key, string content, string hash, string notes)
			{
				Key = key;
				ContentPath = content;
				Sha1Hash = hash;
				Notes = notes;
			}
		}

		static readonly CacheEntry EmptyCacheEntry = new CacheEntry(null, null, null, null);

		readonly FileStream _indexStream;
		readonly StreamWriter _indexWriter;
		readonly Dictionary<string, CacheEntry> _cacheIndex = new Dictionary<string, CacheEntry>();
		int _indexEntriesCount;
		readonly string _cacheBaseDir;

		public bool AutoCalcHash = true;

		public FileCache(string baseDir)
		{
			_cacheBaseDir = baseDir;

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
				if (arr.Length != 4)
				{
					Console.Error.WriteLine("Bad cache line: " + line);
					Environment.Exit(-1);
				}

				for (var i = 0; i < arr.Length; i++)
				{
					if(arr[i] == "")
						arr[i] = null;
				}

				var ce = new CacheEntry(arr[0], arr[1], arr[2], arr[3]);

				_cacheIndex[ce.Key] = ce;
				
				_indexEntriesCount++;
			}
		}

		SHA1Managed _hashAlgo;

		void WriteIndexEntry(CacheEntry ce)
		{
			_indexWriter.WriteLine("{0}	{1}	{2}	{3}", ce.Key, ce.ContentPath, ce.Sha1Hash, ce.Notes);
			_indexEntriesCount++;
		}

		public void AddNotes(string key, string notes)
		{
			lock(this)
			{
				var ce = new CacheEntry(key, null, null, notes);

				_cacheIndex[ce.Key] = ce;

				WriteIndexEntry(ce);
			}
		}

		public void AddFile(string key, string path, bool copy, string notes = null, string hash = null)
		{
			lock(this)
			{
				// AutoCalcHash hash if need
				if(hash == null && AutoCalcHash && path != null)
				{
					if(_hashAlgo == null)
						_hashAlgo = new SHA1Managed();

					using (var s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
					{
						hash = Convert.ToBase64String(_hashAlgo.ComputeHash(s));
					}
				}

				string oldDataPath = null;

				CacheEntry ce;
				if(_cacheIndex.TryGetValue(key, out ce))
				{
					// remove after adding new entry for same key
					oldDataPath = ce.ContentPath;
				}
			
				var store = Path.Combine(_cacheBaseDir, (_indexEntriesCount / 1000).ToString(CultureInfo.InvariantCulture));

				if(!Directory.Exists(store))
					Directory.CreateDirectory(store);

				var filePath = Path.Combine(store, string.Format("{0}-{1}", _indexEntriesCount, Path.GetFileName(path)));

				ce = new CacheEntry(key, filePath, hash, notes);

				if(copy)
					File.Copy(path, filePath);
				else
					File.Move(path, filePath);

				_cacheIndex[ce.Key] = ce;

				WriteIndexEntry(ce);

				if(oldDataPath != null && File.Exists(oldDataPath))
					File.Delete(oldDataPath);
			}
		}

		public CacheEntry GetFileInfo(string key)
		{
			lock(this)
			{
				CacheEntry ret;

				_cacheIndex.TryGetValue(key, out ret);

				return ret;
			}
		}

		public string GetFilePath(string key)
		{
			return (GetFileInfo(key) ?? EmptyCacheEntry).ContentPath;
		}

		public string GetFileNotes(string key)
		{
			return (GetFileInfo(key) ?? EmptyCacheEntry).Notes;
		}

		public string GetHash(string key)
		{
			return (GetFileInfo(key) ?? EmptyCacheEntry).Sha1Hash;
		}

		public void Dispose()
		{
			_indexStream.Close();
		}
	}
}
