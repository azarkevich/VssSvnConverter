using System;

namespace VssSvnConverter.Core
{
	static class Utils
	{
		static readonly DateTime UnixTimeBase = new DateTime(1970, 1, 1, 0, 0, 0, 0);

		public static long GetUnixTimestamp(DateTime dt)
		{
			return (long)(dt - UnixTimeBase).TotalSeconds;
		}
	}
}
