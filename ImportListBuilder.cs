using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.SourceSafe.Interop;
using System.IO;

namespace VssSvnConverter
{
	class ImportListBuilder
	{
		public void Build(Options opts)
		{
			if(File.Exists("1-import-list.txt"))
				File.Delete("1-import-list.txt");

			_files = new List<string>();
			_isInclude = opts.IncludePredicate;

			using(_log = File.CreateText("1-import-list.txt.log"))
			{
				foreach (var root in opts.VssRoots)
				{
					Console.WriteLine("VSS Root: {0}", root);

					var rootItem = opts.DB.VSSItem[root];

					WalkItem(rootItem);
				}

				File.WriteAllLines("1-import-list.txt", _files.ToArray());
			}

			// build extensions map
			Console.WriteLine("Imported files extensions:");
			_files
				.Select(Path.GetExtension)
				.Select(e => e.ToLowerInvariant())
				.OrderBy(e => e)
				.GroupBy(e => e)
				.ToList()
				.ForEach(g => Console.Write("{0}({1}) ", g.Key, g.Count()))
			;
			Console.WriteLine();
			Console.WriteLine();

			Console.WriteLine("Building import files compete. Check: 1-import-list.txt");
		}

		public List<string> Load()
		{
			return File.ReadAllLines("1-import-list.txt").ToList();
		}

		List<string> _files;
		Func<string, bool> _isInclude;

		StreamWriter _log;

		void WalkItem(IVSSItem item)
		{
			if(item.Type == 1)
			{
				_files.Add(item.Spec);
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
