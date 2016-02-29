using System.Collections.Generic;
using System.Linq;

namespace VssSvnConverter.Core
{
	public static class EnumerableExtensions
	{
		public static List<List<string>> Partition(this IList<string> source, int chunkSize)
		{
			return source
				.Select((x, i) => new { Index = i, Value = x })
				.GroupBy(x => x.Index / chunkSize)
				.Select(x => x.Select(v => v.Value).ToList())
				.ToList()
			;
		}
	}
}
