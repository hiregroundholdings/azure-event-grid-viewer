namespace Viewer.Web.Controllers
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;

    [Route("api/[controller]")]
    public class MessagesController : Controller
    {
        private readonly EventGridEventProcessor _processor;

        public MessagesController(EventGridEventProcessor processor)
        {
            _processor = processor;
        }

        [HttpOptions]
        public async Task<IActionResult> Options()
        {
            return await _processor.HandleRequestAsync(Request);
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            return await _processor.HandleRequestAsync(Request);
        }
    }
}
