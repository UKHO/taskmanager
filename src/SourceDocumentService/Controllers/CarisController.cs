using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SourceDocumentService.Controllers
{
    [ApiController]
    public class CarisController : ControllerBase
    {
        [Route("/CarisService/v1/GetNewWreckageId")]
        [HttpGet]
        public IActionResult GetNewWreckageId()
        {
            return Ok(int.MaxValue);
        }
    }
}
