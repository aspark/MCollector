using MCollector.Core.Contracts;
using System.Diagnostics;
using System.Text;

namespace MCollector.Core.Collectors
{
    internal class CmdCollector : ICollector
    {
        public string Type => "cmd";

        //public string[] ForbidCommand { get; set; } = new string[] { "cd", "dir", "ls", "ll", "rm", "del", "mkdir", "touch" };

        public async Task<CollectedData> Collect(CollectTarget target)
        {
            return await ExecuteAll(target);
        }

        private (string app, string args) ParseCmd(string line)
        {
            var app = line;
            var args = string.Empty;

            if (string.IsNullOrWhiteSpace(app))
            {
                app = Environment.OSVersion.Platform == PlatformID.Win32NT ? "cmd" : "/bin/bash";
            }


            //cmd /c
            //bin/bash -f adf --go
            //"a b c/c" -f args
            //regex???
            var split = app[0] == '"' ? app.IndexOf('"', 1) : app.IndexOf(' ');
            if (split > -1)//有分隔命令和参数的样式
            {
                if (split < app.Length)
                {
                    args = app.Substring(split + 1);
                }
                app = app.Substring(0, split);
            }

            return (app, args);
        }

        private async Task<CollectedData> ExecuteAll(CollectTarget target)
        {
            (var app, var args) = ParseCmd(target.Target);

            //if (target.Contents != null && target.Contents.Any())
            //    args += ("/c " + string.Join("&&", target.Contents));

            var commands = target.Contents?.ToList() ?? new List<string>();

            var info = new ProcessStartInfo(app, args)
            {
                WorkingDirectory = Directory.GetCurrentDirectory(),
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                //StandardErrorEncoding = Encoding.UTF8,
                //StandardInputEncoding = Encoding.UTF8,
                //StandardOutputEncoding = Encoding.UTF8
            };

            var data = new CollectedData(target.Name, target) { };

            using (var process = new Process() { StartInfo = info })
            {
                var output = new List<string>();
                //var error = new List<string>(); //curl -v 使用的是error流输出的。。。
                using var watier = new Waiter(1500);

                process.Start();

                process.OutputDataReceived += (o, args) => {
                    watier.Reset();
                    if (args.Data != null)
                        output.Add(args.Data);
                };

                process.ErrorDataReceived += (o, args) => {
                    watier.Reset();
                    //data.IsSuccess = false;
                    if (args.Data != null)
                        output.Add(args.Data);
                };

                process.BeginErrorReadLine();
                process.BeginOutputReadLine();

                var prompts = new List<string>();

                try
                {
                    if (commands.Any())
                    {
                        process.StandardInput.AutoFlush = true;

                        foreach (var cmd in commands)
                        {
                            if (!data.IsSuccess)
                                break;

                            process.StandardInput.WriteLine(cmd);
                            //process.StandardInput.Flush();

                            //process.WaitForInputIdle();
                            watier.WaitForIdle();
                            //await ReadLines(process, output, error);

                            if (output.Any())
                            {
                                data.Content += (string.Join(Environment.NewLine, output) + Environment.NewLine);
                                output.Clear();
                            }

                            //if (error.Any())
                            //{
                            //    data.Content += (string.Join(Environment.NewLine, error) + Environment.NewLine);
                            //    error.Clear();
                            //}
                        }
                    }

                    await process.StandardInput.DisposeAsync();
                    await process.WaitForExitAsync();
                }
                catch (Exception ex)
                {
                    data.IsSuccess = false;
                    data.Content += ex.GetBaseException().Message;

                    return data;
                }

                if(data.IsSuccess)
                    data.IsSuccess = process.ExitCode == 0;
            }

            return data;
        }

        public class Waiter : IDisposable
        {
            private int _timeout = 1000;
            AutoResetEvent _mre = null;

            public Waiter(int timeout)
            {
                _timeout = timeout;
                _mre = new AutoResetEvent(false);
            }

            public void Dispose()
            {
                _mre?.Dispose();
            }

            public void Reset()
            {
                _mre.Set();
            }

            public void WaitForIdle()
            {
                for (var i = 0; i < 1000; i++)//不用while，避免一直等待
                {
                    if (_mre.WaitOne(_timeout) == false) { break; }//等待时间到了，退出
                }
            }
        }
        //private async Task ReadLines(Process proc, List<string> output, List<string> error)
        //{
        //    output.Add(await ReadLines(proc.StandardOutput));
        //    error.Add(await ReadLines(proc.StandardError));
        //}

        //private async Task<string> ReadLines(StreamReader sr)
        //{
        //    var content = new StringBuilder();

        //    for (var i = 0; i < 10; i++)
        //    {
        //        while (true)
        //        {
        //            if (sr.Peek() > -1)
        //            {
        //                content.Append((char)sr.Read());
        //            }
        //            else
        //            {
        //                await Task.Delay(500);
        //                break;
        //            }
        //        }
        //    }

        //    return content.ToString();
        //}
    }



    //internal class CmdCollector : ICollector
    //{
    //    public string Type => "cmd";

    //    public Task<CollectedData> Collect(CollectTarget target)
    //    {

    //        var app = target.Target;
    //        var args = string.Empty;

    //        var commands = target.Contents?.ToList() ?? new List<string>();
    //        if (string.IsNullOrWhiteSpace(app))
    //        {
    //            app = Environment.OSVersion.Platform == PlatformID.Win32NT ? "cmd" : "/bin/bash";
    //        }


    //        //cmd /c
    //        //bin/bash -f adf --go
    //        //"a b c/c" -f args
    //        //regex???
    //        var split = app[0] == '"' ? app.IndexOf('"', 1) : app.IndexOf(' ');
    //        if (split > -1)//有分隔命令和参数的样式
    //        {
    //            if (split < app.Length)
    //            {
    //                args = app.Substring(split + 1);
    //            }
    //            app = app.Substring(0, split);
    //        }


    //        //if (target.Contents != null && target.Contents.Any())
    //        //    args += ("/c " + string.Join("&&", target.Contents));

    //        var info = new ProcessStartInfo(app, args)
    //        {
    //            WorkingDirectory = Directory.GetCurrentDirectory(),
    //            CreateNoWindow = true,
    //            RedirectStandardError = true,
    //            RedirectStandardInput = true,
    //            RedirectStandardOutput = true,
    //            //StandardErrorEncoding = Encoding.UTF8,
    //            //StandardInputEncoding = Encoding.UTF8,
    //            //StandardOutputEncoding = Encoding.UTF8
    //        };

    //        var data = new CollectedData(target.Name, target) { };
    //        //Debug.WriteLine("开始cmd" + DateTime.Now.ToString("HH;mm:ss"));

    //        using (var process = new Process() { StartInfo = info })
    //        {

    //        }

    //        return null;
    //    }
    //}
}
