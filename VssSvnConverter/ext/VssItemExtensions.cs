using System;
using SourceSafeTypeLib;

namespace vsslib
{
	public static class VssItemExtensions
	{
		/// <summary>
		/// If VSS conatins $/Project1 but requested $/project1, then case will not be fixed and $/project1 will be retuned.
		/// This methor return case to normal -> $/Project1
		/// </summary>
		/// <param name="item">Item for normalize</param>
		/// <returns>Normalized item</returns>
		public static IVSSItem Normalize(this IVSSItem item, IVSSDatabase db)
		{
			IVSSItem current = db.VSSItem["$/"];

			if (item.Spec.Replace('\\', '/').TrimEnd("/".ToCharArray()) == "$")
				return current;

			foreach (var pathPart in item.Spec.Replace('\\', '/').TrimStart("$/".ToCharArray()).TrimEnd('/').Split('/'))
			{
				IVSSItem next = null;
				foreach (IVSSItem child in current.Items)
				{
					var parts = child.Spec.TrimEnd('/').Split('/');
					var lastPart = parts[parts.Length - 1];

					if(String.Compare(pathPart, lastPart, StringComparison.OrdinalIgnoreCase) == 0)
					{
						next = child;
						break;
					}
				}

				if(next == null)
					throw new ApplicationException("Can't normalize: " + item.Spec);

				current = next;
			}

			return current;
		}
	}
}
