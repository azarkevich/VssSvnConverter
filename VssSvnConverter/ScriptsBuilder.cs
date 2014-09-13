using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using vcslib;
using System;
using System.Diagnostics;

namespace VssSvnConverter
{
	class ScriptsBuilder
	{
		public void Build(Options opts, IList<Tuple<string, int>> importSpecs, Dictionary<string, bool> roots)
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
				swRmVss.WriteLine();

				// remove vss projects, local files
				foreach (var kvp in roots)
				{
					swRmVss.WriteLine("ss.exe DELETE \"{0}\"", kvp.Key);

					if(kvp.Value)
						swRmLocal.WriteLine("rd /S /Q \"{0}\"", kvp.Key.TrimStart("$/\\".ToCharArray()).Replace('/', '\\').TrimEnd('\\'));
					else
						swRmLocal.WriteLine("del /F \"{0}\"", kvp.Key.TrimStart("$/\\".ToCharArray()).Replace('/', '\\'));
				}
			}

			// generate script for update links information + new links.db file
			using(var sw = File.CreateText("scripts\\apply-link-tokens.bat"))
			{
				var file2Token = new XRefMap();
				var linksDb = opts.Config["links-db-latest"].FirstOrDefault();
				if (linksDb != null)
				{
					IDisposable disp = null;
					try
					{
						var user = opts.Config["links-db-user"].FirstOrDefault();
						var password = opts.Config["links-db-password"].FirstOrDefault();
						if (user != null && password != null)
						{
							disp = WindowsImpersonation.Impersonate(new NetworkCredential(user, password));
						}

						// get max file
						if (Directory.Exists(linksDb))
						{
							linksDb = Directory
								.GetFiles(linksDb, "*.gz", SearchOption.AllDirectories)
								.Select(f => new { Ind = Int32.Parse(Path.GetFileNameWithoutExtension(f)), Path = f })
								.OrderByDescending(f => f.Ind)
								.First()
								.Path
							;
						}

						using (var src = new GZipStream(File.OpenRead(linksDb), CompressionMode.Decompress))
						using (var dst = File.Create(string.Format("_links_db_token_file.{0}.original.txt", Path.GetFileNameWithoutExtension(linksDb))))
							src.CopyTo(dst);

						using (var sr = new StreamReader(new GZipStream(File.OpenRead(linksDb), CompressionMode.Decompress)))
							file2Token.LoadTokenFile(sr);
					}
					finally
					{
						if(disp != null)
							disp.Dispose();
					}
				}

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
					var forAdd2Db = otherLinks.Where(l => !importedSet.Contains(l) && !add2DbSet.Contains(l) && !file2Token.Map.ContainsKey(l)).ToList();

					// mark imported file with token
					var file = importedLink.Trim("$/\\".ToCharArray());
					Console.WriteLine("ihs:link-token: {0}", file);
					sw.WriteLine("svn ps ihs:link-token \"{0}\" \"{1}\"", token, file);

					// add other links into DB
					forAdd2Db.ForEach(l => {
						Console.WriteLine("Add 2 DB: {0}", l);
						file2Token.AddRef(l, token);
						token2Files.AddRef(token, l);
						add2DbSet.Add(l);
					});

					Console.WriteLine();
				}

				// check all imported specs (except already handled) if they has token in db
				foreach (var imported in importSpecs.Select(t => t.Item1).Where(i => !importedSet.Contains(i)))
				{
					List<string> files;
					if(!file2Token.Map.TryGetValue(imported, out files))
						continue;

					var token = files[0];

					// mark imported file with token
					var file = imported.Trim("$/\\".ToCharArray());

					Console.WriteLine("Mark with ihs:link-token: {0}", file);

					sw.WriteLine("svn ps ihs:link-token \"{0}\" \"{1}\"", token, file);
				}

				if (linksDb != null)
				{
					var updated = string.Format("_links_db_token_file.{0}.updated.txt", Path.GetFileNameWithoutExtension(linksDb));
					file2Token.SaveTokenFile(updated);
					File.AppendAllText(updated, "# hash: skip");
				}
			}
		}
	}
}
