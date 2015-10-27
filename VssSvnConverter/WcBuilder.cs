using System.IO;
using System;
using VssSvnConverter.Core;

namespace VssSvnConverter
{
	class WcBuilder
	{
		public void Build(Options opts)
		{
			File.WriteAllText(Importer.DataFileName, "0\n");

			if (opts.DestinationDriver == "git")
			{
				if (!opts.IsRepoDirExternal)
				{
					GitDriver.Create(opts.GitExe, opts.RepoDir);
				}
			}
			else if(opts.DestinationDriver == "tfs")
			{
				var driver = new TfsDriver(opts.TfExe, opts.RepoDir, Console.Out);
				driver.CleanupWorkingTree();
			}
			else
			{
				SvnDriver.Create(opts.RepoDir, Path.Combine(Environment.CurrentDirectory, "svn-wc"));
			}
		}
	}
}
