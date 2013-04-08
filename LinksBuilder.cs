using System;
using System.Collections.Generic;
using SourceSafeTypeLib;
using vcslib;

namespace VssSvnConverter
{
	class LinksBuilder
	{
		public const string DataFileName = "3-state-links.txt";
		public const string DataFileUiName = "3-state-links-ui.txt";
		public const string DataFileCoName = "3-state-checkouts.txt";

		public void Build(Options opts, List<string> files)
		{
			var xrefsCo = new XRefMap();
			var xrefs = new XRefMap();
			foreach (var file in files)
			{
				Console.WriteLine(file);

				var item = opts.DB.VSSItem[file];
				var itemSpec = NormPath(item.Spec);

				foreach (IVSSItem vssItem in item.Links)
				{
					var linkSpec = NormPath(vssItem.Spec);

					if(itemSpec != linkSpec)
						xrefs.AddRef(itemSpec, linkSpec);

					foreach (IVSSCheckout vssCheckout in vssItem.Checkouts)
					{
						xrefsCo.AddRef(itemSpec, vssCheckout.Username + " at " + vssCheckout.Date);
					}
				}
			}

			xrefs.Save(DataFileName);
			xrefs.Save(DataFileUiName, true);
			xrefsCo.Save(DataFileCoName, true);
		}

		string NormPath(string path)
		{
			return path.Replace('\\', '/').ToLowerInvariant();
		}
	}
}
