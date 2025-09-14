using DZarsky.CommonLibraries.AzureFunctions.Infrastructure;
using DZarsky.CommonLibraries.AzureFunctions.Models.Auth;
using DZarsky.NotesFunction.HealthChecks;
using DZarsky.NotesFunction.Infrastructure;
using DZarsky.NotesFunction.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    //.AddUserSecrets<Program>(true)
    .Build();

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services => {
        services.AddCommonFunctionServices(configuration, AuthType.Zitadel);
        services.AddFirebase(configuration);
        services.AddScoped<NoteService>();
        services.AddScoped<IntegrationConnectivityHealthCheck>();
    })
    .ConfigureAppConfiguration(builder => builder.AddConfiguration(configuration))
    .Build();

host.Run();
