using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SharpSvn;

namespace VssSvnConverter
{
	class WcBuilder
	{
		public void Build(Options opts)
		{
			File.WriteAllText("4-import.txt", "0\n");

			if(Directory.Exists("svn-wc"))
			{
				Directory.Delete("svn-wc", true);
			}
			
			using (var svn = new SvnRepositoryClient())
			{
				if(Directory.Exists(opts.SvnRepo))
					svn.DeleteRepository(opts.SvnRepo);

				svn.CreateRepository(opts.SvnRepo);
			}

			// create hooks
			File.WriteAllText(Path.Combine(opts.SvnRepo, "hooks/post-revprop-change.bat"), "exit 0");
			File.WriteAllText(Path.Combine(opts.SvnRepo, "hooks/pre-revprop-change.bat"), "exit 0");

			using (var svn = new SvnClient())
			{
				svn.CheckOut(new SvnUriTarget(opts.SvnRepoUri), "svn-wc");
			}
		}
	}
}
