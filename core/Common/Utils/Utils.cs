using System.Text;

namespace OnlineMarket.Core.Common.Utils
{
	public sealed class Utils
	{
		public static Guid GetGuid(string str)
		{
            byte[] array = new byte[16]; 
			var base_ = Encoding.UTF8.GetBytes(str);
            for (int i = 0; i < base_.Length; i++)
            {
                array[i] = base_[i];
            }
            return new Guid(array);
        }
	}
}