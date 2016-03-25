using System.IO;
using System;
using System.Windows.Forms;
using VssSvnConverter.Core;

namespace VssSvnConverter
{
	class WcBuilder
	{
		public void Build(Options opts, bool noPrompt)
		{
			if (opts.ImportDriver == "git")
			{
				if (!opts.IsGitRepoDirExternal)
				{
					if (noPrompt || MessageBox.Show("Repository and work tree will be recreated", "Confirm", MessageBoxButtons.OKCancel) == DialogResult.OK)
					{
						File.WriteAllText(Importer.DataFileName, "0\n");
						GitDriver.Create(opts.GitExe, opts.GitRepoDir);
					}
				}
			}
			else if(opts.ImportDriver == "tfs")
			{
				if (noPrompt || MessageBox.Show("Work tree will be cleanup", "Confirm", MessageBoxButtons.OKCancel) == DialogResult.OK)
				{
					var driver = new TfsDriver(opts.TfExe, opts.TfsWorkTreeDir, Console.Out, false);
					driver.CleanupWorkingTree();
				}
			}
			else if (opts.ImportDriver == "svn")
			{
				if (opts.IsSvnRepoDirExternal)
				{
					if (noPrompt || MessageBox.Show("Working copy will be recreated (but not repository)", "Confirm", MessageBoxButtons.OKCancel) == DialogResult.OK)
					{
						SvnDriver.Checkout(opts.SvnRepoUrl, opts.SvnWorkTreeDir);
					}
				}
				else
				{
					if (noPrompt || MessageBox.Show("Repository and Working copy will be recreated", "Confirm", MessageBoxButtons.OKCancel) == DialogResult.OK)
					{
						File.WriteAllText(Importer.DataFileName, "0\n");
						SvnDriver.CreateRepo(opts.SvnRepoUrl);
						SvnDriver.Checkout(opts.SvnRepoUrl, opts.SvnWorkTreeDir);
					}
				}
			}
			else
			{
				throw new Exception("Unknown import driver: " + opts.ImportDriver);
			}
		}
	}
}
