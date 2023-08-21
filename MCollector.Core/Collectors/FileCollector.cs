using MCollector.Core.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCollector.Core.Collectors
{
    internal class FileCollector : ICollector
    {
        public string Type => "file";

        public async Task<CollectedData> Collect(CollectTarget target)
        {
            var data = new CollectedData(target.Name, target);

            var path = target.Target;
            if(Path.IsPathRooted(path) == false)
            {
                path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
            }

            if (!File.Exists(path))
            {
                data.IsSuccess = false;
                data.Content = "文件不存在";
            }
            else
            {
                data.Content = await File.ReadAllTextAsync(path);
            }

            return data;
        }
    }
}
