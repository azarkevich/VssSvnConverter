using System;
using System.Collections.Generic;
using SourceSafeTypeLib;
using vcslib;

namespace VssSvnConverter
{
	class LinksBuilder
	{
		public void Build(Options opts, List<string> files)
		{
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
				}
			}

			xrefs.Save("2a-links-list.txt");
			xrefs.Save("2a-links-list-ui.txt", true);
		}

		string NormPath(string path)
		{
			return path.Replace('\\', '/').ToLowerInvariant();
		}
	}
}
