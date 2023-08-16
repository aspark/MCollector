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

builder.Services.AddLogging(cfg => cfg.AddConsole().SetMinimumLevel(LogLevel.Warning));

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory(cfg =>
{
    var assemblies = new List<Assembly>() {
        Assembly.GetEntryAssembly()!,
        typeof(CollectorStarter).Assembly,
        typeof(MCollector.Plugins.Prometheus.PrometheusExporter).Assembly,
        typeof(MCollector.Plugins.OAuth.OAuth20Preparer).Assembly,
        typeof(MCollector.Plugins.ES.ESIndicesCollector).Assembly,
        typeof(MCollector.Plugins.AgileConfig.AgileConfigCollector).Assembly,
    };

    //加载插件
    var dirPlugins = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
    if (Directory.Exists(dirPlugins))
    {
        var loaded = new HashSet<string>(AppDomain.CurrentDomain.GetAssemblies().Select(a => a.FullName!));
        foreach (var file in Directory.GetFiles(dirPlugins, "*.dll", SearchOption.AllDirectories))
        {
            var name = AssemblyName.GetAssemblyName(file).FullName;
            //var fileName = Path.GetFileName(file);
            if (!loaded.Contains(name))
            {
                var asm = Assembly.LoadFrom(file);
                if (name.StartsWith("MCollector", StringComparison.InvariantCultureIgnoreCase)) //只resolve MCollector的类
                    assemblies.Add(asm);

                loaded.Add(name);
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
builder.Services.AddSingleton<IOptions<CollectorConfig>>(sp => Options.Create(sp.GetRequiredService<IConfigParser>().Get()));

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


app.Run("http://0.0.0.0:" + app.Services.GetRequiredService<IOptions<CollectorConfig>>().Value.Port);//host.Configuration["port"]

