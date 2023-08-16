using k8s;
using MCollector.Core.Common;
using MCollector.Core.Contracts;
using Microsoft.AspNetCore.DataProtection;
using System.Text.RegularExpressions;

namespace MCollector.Common
{
    public class DefaultProtector : IProtector
    {
        IDataProtector _protector;
        public DefaultProtector(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector("mcollector");
        }

        public string Protect(string content)
        {
            return Const.EncrptedTextPrefix + _protector.Protect(content);
        }

        public string Unprotect(string content)
        {
            if(!string.IsNullOrEmpty(content))
            {
                if (content.StartsWith(Const.EncrptedTextPrefix))
                {
                    content = content.Substring(Const.EncrptedTextPrefix.Length);
                }
                try
                {
                    return _protector.Unprotect(content);
                }
                catch(Exception ex)
                {
                    Console.Write(ex.ToString());
                }
            }

            return content;
        }

        public string[] FindProtectedText(string content)
        {
            var items = new string[0];

            if (!string.IsNullOrWhiteSpace(content))
            {
                var reg = new Regex(Regex.Escape(Const.EncrptedTextPrefix) + @"[\w\-_]+");//@\?\?\?\:
                var matches = reg.Matches(content);
                if (matches.Any())
                {
                    items = matches.Select(m => m.Value).ToArray();
                }
            }

            return items;
        }

        public string FindAndUnprotectText(string content)
        {
            if (!string.IsNullOrWhiteSpace(content))
            {
                var items = FindProtectedText(content);
                if (items.Any())
                {
                    foreach (var item in items)
                    {
                        content = content.Replace(item, Unprotect(item));
                    }
                }
            }

            return content;
        }
    }
}
