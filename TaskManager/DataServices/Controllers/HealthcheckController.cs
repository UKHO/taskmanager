using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SDRAAssessmentWebService;

namespace DataServices.Controllers
{
    [Route("/")]
    [ApiController]
    public class HealthcheckController : ControllerBase
    {
        public readonly SDRAAssessmentWebService.SDRAExternalInterfaceAssessmentWebServiceSoap _sdraAssessmentWebService;

        public readonly SDRADataAccessWebService.SDRAExternalInterfaceDataAccessWebServiceSoap _sdraDataAccessWebService;

        private readonly ILogger _logger;

        public HealthcheckController(SDRAAssessmentWebService.SDRAExternalInterfaceAssessmentWebServiceSoap sdraAssessmentWebService,
            SDRADataAccessWebService.SDRAExternalInterfaceDataAccessWebServiceSoap sdraDataAccessWebService,
            ILogger<HealthcheckController> logger)
        {
            _sdraAssessmentWebService = sdraAssessmentWebService;
            _sdraDataAccessWebService = sdraDataAccessWebService;
            _logger = logger;
        }

        [HttpGet]
        [Route("healthcheck")]
        public IActionResult Get()
        {
            _logger.LogInformation("Healthcheck GET");
            return Ok(Assembly.GetExecutingAssembly().GetName().Name + " is ok.");
        }

        [HttpGet]
        [Route("healthcheck/SDRAAssessment")]
        public IActionResult GetSDRAAssessmentWebService()
        {
            _logger.LogInformation("Healthcheck/SDRAAssessment GET");
            try
            {
                //TODO: Change the SDRA method to request to something less intensive
                var result = _sdraAssessmentWebService.GetDocumentsForAssessmentAsync(
                    new GetDocumentsForAssessmentRequest()).Result;

                return Ok("SDRAAssessmentWebService is ok.");
            }
            catch (Exception e)
            {
                return base.StatusCode(500, $"SDRAAssessmentWebService is NOT ok. {e.ToString()}");
            }
            
            
        }

        [HttpGet]
        [Route("healthcheck/SDRAData")]
        public IActionResult GetSDRADataAccessWebService()
        {
            _logger.LogInformation("Healthcheck/SDRAData GET");
            try
            {
                //TODO: Change the SDRA method to request to something less intensive
                var result = _sdraDataAccessWebService.GetAllDocumentTypesAsync().Result;

                return Ok("GetSDRADataAccessWebService is ok.");
            }
            catch (Exception e)
            {
                return base.StatusCode(500, $"GetSDRADataAccessWebService is NOT ok. {e.ToString()}");
            }
        }
    }
}