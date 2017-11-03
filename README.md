VssSvnConverter
===============

Yet another converter of Visual Source Safe database to Subversion / Git / TFS repository

Why?

This converter was written and tested during gradual conversion of 18GB VSS repository to Subversion then to TFS and finally to Git.
No one other tool(which I try), such as Vss2Svn, VssMigrate was unable doing this work. It is too big, to complex and corrupted.

So, this converter has advantages and disadvantages.

Most significant disadvantages is

This tool require setup in config files and has _very_ minimalistic GUI.
* Converted only currently visible sources tree, i.e:
* Deleted files not converted
* Moved files will be look as if already live on last place
* Copied files will not have common ancestor.
* Empty directories not converted

But, also took has next advantages:

* If you have big (really BIG) VSS repository, it is possible convert only part of it. Then next piece, next... We continue such continuous conversion already for 3 years.
* Tool has rich filtration rules for prevent conversion unnecessary files in destination repository
* Tool use COM interface to VSS database, but in case of error also try CLI (ss.exe). Some time CLI interface work correct, while COM - failed.
* For several conversion tries used file cache for speedup access to VSS
* Setting for grouping changed into commits
* Mapping user names
* Correct import of pinned files (only latest version)


Conversion procedure
--------------------

* Modify in VssSvnConverter.conf file path to source safe database (full path to sourcesafe.ini file), username and password
* Select one or many VSS project for import and add to config file
* Modify import patterns for exclude *.*scc files and other unwanted files: dll, exe, pdb ...
* If you have autogenerated files, which checked in after each nightly build, this files can be stored with only latest version (for avoid noise in history). Use latest-only key in config
* start VssSvnConverter.exe and perform action step by step as they appears in UI. After each step you can analyze utility output, fix some issues and rerun required step. Utility pass all data through text files which can be edited manually if required.
* Skip last step (8. Build Scripts) because it is mostly for use with my SVN hooks.

After conversion import data to production repository in special directory for imports
```
svnadmin dump d:\VssSvnConverter\_repository | svnadmin load d:\production-svn-repo\ --parent-dir /import
```
And then use standard SVN tools for move imported stuff from /import to any correct place in repository.

Git support
-----------
Despite its name (VssSvnConverter), this utility also support conversion to Git and TFS

Git supported in 2 variants:
 * generate fast-import datapack, which can be then imported with command 
```
git fast-import < datapack
```
For generate fast-import file use command line as last step (instead of import)
```
VssSvnConverter.exe git-fast-import
```
it generate datapack with name 6-git-fast-import.dat

fast-import mopde does not support features: unimportant-diff, censore.

Also author names should be in format `name <email>`

 * 'git' driver, which can be selected in config.

Use command line for perform commits one-by-one. Slower, but support features: unimportant-diff, censore.

TFS support
-----------
TFS also supported as 'tfs' driver in config. It use tf.exe command for perform adding/commits.
