using System;
using DZarsky.CommonLibraries.AzureFunctions.Extensions;
using DZarsky.NotesFunction.Configuration;
using DZarsky.NotesFunction.General;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DZarsky.NotesFunction.Infrastructure;

public static class FirebaseConfigurationExtensions
{
    public static IFunctionsHostBuilder AddFirebase(this IFunctionsHostBuilder builder, IConfiguration configuration)
    {
        var config = new FirebaseCloudMessagingConfiguration
        {
            IsEnabled = configuration.GetValueFromContainer<bool>("FCM:IsEnabled"),
            ServerKey = configuration.GetValueFromContainer<string?>("FCM:ServerKey"),
            Topic = configuration.GetValueFromContainer<string>("FCM:Topic"),
            BaseUrl = configuration.GetValueFromContainer<string>("FCM:BaseUrl")
        };

        if (!config.IsEnabled)
        {
            return builder;
        }
            
        if (string.IsNullOrWhiteSpace(config.ServerKey))
        {
            throw new InvalidOperationException("FCM is enabled but no server key is configured");
        }

        builder.Services.AddHttpClient(HttpClients.FirebaseCloudMessagingClient, client =>
        {
            client.BaseAddress = new Uri(config.BaseUrl);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ServerKey}");
        });

        builder.Services.AddSingleton(config);

        return builder;
    }
}
