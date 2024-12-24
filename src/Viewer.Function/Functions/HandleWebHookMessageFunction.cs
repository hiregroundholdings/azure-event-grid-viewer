namespace Viewer.Function.Functions
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Extensions.Logging;

    public class HandleWebhookMessageFunction
    {
        internal const string FunctionName = "HandleWebHookMessage";

        private readonly EventGridEventProcessor _processor;
        private readonly ILogger _logger;

        public HandleWebhookMessageFunction(EventGridEventProcessor processor, ILogger<HandleWebhookMessageFunction> logger)
        {
            _processor = processor;
            _logger = logger;
        }

        [Function(FunctionName)]
        public async Task<IActionResult> HandleAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "options", "post", Route = "api/messages")] HttpRequest req,
            FunctionContext functionContext)
        {
            _logger.LogInformation("{FunctionName} received a {HttpMethod} request.", functionContext.FunctionDefinition.Name, req.Method);
            
            return await _processor.HandleRequestAsync(req);
        }
    }
}