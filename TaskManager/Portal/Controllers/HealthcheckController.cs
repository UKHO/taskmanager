using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Portal.Controllers
{
    [Route("/")]
    [ApiController]
    public class HealthcheckController : ControllerBase
    {
        private readonly ILogger _logger;

        public HealthcheckController(ILogger<HealthcheckController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("healthcheck")]
        public IActionResult Get()
        {
            _logger.LogInformation("Healthcheck GET");
            return Ok(Assembly.GetExecutingAssembly().GetName().Name + " is ok.");
        }
    }
}