using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace EventService.Controllers
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
    }
}