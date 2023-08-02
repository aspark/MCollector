using System.Security.Cryptography;
using System.Text;

namespace MCollector.Core.Common
{
    internal class HashHelper
    {
        public static string SHA1(string content)
        {
            using var hash = System.Security.Cryptography.SHA1.Create();
            return Convert.ToHexString(hash.ComputeHash(Encoding.UTF8.GetBytes(content))).ToLower();
        }
    }
}
