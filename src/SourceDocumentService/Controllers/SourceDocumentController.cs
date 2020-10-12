using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.IO.Abstractions;
using System.Security;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SourceDocumentService.Configuration;
using SourceDocumentService.HttpClients;

namespace SourceDocumentService.Controllers
{
    [ApiController]
    [Authorize(Policy = "ADRoleOnly")]
    public class SourceDocumentController : ControllerBase
    {
        private readonly ILogger<SourceDocumentController> _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IContentServiceApiClient _contentServiceApiClient;
        private readonly IConfigurationManager _configurationManager;

        public SourceDocumentController(ILogger<SourceDocumentController> logger, IFileSystem fileSystem, IContentServiceApiClient contentServiceApiClient,
            IConfigurationManager configurationManager)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _contentServiceApiClient = contentServiceApiClient;
            _configurationManager = configurationManager;
        }

        /// <summary>
        /// Reads source document from given DFS location and posts it to Content Service
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="sdocId"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        [Route("/SourceDocumentService/v1/PostSourceDocument/{processId}/{sdocId}/{filename}")]
        [HttpPost]
        public async Task<IActionResult> PostSourceDocumentToContentService([FromRoute][Required] int processId, [FromRoute][Required] int sdocId, [Required][FromRoute] string filename)
        {
            _logger.LogInformation($"{nameof(PostSourceDocumentToContentService)} invoked with ProcessId {processId}, " +
                                   $"sdocId {sdocId} and filepath {filename}.");

            byte[] fileBytes;

            var filestorePath = _configurationManager.GetAppSetting("FilestorePath");

            // Retrieve file from DFS
            try
            {
                _logger.LogInformation($"{nameof(PostSourceDocumentToContentService)}: Reading file content '{filename}'; " +
                                                $"from '{filestorePath}'; with ProcessId {processId}, sdocId {sdocId}.");

                fileBytes = await _fileSystem.File.ReadAllBytesAsync(Path.Combine(filestorePath, filename));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, $"{nameof(PostSourceDocumentToContentService)}: No permissions to read file: {filename}; " +
                                                    $"with ProcessId {processId}, sdocId {sdocId}.");
                throw;
            }
            catch (SecurityException ex)
            {
                _logger.LogError(ex, $"{nameof(PostSourceDocumentToContentService)}: No permissions to read file: {filename}; " +
                                                    $"with ProcessId {processId}, sdocId {sdocId}.");
                throw;
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, $"{nameof(PostSourceDocumentToContentService)}: Unable to find file: {filename}; " +
                                                    $"with ProcessId {processId}, sdocId {sdocId}");
                throw;
            }


            // Post to Content Service
            var contentServiceId = Guid.Empty;

            try
            {
                _logger.LogInformation($"{nameof(PostSourceDocumentToContentService)}: Uploading file content '{filename}' to contents service; " +
                                                    $"with ProcessId {processId}, sdocId {sdocId}.");

                contentServiceId = await _contentServiceApiClient.Post(fileBytes, Path.GetFileName(filename));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(PostSourceDocumentToContentService)}: Unable to find file: {filename}; " +
                                                    $"with ProcessId {processId}, sdocId {sdocId}");
                throw;
            }

            _logger.LogInformation($"{nameof(PostSourceDocumentToContentService)}: Successfully Uploaded file content '{filename}' to contents service; " +
                                                    $"with ProcessId {processId}, sdocId {sdocId}.");

            return Ok(contentServiceId);
        }
    }
}
