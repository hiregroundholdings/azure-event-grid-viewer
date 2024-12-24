namespace Viewer.Hubs
{
    using Azure.Messaging.WebPubSub;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using System;

    public static class GridEventsHubClientExtensions
    {
        public static IServiceCollection AddGridEventsHubClient(this IServiceCollection services, Action<GridEventsHubClientOptions> configureOptions, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
        {
            services.AddKeyedSingleton(nameof(GridEventsHubClient), (sp, _) =>
            {
                GridEventsHubClientOptions options = new();
                configureOptions.Invoke(options);
                if (string.IsNullOrWhiteSpace(options.ConnectionString))
                {
                    throw new InvalidOperationException("The GridEventHub connection string is not defined.");
                }

                options.HubName ??= "gridevents";
                return new WebPubSubServiceClient(options.ConnectionString, options.HubName);
            });

            ServiceDescriptor serviceDescriptor = new(
                typeof(GridEventsHubClient),
                (sp) =>
                {
                    WebPubSubServiceClient webPubSubServiceClient = sp.GetRequiredKeyedService<WebPubSubServiceClient>(nameof(GridEventsHubClient));
                    return new GridEventsHubClient(webPubSubServiceClient, sp.GetRequiredService<ILogger<GridEventsHubClient>>());
                },
                serviceLifetime);

            services.Add(serviceDescriptor);
            return services;
        }
    }
}
