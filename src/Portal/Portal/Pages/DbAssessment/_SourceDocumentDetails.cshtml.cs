﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Factories;
using Common.Factories.Interfaces;
using Common.Messages.Enums;
using Common.Messages.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Portal.Configuration;
using Portal.HttpClients;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    public class _SourceDocumentDetailsModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly IOptions<UriConfig> _uriConfig;
        private readonly IEventServiceApiClient _eventServiceApiClient;
        private readonly IDocumentStatusFactory _documentStatusFactory;

        [BindProperty(SupportsGet = true)] public int ProcessId { get; set; }
        public AssessmentData Assessment { get; set; }
        public IEnumerable<LinkedDocuments> LinkedDocuments { get; set; }
        public IEnumerable<LinkedDocuments> AttachedLinkedDocuments { get; set; }
        public PrimaryDocumentStatus PrimaryDocumentStatus { get; set; }
        public Uri PrimaryDocumentContentServiceUri { get; set; }

        public _SourceDocumentDetailsModel(WorkflowDbContext dbContext,
            IOptions<UriConfig> uriConfig, IEventServiceApiClient eventServiceApiClient, IDocumentStatusFactory documentStatusFactory)
        {
            _dbContext = dbContext;
            _uriConfig = uriConfig;
            _eventServiceApiClient = eventServiceApiClient;
            _documentStatusFactory = documentStatusFactory;
        }

        public void OnGet()
        {
            GetPrimaryDocumentData();
            GetPrimaryDocumentStatus();
            GetLinkedDocuments();
            GetAttachedLinkedDocuments();
        }

        private void GetPrimaryDocumentData()
        {
            try
            {
                Assessment = _dbContext
                    .AssessmentData
                    .First(c => c.ProcessId == ProcessId);
            }
            catch (InvalidOperationException e)
            {
                // Log and throw, as we're unable to get assessment data
                e.Data.Add("OurMessage", "Unable to retrieve AssessmentData");
                Console.WriteLine(e);
                throw;
            }
        }

        private void GetLinkedDocuments()
        {
            try
            {
                LinkedDocuments = _dbContext
                    .LinkedDocument
                    .Where(c => c.ProcessId == ProcessId).ToList();
            }
            catch (ArgumentNullException e)
            {
                // Log and throw, as we're unable to get Linked Documents
                e.Data.Add("OurMessage", "Unable to retrieve Linked Documents");
                Console.WriteLine(e);
                throw;
            }
        }

        private void GetPrimaryDocumentStatus()
        {
            try
            {
                PrimaryDocumentStatus = _dbContext.PrimaryDocumentStatus.First(s => s.ProcessId == ProcessId);

                if (PrimaryDocumentStatus.ContentServiceId.HasValue)
                    PrimaryDocumentContentServiceUri =
                        _uriConfig.Value.BuildContentServiceUri(PrimaryDocumentStatus.ContentServiceId.Value);
            }
            catch (InvalidOperationException e)
            {
                // Log that we're unable to get a Source Doc Status row
                e.Data.Add("OurMessage", "Unable to retrieve PrimaryDocumentStatus");
                Console.WriteLine(e);
            }
        }

        private void GetAttachedLinkedDocuments()
        {
            if (LinkedDocuments != null && LinkedDocuments.Any())
            {
                AttachedLinkedDocuments = LinkedDocuments.Where(l =>
                    !l.Status.Equals(LinkedDocumentRetrievalStatus.NotAttached.ToString(),
                        StringComparison.OrdinalIgnoreCase));

                foreach (var attachedLinkedDocument in AttachedLinkedDocuments)
                {
                    if (attachedLinkedDocument.ContentServiceId.HasValue)
                        attachedLinkedDocument.ContentServiceUri =
                            _uriConfig.Value.BuildContentServiceUri(attachedLinkedDocument.ContentServiceId.Value);
                }
            }
        }

        public async Task<IActionResult> OnPostAttachLinkedDocumentAsync(int linkedSdocId, int processId, Guid correlationId)
        {
            // Update DB first, as it is the one used for populating Attached secondary sources
            await SourceDocumentHelper.UpdateSourceDocumentStatus(
                                                                    _documentStatusFactory,
                                                                    processId,
                                                                    linkedSdocId,
                                                                    SourceDocumentRetrievalStatus.Started,
                                                                    SourceDocumentType.Linked);

            var docRetrievalEvent = new InitiateSourceDocumentRetrievalEvent
            {
                CorrelationId = correlationId,
                ProcessId = processId,
                SourceDocumentId = linkedSdocId,
                GeoReferenced = false,
                DocumentType = SourceDocumentType.Linked
            };

            await _eventServiceApiClient.PostEvent(nameof(InitiateSourceDocumentRetrievalEvent),docRetrievalEvent);

            return StatusCode(200);
            // TODO: Log!
        }

        /// <summary>
        /// Result of user clicking the Add Source from SDRA button
        /// </summary>
        /// <param name="sdocId"></param>
        /// <param name="processId"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostAddSourceFromSdraAsync(int sdocId, int processId, Guid correlationId)
        {
            // Update DB first
            await SourceDocumentHelper.UpdateSourceDocumentStatus(
                _documentStatusFactory,
                processId,
                sdocId,
                SourceDocumentRetrievalStatus.Started,
                SourceDocumentType.Database);

            var docRetrievalEvent = new InitiateSourceDocumentRetrievalEvent
            {
                CorrelationId = correlationId,
                ProcessId = processId,
                SourceDocumentId = sdocId,
                GeoReferenced = false,
                DocumentType = SourceDocumentType.Database
            };

            await _eventServiceApiClient.PostEvent(nameof(InitiateSourceDocumentRetrievalEvent), docRetrievalEvent);

            return StatusCode(200);
            // TODO: Log!
        }
    }
}