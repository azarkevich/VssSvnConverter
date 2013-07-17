using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using SourceSafeTypeLib;
using vsslib;

namespace VssSvnConverter
{
	class ImportListBuilder
	{
		const string DataFileName = "1-import-list.txt";
		const string DataFileRootTypes = "1-roots.txt";
		const string DataExtsFileName = "1-import-list-exts.txt";
		const string DataSizesFileName = "1-import-list-sizes.txt";
		const string LogFileName = "log-1-import-list.txt";

		public void Build(Options opts)
		{
			var sizes = new Dictionary<string, int>();

			_files = new List<string>();
			_isInclude = opts.IncludePredicate;

			using(_log = File.CreateText(LogFileName))
			using(var rootTypes = File.CreateText(DataFileRootTypes))
			{
				_log.AutoFlush = true;
				rootTypes.AutoFlush = true;

				foreach (var root in opts.Config["import-root"])
				{
					Console.WriteLine("VSS Root: {0}", root);

					var rootItem = opts.DB.VSSItem[root].Normalize(opts.DB);

					rootTypes.WriteLine("{0}	{1}", rootItem.Spec, rootItem.Type == 0 ? "d" : "f");

					WalkItem(rootItem, sizes);
				}

				File.WriteAllLines(DataFileName, _files.ToArray());
			}

			// build extensions map
			Console.WriteLine("Imported files extensions:");
			_files
				.Select(Path.GetExtension)
				.Select(e => e.ToLowerInvariant())
				.GroupBy(e => e)
				.ToList()
				.ForEach(g => Console.Write("{0}({1}) ", g.Key, g.Count()))
			;
			Console.WriteLine();
			Console.WriteLine();

			// dump extensions map
			using (var map = File.CreateText(DataExtsFileName))
			{
				_files
					.GroupBy(f => (Path.GetExtension(f) ?? "").ToLowerInvariant())
					.ToList()
					.ForEach(g => { 
						map.WriteLine("{0}({1}):", g.Key, g.Count());

						foreach(var f in g)
						{
							map.WriteLine("	{0}", f);
						}
					})
				;
			}

			// dump files by size
			using (var map = File.CreateText(DataSizesFileName))
			{
				sizes
					.Select(kvp => new { Spec = kvp.Key, Size = kvp.Value })
					.OrderByDescending(inf => inf.Size)
					.ToList()
					.ForEach(inf => map.WriteLine("{0,10:0.0} KiB	{1}", inf.Size / 1024.0, inf.Spec))
				;
			}

			Console.WriteLine("Building import files compete. Check: " + DataFileName);
		}

		public List<string> Load()
		{
			return File.ReadAllLines(DataFileName).ToList();
		}

		// spec -> isdir
		public Dictionary<string, bool> LoadRootTypes()
		{
			return File
				.ReadAllLines(DataFileRootTypes)
				.Select(l => l.Split('\t'))
				.ToDictionary(ar => ar[0], ar => ar[1] == "d")
			;
		}

		List<string> _files;
		Func<string, bool> _isInclude;

		StreamWriter _log;

		void WalkItem(IVSSItem item, Dictionary<string, int> sizes)
		{
			if(item.Type == 1)
			{
				sizes[item.Spec] = item.Size;
				_files.Add(item.Spec);
				_log.WriteLine("+{0}", item.Spec);
			}
			else
			{
				WalkItems(item.Items, sizes);
			}
		}

		void WalkItems(IVSSItems items, Dictionary<string, int> sizes)
		{
			foreach (IVSSItem item in items)
			{
				if(!_isInclude(item.Spec))
				{
					_log.WriteLine("-{0}", item.Spec);
					continue;
				}

				WalkItem(item, sizes);
			}
		}
	}
}
