using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace vcslib
{
	// hold references
	// string (aka token) -> list of strings (aka values)
	public class XRefMap
	{
		public Dictionary<string, List<string>> Map { get; protected set; }

		readonly List<string> _empty = new List<string>();

		public XRefMap()
		{
			Map = new Dictionary<string, List<string>>();
		}

		public void AddRef(string key, string value)
		{
			List<string> list;
			if(!Map.TryGetValue(key, out list))
			{
				list = new List<string>();
				Map[key] = list;
			}
			list.Add(value);
		}

		public List<string> GetRefs(string key, bool nullIfEmpty = false)
		{
			List<string> list;
			if(!Map.TryGetValue(key, out list))
				return nullIfEmpty ? null : _empty;

			return list;
		}

		public XRefMap Inverse()
		{
			var inversed = new XRefMap();
			foreach (var kvp in Map)
			{
				foreach (var xref in kvp.Value)
				{
					inversed.AddRef(xref, kvp.Key);
				}
			}
			return inversed;
		}

		public void Save(string file, bool wrapLines = false)
		{
			using (var sw = File.CreateText(file))
			{
				foreach (var kvp in Map)
				{
					sw.Write(kvp.Key);
					if(wrapLines)
						sw.WriteLine();
					
					foreach (var xref in kvp.Value)
					{
						sw.Write("\t{0}", xref);
						if(wrapLines)
							sw.WriteLine();
					}

					if(!wrapLines)
						sw.WriteLine();
				}
			}
		}
		
		public void SaveTokenFile(string file)
		{
			using (var sw = File.CreateText(file))
			{
				foreach (var kvp in Map)
				{
					foreach (var xref in kvp.Value)
					{
						sw.WriteLine("{0}\t{1}", kvp.Key, xref);
					}
				}
			}
		}

		public void Load(string file)
		{
			Map = new Dictionary<string, List<string>>();

			string key = null;
			foreach(var line in File.ReadLines(file).Select(l => l.TrimEnd()).Where(l => !string.IsNullOrWhiteSpace(l)))
			{
				if(line.StartsWith("\t"))
				{
					if(key == null)
						throw new ApplicationException("Incorrect map storage file");

					AddRef(key, line.Substring(1));
				}
				else
				{
					var ar = line.Split('\t');
					key = ar[0];

					for (var i = 1; i < ar.Length; i++)
					{
						AddRef(key, line);
					}
				}
			}
		}

		public void LoadTokenFile(string file)
		{
			Map = new Dictionary<string, List<string>>();

			foreach(var line in File.ReadLines(file).Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)))
			{
				var ar = line.Split('\t');
				
				if(ar.Length != 2)
					throw new ApplicationException("Incorrect token map storage file");

				AddRef(ar[0], ar[1]);
			}
		}

		public void LoadTokenFile(StreamReader sr)
		{
			Map = new Dictionary<string, List<string>>();

			string line;
			while ((line = sr.ReadLine()) != null)
			{
				line = line.Trim();

				if (string.IsNullOrWhiteSpace(line) || line.IndexOf('#') == 0)
					continue;

				var ar = line.Split('\t');

				if (ar.Length != 2)
					throw new ApplicationException("Incorrect token map storage file");

				AddRef(ar[0], ar[1]);
			}
		}
	}
}
