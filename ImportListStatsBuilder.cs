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
				files
					.GroupBy(t => (Path.GetExtension(t.Item1) ?? "").ToLowerInvariant())
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
