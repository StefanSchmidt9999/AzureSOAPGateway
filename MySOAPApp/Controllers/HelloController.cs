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
        private static string? _lastReceivedXml;
        private static DateTime? _lastReceivedTime;

        private static readonly Queue<string> _messages = new Queue<string>();
        private static readonly Queue<string> _responses = new Queue<string>();
        private static readonly object _lock = new object();

        //[HttpPost]
        //public async Task<IActionResult> Post()
        //{
        //    using var reader = new StreamReader(Request.Body);
        //    string xml = await reader.ReadToEndAsync();

        //    if (string.IsNullOrWhiteSpace(xml))
        //    {
        //        return BadRequest("Keine XML empfangen.");
        //    }

        //    lock (_lock)
        //    {
        //        _messages.Enqueue(xml);
        //    }

        //    return Ok();
        //}
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
                _lastReceivedXml = xml;
                _lastReceivedTime = DateTime.Now;

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

        [HttpGet("last")]
        public IActionResult Last()
        {
            lock (_lock)
            {
                if (string.IsNullOrWhiteSpace(_lastReceivedXml))
                {
                    return Content("Noch keine XML empfangen.", "text/plain");
                }

                string html = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset=""utf-8"" />
                        <title>Letzte empfangene SOAP/XML</title>
                    </head>
                    <body>
                        <h1>Letzte empfangene SOAP/XML</h1>

                        <p><strong>Zeit:</strong> {_lastReceivedTime}</p>

                        <pre style=""border:1px solid #999; padding:10px; background:#f4f4f4;"">{SecurityElement.Escape(_lastReceivedXml)}</pre>
                    </body>
                    </html>";

                return Content(html, "text/html; charset=utf-8");
            }
        }
        [HttpPost("response")]
        public async Task<IActionResult> Response()
        {
            using var reader = new StreamReader(Request.Body);
            string xml = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(xml))
                return BadRequest("Keine XML empfangen.");

            lock (_lock)
            {
                _responses.Enqueue(xml);
            }

            return Content("<ok/>", "application/xml");
        }

        [HttpGet("response/status")]
        public IActionResult ResponseStatus()
        {
            lock (_lock)
            {
                return Content( $"Responses in Queue: {_responses.Count}", "text/plain");
            }
        }

        [HttpGet("response")]
        public IActionResult GetResponse()
        {
            string? xml = null;

            lock (_lock)
            {
                if (_responses.Count > 0)
                {
                    xml = _responses.Dequeue();
                }
            }

            if (xml == null)
                return NoContent();

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
            string? commandId = doc.Descendants("CommandId").FirstOrDefault()?.Value;
            string? storedProcedureId = doc.Descendants("StoredProcedureId").FirstOrDefault()?.Value;
            string? commandText = doc.Descendants("CommandText").FirstOrDefault()?.Value;

            if (string.IsNullOrWhiteSpace(clientId))
                return BadRequest("ClientId fehlt.");

            if (string.IsNullOrWhiteSpace(requestId))
                return BadRequest("RequestId fehlt.");

            if (string.IsNullOrWhiteSpace(commandId))
                return BadRequest("CommandId fehlt.");

            if (string.IsNullOrWhiteSpace(storedProcedureId))
                return BadRequest("StoredProcedureId fehlt.");

            bool clientOk = clientId == "CPP-CLIENT-001";

            string status = clientOk ? "OK" : "DENIED";

            string result = clientOk
                ? $"ClientId akzeptiert. CommandId={commandId}, StoredProcedureId={storedProcedureId}, CommandText={commandText}"
                : "ClientId ist nicht zugelassen.";

            string responseXml = $@"<?xml version=""1.0""?>
                <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                    <soap:Body>
                        <Response>
                            <ClientId>{SecurityElement.Escape(clientId)}</ClientId>
                            <RequestId>{SecurityElement.Escape(requestId)}</RequestId>
                            <Command>
                                <CommandId>{SecurityElement.Escape(commandId)}</CommandId>
                                <StoredProcedureId>{SecurityElement.Escape(storedProcedureId)}</StoredProcedureId>
                                <CommandText>{SecurityElement.Escape(commandText ?? "")}</CommandText>
                            </Command>
                            <Status>{status}</Status>
                            <Result>{SecurityElement.Escape(result)}</Result>
                        </Response>
                    </soap:Body>
                </soap:Envelope>";

            return Content(responseXml, "application/xml");
        }
    }
}