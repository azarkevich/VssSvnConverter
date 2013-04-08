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

				foreach (IVSSItem vssLink in item.Links)
				{
					if(item.Spec != vssLink.Spec)
						xrefs.AddRef(item.Spec, vssLink.Spec);

					foreach (IVSSCheckout vssCheckout in vssLink.Checkouts)
					{
						xrefsCo.AddRef(item.Spec, vssCheckout.Username + " at " + vssCheckout.Date);
					}
				}
			}

			xrefs.Save(DataFileName);
			xrefs.Save(DataFileUiName, true);
			xrefsCo.Save(DataFileCoName, true);
		}
	}
}
