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
		const string LogFileName = "log-1-import-list.txt";

		List<Tuple<string, int>> _files;
		Func<string, bool> _isInclude;

		StreamWriter _log;

		public void Build(Options opts)
		{
			_files = new List<Tuple<string, int>>();
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

					WalkItem(rootItem);
				}

				File.WriteAllLines(DataFileName, _files.Select(t => string.Format("{0}	{1}", t.Item1, t.Item2)).ToArray());
			}

			new ImportListStatsBuilder().Build(opts, _files);

			Console.WriteLine("Building import files compete. Check: " + DataFileName);
		}

		public List<Tuple<string, int>> Load()
		{
			return File.ReadAllLines(DataFileName).Select(l => Tuple.Create(l.Split('\t')[0], Int32.Parse(l.Split('\t')[1]))).ToList();
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

		void WalkItem(IVSSItem item)
		{
			if(item.Type == 1)
			{
				_files.Add(Tuple.Create(item.Spec, item.Size));
				_log.WriteLine("+{0}", item.Spec);
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
				if(!_isInclude(item.Spec))
				{
					_log.WriteLine("-{0}", item.Spec);
					continue;
				}

				WalkItem(item);
			}
		}
	}
}
