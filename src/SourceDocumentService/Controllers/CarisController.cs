using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SourceDocumentService.Helpers;

namespace SourceDocumentService.Controllers
{
    [ApiController]
    public class CarisController : ControllerBase
    {
        private readonly ILogger<CarisController> _logger;
        private readonly ICuiaDatabaseHelper _cuiaDatabaseHelper;

        public CarisController(ILogger<CarisController> logger, ICuiaDatabaseHelper cuiaDatabaseHelper)
        {
            _logger = logger;
            _cuiaDatabaseHelper = cuiaDatabaseHelper;
        }

        [Route("/CarisService/v1/GetNewWreckageId")]
        [HttpGet]
        public async Task<IActionResult> GetNewWreckageId()
        {
            _logger.LogInformation($"{nameof(GetNewWreckageId)} invoked.");

            var result = 0;

            try
            {
                _logger.LogInformation($"{nameof(GetNewWreckageId)}: retrieving next value from wreck id sequence.");

                result = await _cuiaDatabaseHelper.GetNextWreckIdAsync();

                _logger.LogInformation($"{nameof(GetNewWreckageId)}: successfully retrieved next value from wreck id sequence {result}.");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex,$"{nameof(GetNewWreckageId)}: failed to retrieve next value from wreck id sequence.");

                return StatusCode(500, $"Failed to get next number for wreck id: {ex.Message}");
            }

            return Ok(result);
        }
    }
}
