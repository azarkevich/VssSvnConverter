using System;

namespace VssSvnConverter.Core
{
	interface IDestinationDriver
	{
		string WorkingCopy { get; }

		void StartRevision();
		void AddDirectory(string dir);
		void AddFile(string file);
		string GetDiff(string file);
		void Revert(string file);
		void CommitRevision(string author, string comment, DateTime time);
	}
}
