namespace Viewer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using Viewer.Models;

    public static class EventGridHelper
    {
        private static readonly JsonSerializerOptions serializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public static bool TryParseGridEvents<T>(string jsonContent, [NotNullWhen(true)] out IEnumerable<GridEvent<T>> events)
            where T : class
        {
            List<GridEvent<T>> results = new();
            events = results;
            JsonNode? node = JsonNode.Parse(jsonContent);
            if (node is JsonArray nodeArray)
            {
                foreach (JsonNode? @event in nodeArray)
                {
                    if (@event?.Deserialize<GridEvent<T>>(serializerOptions) is GridEvent<T> details)
                    {
                        details.RawEvent = @event.ToString();
                        results.Add(details);
                    }
                }
            }
            else if (node is JsonObject jsonObject)
            {
                try
                {
                    GridEvent<T> gridEvent = jsonObject.Deserialize<GridEvent<T>>(serializerOptions)!;
                    gridEvent.RawEvent = jsonObject.ToString();
                    results.Add(gridEvent);
                }
                catch (JsonException)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool TryParseEventGridValidationCode(string jsonContent, out string? validationCode)
        {
            if (!TryParseGridEvents(jsonContent, out IEnumerable<GridEvent<Dictionary<string, string>>>? events))
            {
                validationCode = null;
                return false;
            }

            GridEvent<Dictionary<string, string>>? gridEvent = events.FirstOrDefault();

            // Retrieve the validation code and echo back.
            validationCode = gridEvent?.Data["validationCode"];
            return true;
        }

        public static bool TryParseCloudEvent<T>(string jsonContent, [NotNullWhen(true)] out CloudEvent<T>? cloudEvent)
            where T : class
        {
            JsonNode? parsedJson;
            try
            {
                parsedJson = JsonNode.Parse(jsonContent);
                cloudEvent = parsedJson?.Deserialize<CloudEvent<T>>(serializerOptions) ?? new();
            }
            catch (JsonException)
            {
                cloudEvent = new CloudEvent<T>();
                return false;
            }

            cloudEvent.RawEvent = parsedJson?.ToString() ?? jsonContent;
            return true;
        }

        public static bool IsCloudEvent(string jsonContent)
        {
            // Cloud events are sent one at a time, while Grid events
            // are sent in an array. As a result, the JObject.Parse will 
            // fail for Grid events. 
            try
            {
                // Attempt to read one JSON object. 
                JsonNode? eventData = JsonNode.Parse(jsonContent);

                // Check for the spec version property.
                string? specVersion = eventData?["specversion"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(specVersion))
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }

            return false;
        }
    }
}
