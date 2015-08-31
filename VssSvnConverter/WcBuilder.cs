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

			if(opts.UseGit)
			{
				if (!opts.IsRepoDirExternal)
				{
					GitDriver.Create(opts.GitExe, opts.RepoDir);
				}
			}
			else
			{
				SvnDriver.Create(opts.RepoDir, Path.Combine(Environment.CurrentDirectory, "svn-wc"));
			}
		}
	}
}
