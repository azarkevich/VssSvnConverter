using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace VssSvnConverter
{
	class ImportListStatsBuilder
	{
		const string DataExtsFileName = "1-import-list-exts.txt";
		const string DataSizesFileName = "1-import-list-sizes.txt";

		public void Build(Options opts, IList<Tuple<string, int>> files)
		{
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
				map.WriteLine("<all>    : Count: {0,5}, Size: {1,10:0.00} Kb", files.Count, files.Sum(f => f.Item2) / 1024.0);

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
					.ForEach(g => {
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
	}
}
