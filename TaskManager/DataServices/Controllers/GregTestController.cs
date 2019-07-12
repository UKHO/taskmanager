using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SDRAAssessmentWebService;

namespace DataServices.Controllers
{
    [Route("/")]
    [ApiController]
    public class GregTestController : ControllerBase
    { 
    
        private readonly ILogger _logger;

        public GregTestController(ILogger<GregTestController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("gregtest")]
        public IActionResult Get()
        {
            _logger.LogInformation("!!TOM TEST INFORMATION!!");
            _logger.LogWarning("!!TOM TEST WARNING!!");
            _logger.LogError("!!TOM TEST ERROR!!");
            try
            {
                throw new Exception("BAD THINGS");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "bad things happened");
            }
            throw new Exception("REALLY BAD THINGS");
            return Ok(Assembly.GetExecutingAssembly().GetName().Name + " is ok.");
        }
    }
}