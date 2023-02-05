using DZarsky.CommonLibraries.AzureFunctions.Infrastructure;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

[assembly: FunctionsStartup(typeof(DZarsky.NotesFunction.Startup))]
namespace DZarsky.NotesFunction
{
    internal class Startup : FunctionsStartup
    {
        private static readonly IConfigurationRoot _configuration = new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddUserSecrets<Startup>(true)
            .Build();

        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.AddCommonFunctionServices(_configuration);

        }
    }
}
