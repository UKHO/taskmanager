using Microsoft.AspNetCore.Mvc;

namespace SourceDocumentService.Controllers
{
    [ApiController]
    public class SourceDocumentController : ControllerBase
    {
        [HttpGet("{id}")]
        [Route("/SourceDocumentService/v1/")]
        public string Get(int id)
        {
            return "Ok";
        }

        [Route("/SourceDocumentService/v1/")]
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }
    }
}
