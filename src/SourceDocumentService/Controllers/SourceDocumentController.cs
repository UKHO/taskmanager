using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace SourceDocumentService.Controllers
{
    [ApiController]
    public class SourceDocumentController : ControllerBase
    {
        readonly ILogger<SourceDocumentController> _logger;

        public SourceDocumentController(ILogger<SourceDocumentController> logger)
        {
            _logger = logger;
        }

        [HttpGet("{id}")]
        [Route("/SourceDocumentService/v1/")]
        public string Get()
        {
            _logger.LogInformation("GET accessed at " + DateTime.Now);
            return "Ok";
        }

        [Route("/SourceDocumentService/v1/")]
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }
    }
}
