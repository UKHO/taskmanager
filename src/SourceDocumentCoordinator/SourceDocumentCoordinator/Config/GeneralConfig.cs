using System;
using System.IO;

namespace SourceDocumentCoordinator.Config
{
    public class GeneralConfig
    {
        public string CallerCode { get; set; }
        public string SourceDocumentCoordinatorName { get; set; }
        public string LocalDbServer { get; set; }
        public ConnectionStrings ConnectionStrings { get; set; }
        public int SourceDocumentCoordinatorQueueStatusIntervalSeconds { get; set; }
        public string SourceDoumentWriteableFolderName { get; set; }

        public string GetSourceDoumentWriteableFolderFullPath(int sdocId)
        {
            return Path.Combine(this.SourceDoumentWriteableFolderName,sdocId.ToString());
        }

    }

    public class ConnectionStrings
    {
        public Uri AzureDbTokenUrl { get; set; }
    }
}
