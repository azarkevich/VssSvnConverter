using System;

namespace VssSvnConverter.Core
{
	interface IDestinationDriver
	{
		string WorkingCopy { get; }

		void CleanupWorkingTree();

		void StartRevision();
		void AddDirectory(string dir);
		void AddFiles(params string[] files);
		string GetDiff(string file);
		void Revert(string file);
		void CommitRevision(string author, string comment, DateTime time);
	}
}
