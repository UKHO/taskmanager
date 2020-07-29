﻿using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using SourceDocumentService.HttpClients;
using System.IO;
using System.Security;
using Microsoft.AspNetCore.Authorization;
using SourceDocumentService.Configuration;

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
                fileBytes = await _fileSystem.File.ReadAllBytesAsync(Path.Combine(filestorePath, filename));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, $"No permissions to read file: {filename}.");
                throw;
            }
            catch (SecurityException ex)
            {
                _logger.LogError(ex, $"No permissions to read file: {filename}.");
                throw;
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, $"Unable to find file: {filename}");
                throw;
            }

            // Post to Content Service
            var contentServiceId = await _contentServiceApiClient.Post(fileBytes, Path.GetFileName(filename));

            _logger.LogInformation($"PostSourceDocumentToContentService invoked with ProcessId {processId}, sdocId {sdocId} and filepath {filename}. " +
                                   $"Returned Guid: {contentServiceId}");

            return Ok(contentServiceId);
        }
    }
}