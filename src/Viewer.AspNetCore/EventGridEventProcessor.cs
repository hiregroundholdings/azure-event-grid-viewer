namespace Viewer
{
    using Microsoft.Extensions.Primitives;
    using System.Text;
    using Viewer.Models;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Viewer.Hubs;
    using Microsoft.Extensions.Logging;

    public class EventGridEventProcessor
    {
        private readonly IGridEventRepository _eventRepository;
        private readonly GridEventsHubClient _hubClient;
        private readonly ILogger _logger;

        public EventGridEventProcessor(
            IGridEventRepository eventRepository,
            GridEventsHubClient hubClient,
            ILogger<EventGridEventProcessor> logger)
        {
            _eventRepository = eventRepository;
            _hubClient = hubClient;
            _logger = logger;
        }

        public static bool IsSubcriptionValidationEvent(HttpRequest req)
        {
            if (req.Headers.TryGetValue("aeg-event-type", out StringValues headerValue) && headerValue == "SubscriptionValidation")
            {
                return true;
            }

            return false;
        }

        public static bool IsNotificationEvent(HttpRequest req)
        {
            if (req.Headers.TryGetValue("aeg-event-type", out StringValues headerValue) && headerValue == "Notification")
            {
                return true;
            }

            return false;
        }

        public async Task<IActionResult> HandleRequestAsync(HttpRequest req)
        {
            _logger.LogInformation("Handling request: {Method} {Path}", req.Method, req.Path);

            if (HttpMethods.IsOptions(req.Method))
            {
                string? webHookRequestOrigin = req.Headers["WebHook-Request-Origin"].FirstOrDefault();
                string? webHookRequestCallback = req.Headers["WebHook-Request-Callback"];
                string? webHookRequestRate = req.Headers["WebHook-Request-Rate"];
                _logger.LogInformation(
                    "Processing OPTIONS request.\nWebHook Request Origin: {WebHookRequestOrigin}\nWebHook Request Callback: {WebHookRequestCallback}\nWebHook Request Rate: {WebHookRequestRate}",
                    webHookRequestOrigin,
                    webHookRequestCallback,
                    webHookRequestRate);

                req.HttpContext.Response.Headers.TryAdd("WebHook-Allowed-Rate", "*");
                req.HttpContext.Response.Headers.TryAdd("WebHook-Allowed-Origin", webHookRequestOrigin);

                _logger.LogInformation("Set WebHook-Allowed-Origin to {Origin}", webHookRequestOrigin);

                return new OkResult();
            }

            if (!HttpMethods.IsPost(req.Method))
            {
                _logger.LogWarning("Method not allowed: {Method}", req.Method);
                return new StatusCodeResult(StatusCodes.Status405MethodNotAllowed);
            }

            string jsonContent = await GetJsonContentAsync(req);
            _logger.LogDebug("Received JSON content: {JsonContent}", jsonContent);

            if (IsSubcriptionValidationEvent(req))
            {
                _logger.LogInformation("Handling SubscriptionValidation event.");
                return await HandleSubscriptionValidationEventAsync(jsonContent);
            }
            else if (IsNotificationEvent(req))
            {
                _logger.LogInformation("Handling Notification event.");
                return await HandleNotificationEventAsync(jsonContent);
            }

            _logger.LogWarning("Unknown event type received.");
            return new BadRequestResult();
        }

        private async Task<IActionResult> HandleNotificationEventAsync(string jsonContent)
        {
            _logger.LogInformation("Processing notification event.");

            if (EventGridHelper.IsCloudEvent(jsonContent))
            {
                _logger.LogInformation("Detected CloudEvent format.");
                return await HandleCloudEvent(jsonContent);
            }
            else
            {
                _logger.LogInformation("Detected GridEvents format.");
                return await HandleGridEvents(jsonContent);
            }
        }

        private async Task<IActionResult> HandleSubscriptionValidationEventAsync(string jsonContent)
        {
            _logger.LogInformation("Processing subscription validation event.");

            if (!EventGridHelper.TryParseGridEvents(jsonContent, out IEnumerable<GridEvent<Dictionary<string, string>>> events))
            {
                _logger.LogError("Failed to parse GridEvents from JSON content.");
                return new BadRequestResult();
            }

            GridEvent<Dictionary<string, string>> gridEvent = events.First();
            _logger.LogInformation("Parsed subscription validation event with ID: {EventId}", gridEvent.Id);

            try
            {
                await _eventRepository.AddAsync(gridEvent);
                _logger.LogInformation("Stored event with ID: {EventId}", gridEvent.Id);
            }
            catch (DuplicateResourceException)
            {
                _logger.LogWarning("Event with ID {EventId} already stored.", gridEvent.Id);
            }

            await _hubClient.PublishAsync(gridEvent);
            _logger.LogInformation("Published event with ID: {EventId} to hub.", gridEvent.Id);

            string validationCode = gridEvent.Data["validationCode"];
            _logger.LogInformation("Validation code: {ValidationCode}", validationCode);

            return new JsonResult(new
            {
                validationResponse = validationCode
            });
        }

        private async Task<IActionResult> HandleGridEvents(string jsonContent, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing GridEvents.");

            if (!EventGridHelper.TryParseGridEvents(jsonContent, out IEnumerable<GridEvent<dynamic>> events))
            {
                _logger.LogError("Failed to parse GridEvents from JSON content.");
                return new BadRequestResult();
            }

            _logger.LogInformation("Parsed {EventCount} GridEvents.", events.Count());

            cancellationToken.ThrowIfCancellationRequested();

            foreach (GridEvent<dynamic> gridEvent in events)
            {
                _logger.LogInformation("Processing GridEvent with ID: {EventId}", gridEvent.Id);

                try
                {
                    await _eventRepository.AddAsync(gridEvent, CancellationToken.None);
                    _logger.LogInformation("Stored GridEvent with ID: {EventId}", gridEvent.Id);
                }
                catch (DuplicateResourceException)
                {
                    _logger.LogWarning("GridEvent with ID {EventId} already stored.", gridEvent.Id);
                }

                await _hubClient.PublishAsync(gridEvent);
                _logger.LogInformation("Published GridEvent with ID: {EventId} to hub.", gridEvent.Id);
            }

            return new OkResult();
        }

        public async Task<IActionResult> HandleCloudEvent(string jsonContent)
        {
            _logger.LogInformation("Processing CloudEvent.");

            if (!EventGridHelper.TryParseCloudEvent(jsonContent, out CloudEvent<dynamic>? cloudEvent))
            {
                _logger.LogError("Failed to parse CloudEvent from JSON content.");
                return new BadRequestResult();
            }

            _logger.LogInformation("Parsed CloudEvent with ID: {EventId}", cloudEvent.Id);

            try
            {
                await _eventRepository.AddAsync(cloudEvent);
                _logger.LogInformation("Stored CloudEvent with ID: {EventId}", cloudEvent.Id);
            }
            catch (DuplicateResourceException)
            {
                _logger.LogWarning("CloudEvent with ID {EventId} already stored.", cloudEvent.Id);
            }

            await _hubClient.PublishAsync(cloudEvent);
            _logger.LogInformation("Published CloudEvent with ID: {EventId} to hub.", cloudEvent.Id);

            return new OkResult();
        }

        private static async Task<string> GetJsonContentAsync(HttpRequest req)
        {
            using StreamReader sr = new(req.Body, Encoding.UTF8);
            string jsonContent = await sr.ReadToEndAsync();
            return jsonContent;
        }
    }
}
