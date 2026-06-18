using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MSMQ.Messaging;
using System.Text;

//namespace MySOAPApp.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class HelloController : ControllerBase
//    {
//        private static string _lastXml;
//        private static readonly object _lock = new object();

//        // Blazor sendet SOAP XML hierhin
//        [HttpPost]
//        public async Task<IActionResult> Post()
//        {
//            using var reader = new StreamReader(Request.Body);
//            string xml = await reader.ReadToEndAsync();

//            lock (_lock)
//            {
//                _lastXml = xml;
//            }

//            return Ok();

//            return Ok();
//        }

//        // Server holt exakt das gleiche XML
//        [HttpGet]
//        public IActionResult Get()
//        {
//            string? xml;

//            lock (_lock)
//            {
//                if (string.IsNullOrEmpty(_lastXml))
//                    return Content("Keine XML", "text/plain");

//                xml = _lastXml;
//                _lastXml = null;
//            }

//            return Content(xml, "application/xml");
//        }

//        [HttpGet("ping")]
//        public IActionResult Ping()
//        {
//            return Content("OK", "text/plain");
//        }
//    }
//}

namespace MySOAPApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HelloController : ControllerBase
    {
        private static readonly Queue<string> _messages = new Queue<string>();
        private static readonly object _lock = new object();

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            using var reader = new StreamReader(Request.Body);
            string xml = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(xml))
            {
                return BadRequest("Keine XML empfangen.");
            }

            lock (_lock)
            {
                _messages.Enqueue(xml);
            }

            return Ok();
        }

        [HttpGet]
        public IActionResult Get()
        {
            string? xml = null;

            lock (_lock)
            {
                if (_messages.Count > 0)
                {
                    xml = _messages.Dequeue();
                }
            }

            if (xml == null)
            {
                return NoContent();
            }

            return Content(xml, "application/xml");
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Content("OK", "text/plain");
        }
    }
}