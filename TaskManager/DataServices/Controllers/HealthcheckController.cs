using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using SDRAAssessmentWebService;

namespace DataServices.Controllers
{
    [Route("/")]
    [ApiController]
    public class HealthcheckController : ControllerBase
    {
        public readonly SDRAAssessmentWebService.SDRAExternalInterfaceAssessmentWebServiceSoap _sdraAssessmentWebService;

        public readonly SDRADataAccessWebService.SDRAExternalInterfaceDataAccessWebServiceSoap _sdraDataAccessWebService;

        public HealthcheckController(SDRAAssessmentWebService.SDRAExternalInterfaceAssessmentWebServiceSoap sdraAssessmentWebService,
            SDRADataAccessWebService.SDRAExternalInterfaceDataAccessWebServiceSoap sdraDataAccessWebService)
        {
            _sdraAssessmentWebService = sdraAssessmentWebService;
            _sdraDataAccessWebService = sdraDataAccessWebService;
        }
        [HttpGet]
        [Route("healthcheck")]
        public IActionResult Get()
        {
            return Ok(Assembly.GetExecutingAssembly().GetName().Name + " is ok.");
        }

        [HttpGet]
        [Route("healthcheck/SDRAAssessment")]
        public IActionResult GetSDRAAssessmentWebService()
        {
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