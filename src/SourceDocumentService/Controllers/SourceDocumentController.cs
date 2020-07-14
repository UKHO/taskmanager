using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Net;
using SourceDocumentService.HttpClients;
using System.IO;
using System.Security;
using Serilog.Context;

namespace SourceDocumentService.Controllers
{
    [ApiController]
    public class SourceDocumentController : ControllerBase
    {
        private readonly ILogger<SourceDocumentController> _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IContentServiceApiClient _contentServiceApiClient;

        public SourceDocumentController(ILogger<SourceDocumentController> logger, IFileSystem fileSystem, IContentServiceApiClient contentServiceApiClient)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _contentServiceApiClient = contentServiceApiClient;
        }

        [HttpGet("{id}")]
        [Route("/SourceDocumentService/v1/")]
        public string Get()
        {
            _logger.LogInformation("GET accessed at " + DateTime.Now);
            return "Ok";
        }

        /// <summary>
        /// Reads source document from given DFS location and posts it to Content Service
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="sdocId"></param>
        /// <param name="filepath"></param>
        /// <returns></returns>
        [Route("/SourceDocumentService/v1/PostSourceDocument/{processId}/{sdocId}")]
        [HttpPost]
        public async Task<IActionResult> PostSourceDocumentToContentService([FromRoute][Required] int processId, [FromRoute][Required] int sdocId, [FromBody][Required] string filepath)
        {
            _logger.LogInformation($"{nameof(PostSourceDocumentToContentService)} invoked with ProcessId {processId}, " +
                                   $"sdocId {sdocId} and filepath {filepath}.");

            byte[] fileBytes = { };

            // Retrieve file from DFS
            try
            {
                fileBytes = await _fileSystem.File.ReadAllBytesAsync(filepath);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, $"No permissions to reed file: {filepath}.");
                throw;
            }
            catch (SecurityException ex)
            {
                _logger.LogError(ex, $"No permissions to reed file: {filepath}.");
                throw;
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, $"Unable to find file: {filepath}");
                throw;
            }

            // Post to Content Service
            var contentServiceId = await _contentServiceApiClient.Post(fileBytes, Path.GetFileName(filepath));

            _logger.LogInformation($"PostSourceDocumentToContentService invoked with ProcessId {processId}, sdocId {sdocId} and filepath {filepath}.");

            return new ObjectResult(contentServiceId);
        }
    }
}
