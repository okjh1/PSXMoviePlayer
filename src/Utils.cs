/**
 * PSX Movie Player.
 * 
 * author: okjh1
 * 
 * this project is licensed under CC-BY-SA-NC-4.0
 */

namespace HLPS1Str
{
	public class Utils
	{
		public static int SignExtend(uint v, int b)
		{
			uint m = (uint)(1 << (b - 1));
			return (int)((v ^ m) - m);
		}
	}
}
