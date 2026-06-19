using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MSMQ.Messaging;
using System.Text;
using System.Security;
using System.Xml.Linq;

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


        [HttpPost("test")]
        public async Task<IActionResult> Test()
        {
            string xml = await new StreamReader(Request.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(xml))
                return BadRequest("Keine XML empfangen.");

            XDocument doc = XDocument.Parse(xml);

            string? clientId = doc.Descendants("ClientId").FirstOrDefault()?.Value;
            string? requestId = doc.Descendants("RequestId").FirstOrDefault()?.Value;

            if (string.IsNullOrWhiteSpace(clientId))
                return BadRequest("ClientId fehlt.");

            if (string.IsNullOrWhiteSpace(requestId))
                return BadRequest("RequestId fehlt.");

            bool clientOk = clientId == "CPP-CLIENT-001";

            string status = clientOk ? "OK" : "DENIED";
            string result = clientOk
                ? "ClientId wurde akzeptiert."
                : "ClientId ist nicht zugelassen.";

            string responseXml = $@"<?xml version=""1.0""?>
                <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                    <soap:Body>
                        <Response>
                            <ClientId>{SecurityElement.Escape(clientId)}</ClientId>
                            <RequestId>{SecurityElement.Escape(requestId)}</RequestId>
                            <Status>{status}</Status>
                            <Result>{SecurityElement.Escape(result)}</Result>
                        </Response>
                    </soap:Body>
                </soap:Envelope>";

            return Content(responseXml, "application/xml");
        }
    }
}