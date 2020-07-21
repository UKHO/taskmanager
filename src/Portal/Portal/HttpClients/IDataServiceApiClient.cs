using System;
using System.Net;
using System.Threading.Tasks;
using DataServices.Models;

namespace Portal.HttpClients
{
    public interface IDataServiceApiClient
    {
        Task<(DocumentAssessmentData assessmentData, HttpStatusCode httpStatusCode, string errorMessage, Uri fullUri)> GetAssessmentData(int sdocId);
    }
}