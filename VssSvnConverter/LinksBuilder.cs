using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SourceSafeTypeLib;
using vcslib;
using VssSvnConverter.Core;

namespace VssSvnConverter
{
	class LinksBuilder
	{
		public const string DataFileName = "3-state-links.txt";
		public const string DataFileUiName = "3-state-links-ui.txt";
		public const string DataFileCoName = "3-state-checkouts.txt";
		public const string DataFileCoByDateName = "3-state-checkouts-by-date.txt";

		public void Build(Options opts, IList<Tuple<string, int>> files)
		{
			var coDict = new Dictionary<DateTime, List<Tuple<string, string>>>();

			var xrefsCo = new XRefMap();
			var xrefs = new XRefMap();
			foreach (var file in files.Select(t => t.Item1))
			{
				Console.WriteLine(file);

				var item = opts.DB.VSSItem[file];

				foreach (IVSSItem vssLink in item.Links)
				{
					if (!String.Equals(item.Spec, vssLink.Spec, StringComparison.OrdinalIgnoreCase) && !vssLink.Deleted)
						xrefs.AddRef(item.Spec, vssLink.Spec);

					foreach (IVSSCheckout vssCheckout in vssLink.Checkouts)
					{
						xrefsCo.AddRef(item.Spec, vssCheckout.Username + " at " + vssCheckout.Date);

						var coDate = vssCheckout.Date.Date;
						List<Tuple<string, string>> list;
						if(!coDict.TryGetValue(coDate, out list))
						{
							list = new List<Tuple<string, string>>();
							coDict[coDate] = list;
						}

						list.Add(Tuple.Create(vssCheckout.Username, item.Spec));
					}
				}
			}

			xrefs.Save(DataFileName);
			xrefs.Save(DataFileUiName, true);
			xrefsCo.Save(DataFileCoName, true);

			// save co dict by dates
			using (var tw = File.CreateText(DataFileCoByDateName))
			{
				foreach (var kvp in coDict.OrderByDescending(kvp => kvp.Key))
				{
					tw.WriteLine("{0:yyyy-MM-dd} ({1} days ago)", kvp.Key, (int)(DateTime.Now - kvp.Key).TotalDays);
					foreach (var tuple in kvp.Value)
					{
						tw.WriteLine("\t{0} at {1}", tuple.Item1, tuple.Item2);
					}
					tw.WriteLine();
				}
			}
		}
	}
}
