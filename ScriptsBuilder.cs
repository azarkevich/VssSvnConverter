using System.Collections.Generic;
using System.IO;
using System.Linq;
using vcslib;
using System;
using System.Diagnostics;

namespace VssSvnConverter
{
	class ScriptsBuilder
	{
		public void Build(Options opts)
		{
			if(!Directory.Exists("scripts"))
				Directory.CreateDirectory("scripts");

			using(var swRmLocal = File.CreateText("scripts\\remove-local.bat"))
			using(var swRmVss = File.CreateText("scripts\\remove-vss.bat"))
			{
				swRmVss.WriteLine("set PATH=%PATH%;C:\\Program Files (x86)\\Microsoft Visual SourceSafe");

				swRmVss.WriteLine("set SSDIR={0}", Path.GetDirectoryName(opts.DB.SrcSafeIni));
				swRmVss.WriteLine("set SSUSER={0}", opts.Config["source-safe-user"].LastOrDefault() ?? "<TODO>");
				swRmVss.WriteLine("set SSPWD={0}", opts.Config["source-safe-password"].LastOrDefault() ?? "<TODO>");

				// remove vss projects, local files
				foreach (var root in opts.VssRoots)
				{
					swRmVss.WriteLine("ss.exe DELETE \"{0}\"", root);

					swRmLocal.WriteLine("rd /S /Q \"{0}\"", root.TrimStart("$/\\".ToCharArray()).Replace('/', '\\').TrimEnd('\\'));
					swRmLocal.WriteLine("del /F \"{0}\"", root.TrimStart("$/\\".ToCharArray()).Replace('/', '\\'));
				}
			}

			// generate script for update links information + new links.db file

			using(var sw = File.CreateText("scripts\\apply-link-tokens.bat"))
			{
				var file2Token = new XRefMap();
				var linksDb = opts.Config["links-db-latest"].FirstOrDefault();
				if(linksDb != null)
					file2Token.LoadTokenFile(linksDb);

				var token2Files = file2Token.Inverse();

				var importedLinks = new XRefMap();
				importedLinks.Load(LinksBuilder.DataFileUiName);

				// 1. Build set with imported files
				var importedSet = new HashSet<string>();
				foreach (var importedLink in importedLinks.Map.Keys)
				{
					importedSet.Add(importedLink);
				}

				// files which added to link DB.
				var add2DbSet = new HashSet<string>();
				foreach (var importedLink in importedLinks.Map.Keys)
				{
					var otherLinks = importedLinks.Map[importedLink].ToList();
					var links = otherLinks.ToList();
					links.Insert(0, importedLink);

					// find or construct token for this bunch of files
					string token = null;
					foreach (var link in links)
					{
						if(file2Token.Map.ContainsKey(link))
						{
							token = file2Token.Map[link][0];
							break;
						}
					}

					// Build new token
					if(token == null)
					{
						token = Path.GetFileName(importedLink).ToLowerInvariant();
						if(token2Files.Map.ContainsKey(token))
							token = token + "!" + DateTimeOffset.UtcNow.Ticks;

						Debug.Assert(token2Files.Map.ContainsKey(token) == false);
					}

					// buld list which should be added to db
					var forAdd2Db = otherLinks.Where(l => !importedSet.Contains(l) && !add2DbSet.Contains(l)).ToList();

					// mark imported file with token
					sw.WriteLine("svn ps ihs:link-token \"{0}\" \"{1}\"", token, importedLink.Trim("$/\\".ToCharArray()));

					// add other links into DB
					forAdd2Db.ForEach(l => {
						Console.WriteLine("Add 2 DB: {0}", l);
						file2Token.AddRef(l, token);
						token2Files.AddRef(token, l);
						add2DbSet.Add(l);
					});
				}

				if(linksDb != null)
					file2Token.SaveTokenFile(linksDb + ".u");
			}
		}
	}
}
