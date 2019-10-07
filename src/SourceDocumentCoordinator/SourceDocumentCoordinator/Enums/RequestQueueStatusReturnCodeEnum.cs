namespace SourceDocumentCoordinator.Enums
{
    public enum RequestQueueStatusReturnCodeEnum
    {
        Success = 0,
        Queued = 1,
        NotGeoreferenced = 20,
        FolderNotWritable = 40,
        NoDocumentFound = 42,
        NotSuitableForConversion = 43,
        QueueInsertionFailed = 44,
        ConversionFailed = 45,
        ConversionTimeOut = 46
    }
}

