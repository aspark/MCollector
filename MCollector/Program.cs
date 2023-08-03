// See https://aka.ms/new-console-template for more information


using Autofac;
using Autofac.Extensions.DependencyInjection;
using MCollector.Core;
using MCollector.Core.Config;
using MCollector.Core.Contracts;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Net;
using System.Reflection;


var options = new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default
};

var builder = WebApplication.CreateBuilder(options); 

builder.Services.AddLogging(cfg => cfg.AddConsole());

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory(cfg =>
{
    var assemblies = new List<Assembly>() { Assembly.GetEntryAssembly()!, typeof(CollectorStarter).Assembly };

    //加载插件
    var dirPlugins = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
    if (Directory.Exists(dirPlugins))
    {
        //var loaded = new HashSet<string>(AppDomain.CurrentDomain.GetAssemblies().Select(a => Path.GetFileName(a.Location)));
        foreach (var file in Directory.GetFiles(dirPlugins, "*.dll", SearchOption.AllDirectories))
        {
            var name = AssemblyName.GetAssemblyName(file);
            var fileName = Path.GetFileName(file);
            //if (!loaded.Contains(fileName))
            {
                var asm = Assembly.LoadFrom(file);
                if (fileName.StartsWith("MCollector", StringComparison.InvariantCultureIgnoreCase)) //只resolve MCollector的类
                    assemblies.Add(asm);

                //loaded.Add(fileName);
            }
        }
    }

    cfg.RegisterAssemblyTypes(assemblies.ToArray()).AsImplementedInterfaces().Where(t => t.IsAssignableTo(typeof(IAsSingleton))).AsSelf().AsImplementedInterfaces().SingleInstance();
    cfg.RegisterAssemblyTypes(assemblies.ToArray()).Where(t => !t.IsAssignableTo(typeof(IAsSingleton))).AsImplementedInterfaces();
    //cfg.register
}));


builder.Host.UseWindowsService();

//

//builder.Configuration.AddYamlFile("collector.yml", false, true);
//builder.Services.Configure<CollectorConfig>(builder.Configuration);

//暂时手动load
var config = ConfigParser.GetConfig();
builder.Services.AddSingleton<IOptions<CollectorConfig>>(Options.Create(config));

builder.Services.AddHttpClient("default").ConfigurePrimaryHttpMessageHandler(()=> {
    return new HttpClientHandler()
    {
        AllowAutoRedirect = true,
        AutomaticDecompression = DecompressionMethods.All
    };
});
builder.Services.AddControllers();

//builder.Configuration.GetSection()

var app = builder.Build();

app.MapControllers();


app.Run("http://0.0.0.0:" + config.Port);//host.Configuration["port"]

