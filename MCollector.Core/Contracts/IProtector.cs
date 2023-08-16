using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCollector.Core.Contracts
{
    public interface IProtector
    {
        /// <summary>
        /// 加密内容会添加指定的前缀，如：@???:
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public string Protect(string content);

        /// <summary>
        /// 解决失败时返回原文
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public string Unprotect(string content);

        /// <summary>
        /// 查找所有密文
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public string[] FindProtectedText(string content);

        /// <summary>
        /// 查所所有密文并替换解密
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        string FindAndUnprotectText(string content);
    }
}
