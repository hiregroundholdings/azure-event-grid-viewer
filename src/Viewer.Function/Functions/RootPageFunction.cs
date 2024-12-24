using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Viewer.Function.Functions
{
    public class RootPageFunction
    {
        private readonly ILogger<RootPageFunction> _logger;

        public RootPageFunction(ILogger<RootPageFunction> logger)
        {
            _logger = logger;
        }

        [Function("RootPageFunction")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
