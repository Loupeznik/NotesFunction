using DZarsky.CommonLibraries.AzureFunctions.Infrastructure;
using DZarsky.CommonLibraries.AzureFunctions.Models.Auth;
using DZarsky.NotesFunction.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

[assembly: FunctionsStartup(typeof(DZarsky.NotesFunction.Startup))]
namespace DZarsky.NotesFunction
{
    internal class Startup : FunctionsStartup
    {
        private static readonly IConfigurationRoot Configuration = new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddUserSecrets<Startup>(true)
            .Build();

        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.AddCommonFunctionServices(Configuration, AuthType.Zitadel);

            builder.Services.AddScoped<NoteService>();
        }
    }
}
