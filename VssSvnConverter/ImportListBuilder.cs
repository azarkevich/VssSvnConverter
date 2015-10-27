using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using SourceSafeTypeLib;
using vsslib;
using VssSvnConverter.Core;

namespace VssSvnConverter
{
	class ImportListBuilder
	{
		const string AllFilesList = "1-list.txt";
		const string ExcludedFilesList = "1-not-import-list.txt";
		const string FilesList = "1-import-list.txt";

		const string DataFileRootTypes = "1-roots.txt";
		const string DataExtsFileName = "1-import-list-exts.txt";
		const string DataSizesFileName = "1-import-list-sizes.txt";

		List<Tuple<string, int>> _files;

		public void Build(Options opts)
		{
			_files = new List<Tuple<string, int>>();

			using(var rootTypes = File.CreateText(DataFileRootTypes))
			{
				rootTypes.AutoFlush = true;

				foreach (var root in opts.Config["import-root"])
				{
					Console.WriteLine("VSS Root: {0}", root);

					var rootItem = opts.DB.Value.VSSItem[root].Normalize(opts.DB.Value);

					rootTypes.WriteLine("{0}	{1}", rootItem.Spec, rootItem.Type == 0 ? "d" : "f");

					WalkItem(rootItem);
				}

				File.WriteAllLines(AllFilesList, _files.Select(t => string.Format("{0}	{1}", t.Item1, t.Item2)).ToArray());
			}

			Console.WriteLine("All files: " + AllFilesList);

			FilterFiles(opts);

			Console.WriteLine("Selected files: " + FilesList);
		}

		static List<Tuple<string, int>> LoadFrom(string path)
		{
			return File.ReadAllLines(path).Select(l => Tuple.Create(l.Split('\t')[0], Int32.Parse(l.Split('\t')[1]))).ToList();
		}

		public List<Tuple<string, int>> Load()
		{
			return LoadFrom(FilesList);
		}

		// spec -> isdir
		public Dictionary<string, bool> LoadRootTypes()
		{
			return File
				.ReadAllLines(DataFileRootTypes)
				.Where(l => !string.IsNullOrWhiteSpace(l))
				.Select(l => l.Trim().Split('\t'))
				.ToDictionary(ar => ar[0], ar => ar[1] == "d")
			;
		}

		public void FilterFiles(Options opts)
		{
			var files = LoadFrom(AllFilesList);

			var isInclude = opts.IncludePredicate;

			var excluded = files.Where(t => !isInclude(t.Item1)).ToList();
			files = files.Where(t => isInclude(t.Item1)).ToList();

			// write included & excluded
			File.WriteAllLines(FilesList, files.Select(t => string.Format("{0}	{1}", t.Item1, t.Item2)).ToArray());
			File.WriteAllLines(ExcludedFilesList, excluded.Select(t => string.Format("{0}	{1}", t.Item1, t.Item2)).ToArray());

			// calc stats

			// filter by prefix
			if (opts.Prefix != null)
			{
				files = files
					.Where(t => t.Item1.Replace('\\', '/').StartsWith(opts.Prefix, StringComparison.OrdinalIgnoreCase))
					.ToList()
				;
			}

			// filter by filter
			if (opts.FilterRx != null)
			{
				files = files
					.Where(t => opts.FilterRx.IsMatch(t.Item1.Replace('\\', '/')))
					.ToList()
				;
			}

			// build extensions map
			Console.WriteLine("Files extensions:");
			files
				.Select(t => Path.GetExtension(t.Item1))
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
				// overview
				map.WriteLine("== Overview ==");
				map.WriteLine("<all>    : Count: {0,5}, Size: {1,10:0.00} Kb", files.Count, files.Sum(f => (double)f.Item2) / 1024.0);

				files
					.Select(t => new { Ext = (Path.GetExtension(t.Item1) ?? "").ToLowerInvariant(), Size = t.Item2 })
					.GroupBy(x => x.Ext)
					.OrderBy(g => g.Sum(f => f.Size))
					//					.OrderBy(g => g.Key)
					.ToList()
					.ForEach(g => map.WriteLine("{0,-9}: Count: {1,5}, Size: {2,10:0.00} Kb, Avg size: {3,7:0.00} Kb", g.Key, g.Count(), g.Sum(f => f.Size) / 1024.0, g.Sum(f => f.Size) / 1024.0 / g.Count()))
				;

				map.WriteLine();
				map.WriteLine();
				map.WriteLine("== Detailed ==");

				files
					.GroupBy(t => (Path.GetExtension(t.Item1) ?? "").ToLowerInvariant())
					.ToList()
					.ForEach(g =>
					{
						map.WriteLine("{0}({1}):", g.Key, g.Count());

						foreach (var f in g.OrderByDescending(ff => ff.Item2))
						{
							map.WriteLine("{0,10} {1}", f.Item2, f.Item1);
						}
						map.WriteLine();
					})
				;
			}

			// dump files by size
			using (var map = File.CreateText(DataSizesFileName))
			{
				files
					.Select(t => new { Spec = t.Item1, Size = t.Item2 })
					.OrderByDescending(inf => inf.Size)
					.ToList()
					.ForEach(inf => map.WriteLine("{0,10:0.0} KiB	{1}", inf.Size / 1024.0, inf.Spec))
				;
			}
		}

		void WalkItem(IVSSItem item)
		{
			if(item.Type == 1)
			{
				_files.Add(Tuple.Create(item.Spec, item.Size));
			}
			else
			{
				WalkItems(item.Items);
			}
		}

		void WalkItems(IVSSItems items)
		{
			foreach (IVSSItem item in items)
			{
				WalkItem(item);
			}
		}
	}
}
