using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace DataServices.Controllers
{
    [Route("/")]
    [ApiController]
    public class HealthcheckController : ControllerBase
    {
        [HttpGet]
        [Route("healthcheck")]
        public IActionResult Get()
        {
            return Ok(Assembly.GetExecutingAssembly().GetName().Name + " is ok.");
        }

        [HttpGet]
        [Route("healthcheck/sdra")]
        public IActionResult GetSdra()
        {
            return Ok("SDRA is ok.");
        }
    }
}