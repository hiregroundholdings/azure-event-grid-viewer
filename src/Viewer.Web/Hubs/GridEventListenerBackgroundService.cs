#nullable enable

namespace Viewer
{
    using Azure.Messaging.WebPubSub.Clients;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Viewer.Hubs;
    using Viewer.Web.Hubs;

    public class GridEventListenerBackgroundService : BackgroundService
    {
        private readonly GridEventsHubClient _hubClient;
        private readonly IHubContext<GridEventsHub> _hubContext;
        private readonly ILogger _logger;

        private WebPubSubClient? _webPubSubClient;

        public GridEventListenerBackgroundService(
            GridEventsHubClient hubClient,
            IHubContext<GridEventsHub> hubContext,
            ILogger<GridEventListenerBackgroundService> logger)
        {
            _hubClient = hubClient;
            _hubContext = hubContext;
            _logger = logger;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            Uri clientAccessUri = await _hubClient.GetClientAccessUriAsync();
            _webPubSubClient ??= new(clientAccessUri);
            _webPubSubClient.GroupMessageReceived += WebPubSubClient_GroupMessageReceived;
            _webPubSubClient.ServerMessageReceived += WebPubSubClient_ServerMessageReceived;

            cancellationToken.ThrowIfCancellationRequested();
            await _webPubSubClient.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Yield();
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping HubListenerBackgroundService...");
            if (_webPubSubClient is not null)
            {
                await _webPubSubClient.StopAsync();
                await _webPubSubClient.DisposeAsync();
            }

            await base.StopAsync(cancellationToken);
        }

        private async Task WebPubSubClient_ServerMessageReceived(WebPubSubServerMessageEventArgs arg)
        {
            _logger.LogDebug("WebPubSubClient server message received");
            await _hubContext.Clients.All.SendAsync("gridupdate", arg.Message.Data);
        }

        private async Task WebPubSubClient_GroupMessageReceived(WebPubSubGroupMessageEventArgs arg)
        {
            _logger.LogDebug("WebPubSubClient message received for group {GroupId}", arg.Message.Group);
            await _hubContext.Clients.All.SendAsync("gridupdate", arg.Message.Data);
        }
    }
}
