namespace SourceDocumentCoordinator.Enums
{
    public enum RequestQueueStatusReturnCodeEnum
    {
        Success = 0,
        Queued = 1,
        NotGeoreferenced = 20,
        FolderNotWritable = 40,
        NotSuitableForConversion = 43,
        ConversionFailed = 45,
        ConversionTimeOut = 46
    }
}

