namespace Viewer.Function
{
    using Azure.Data.Tables;
    using Azure.Monitor.OpenTelemetry.AspNetCore;
    using Azure.Monitor.OpenTelemetry.Exporter;
    using Microsoft.Azure.Functions.Worker.Builder;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using OpenTelemetry;
    using OpenTelemetry.Trace;
    using Viewer.Function.Constants;
    using Viewer.Hubs;

    public class Program
    {
        protected Program() { }

        public static async Task Main(string[] args)
        {
            FunctionsApplicationBuilder builder = FunctionsApplication.CreateBuilder(args);
            TracerProvider tracerProvider = CreateTracerProvider(builder);

            builder.Configuration.AddUserSecrets<Program>(optional: true, reloadOnChange: true);

            builder
                .ConfigureFunctionsWebApplication();

            ConfigureServices(builder);

            ILogger? logger = null;
            IHost host;
            try
            {
                host = builder.Build();
                logger = host.Services.GetRequiredService<ILogger<Program>>();
            }
            catch (Exception ex)
            {
                logger ??= CreateStartupLogger(builder);
                logger.LogError(ex, "Building host has failed.");
                throw;
            }

            try
            {
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Running host has failed.");
                throw;
            }

            tracerProvider.Dispose();
        }

        private static ILogger<Program> CreateStartupLogger(IHostApplicationBuilder hostApplicationBuilder)
        {
            using ILoggerFactory loggerFactory = LoggerFactory.Create(loggingBuilder =>
            {
                loggingBuilder.AddConsole(consoleLoggerOptions =>
                {
                    consoleLoggerOptions.LogToStandardErrorThreshold = LogLevel.Error;
                });

                loggingBuilder
                    .AddOpenTelemetry(otlOptions =>
                    {
                        if (hostApplicationBuilder.Configuration.GetValue<string>(AppSettingName.ApplicationInsightsConnectionString) is string appInsightsConnectionString)
                        {
                            otlOptions.AddAzureMonitorLogExporter(options =>
                            {
                                options.ConnectionString = appInsightsConnectionString;
                            });
                        }
                    });
            });

            return loggerFactory.CreateLogger<Program>();
        }

        private static TracerProvider CreateTracerProvider(IHostApplicationBuilder applicationBuilder)
        {
            TracerProviderBuilder tracerProviderBuilder = Sdk.CreateTracerProviderBuilder();

            if (applicationBuilder.Configuration.GetValue<string>(AppSettingName.ApplicationInsightsConnectionString) is string appInsightsConnectionString)
            {
                tracerProviderBuilder.AddAzureMonitorTraceExporter(options =>
                {
                    options.ConnectionString = appInsightsConnectionString;
                });
            }

            return tracerProviderBuilder.Build();
        }

        private static void ConfigureServices(FunctionsApplicationBuilder builder)
        {
            // Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
            OpenTelemetryBuilder openTelemetryBuilder = builder.Services
                .AddOpenTelemetry();

            if (builder.Configuration.GetValue<string>(AppSettingName.ApplicationInsightsConnectionString) is string appInsightsConnectionString)
            { 
                openTelemetryBuilder.UseAzureMonitor(options =>
                {
                    options.ConnectionString = appInsightsConnectionString;
                });
            }

            builder.Services.AddGridEventsHubClient(options =>
            {
                builder.Configuration.Bind("GridEventsHubClient", options);
            });

            builder.Services.AddSingleton<IGridEventRepository>(sp =>
            {
                string? tableConnectionString = builder.Configuration.GetValue<string>("Azure:Table:ConnectionString");
                if (string.IsNullOrWhiteSpace(tableConnectionString))
                {
                    throw new InvalidOperationException("The TableClient connection string is not set.");
                }

                TableClient tableClient = new(tableConnectionString, "grid_events");
                return new GridEventTableRepository(tableClient);
            });

            builder.Services.AddTransient<EventGridEventProcessor>();
        }
    }
}
