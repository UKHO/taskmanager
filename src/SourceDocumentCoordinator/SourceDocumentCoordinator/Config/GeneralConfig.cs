﻿using System;

namespace SourceDocumentCoordinator.Config
{
    public class GeneralConfig
    {
        public string CallerCode { get; set; }
        public string SourceDocumentCoordinatorName { get; set; }
        public string LocalDbServer { get; set; }
        public ConnectionStrings ConnectionStrings { get; set; }
        public int SourceDocumentCoordinatorQueueStatusIntervalSeconds { get; set; }
        public string SourceDocumentWriteableFolderName { get; set; }
        public string EventServiceName { get; set; }
    }

    public class ConnectionStrings
    {
        public Uri AzureDbTokenUrl { get; set; }
    }
}
