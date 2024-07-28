using System;
using DZarsky.CommonLibraries.AzureFunctions.Extensions;
using DZarsky.NotesFunction.Configuration;
using DZarsky.NotesFunction.General;
using DZarsky.NotesFunction.Infrastructure.Handlers;
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
            Topic = configuration.GetValueFromContainer<string>("FCM:Topic"),
            BaseUrl = configuration.GetValueFromContainer<string>("FCM:BaseUrl"),
            ProjectId = configuration.GetValueFromContainer<string>("FCM:ProjectId")
        };

        if (!config.IsEnabled)
        {
            return builder;
        }

        builder.Services.AddTransient<FirebaseClientAuthHandler>();

        builder.Services.AddHttpClient(HttpClients.FirebaseCloudMessagingClient, client =>
        {
            client.BaseAddress = new Uri(string.Concat(config.BaseUrl, config.ProjectId));
        }).AddHttpMessageHandler<FirebaseClientAuthHandler>();

        builder.Services.AddSingleton(config);

        return builder;
    }
}
