namespace SourceDocumentCoordinator.Enums
{
    public enum ClearFromQueueReturnCodeEnum
    {
        Success = 0,
        Warning = 21,
        NoSuchJob = 47,
        CannotRemoveFromQueue = 48,
        QueueUnavailable = 60
    }
}

