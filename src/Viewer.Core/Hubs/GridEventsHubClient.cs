namespace Viewer.Hubs
{
    using Azure.Messaging.WebPubSub;
    using Polly.Retry;
    using Polly;
    using Viewer.Models;
    using Azure.Core;
    using Azure;
    using Microsoft.Extensions.Logging;

    public class GridEventsHubClient
    {
        private readonly WebPubSubServiceClient _webPubSubServiceClient;
        private readonly ILogger _logger;

        public GridEventsHubClient(WebPubSubServiceClient webPubSubServiceClient, ILogger<GridEventsHubClient> logger)
        {
            _webPubSubServiceClient = webPubSubServiceClient;
            _logger = logger;
            _logger.LogDebug("GridEventsHubClient initialized.");
        }

        private AsyncRetryPolicy WebPubSubServiceClientSendRetryPolicy => Policy
            .Handle<RequestFailedException>()
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (ex, retryAfter, retryAttempt, _) =>
                {
                    _logger.LogWarning(
                        ex,
                        "WebPubSub service client send operation failed {RetryAttempt} time(s) with error: {ErrorMessage}. Will retry in {BackOffInSeconds} seconds.",
                        retryAttempt,
                        ex.Message,
                        retryAfter.TotalSeconds);
                    return Task.CompletedTask;
                });

        public async Task<Uri> GetClientAccessUriAsync(string? userId = null)
        {
            _logger.LogInformation("Attempting to get client access URI for user '{UserId}'.", userId);
            Uri result = await _webPubSubServiceClient.GetClientAccessUriAsync(userId: userId);
            _logger.LogInformation("Successfully obtained client access URI for user '{UserId}'.", userId);
            return result;
        }

        public async Task PublishAsync<T>(CloudEvent<T> cloudEvent) where T : class
        {
            _logger.LogInformation("Publishing CloudEvent with ID '{EventId}' and type '{EventType}'.", cloudEvent.Id, cloudEvent.Type);
            RequestContent requestContent = CreateRequestContent(
                cloudEvent.Id,
                cloudEvent.Type,
                cloudEvent.Subject,
                cloudEvent.Time,
                cloudEvent.RawEvent);
            await PublishAsync(requestContent);
            _logger.LogInformation("Successfully published CloudEvent with ID '{EventId}'.", cloudEvent.Id);
        }

        public async Task PublishAsync<T>(GridEvent<T> gridEvent) where T : class
        {
            _logger.LogInformation("Publishing GridEvent with ID '{EventId}' and type '{EventType}'.", gridEvent.Id, gridEvent.EventType);
            RequestContent requestContent = CreateRequestContent(
                gridEvent.Id,
                gridEvent.EventType,
                gridEvent.Subject,
                gridEvent.EventTime,
                gridEvent.RawEvent);
            await PublishAsync(requestContent);
            _logger.LogInformation("Successfully published GridEvent with ID '{EventId}'.", gridEvent.Id);
        }

        private async Task PublishAsync(RequestContent requestContent)
        {
            _logger.LogDebug("Sending message to all subscribers.");
            await WebPubSubServiceClientSendRetryPolicy.ExecuteAsync(async () =>
            {
                await _webPubSubServiceClient.SendToAllAsync(requestContent, ContentType.ApplicationJson);
            });
            _logger.LogDebug("Message sent to all subscribers.");
        }

        private static RequestContent CreateRequestContent(string eventId, string eventType, string eventSubject, DateTimeOffset eventTimestamp, string eventPayload)
        {
            object content = new
            {
                id = eventId,
                eventType = eventType,
                subject = eventSubject,
                eventTime = eventTimestamp.ToString(),
                payload = eventPayload,
            };

            return RequestContent.Create(content);
        }
    }
}
