namespace WorkflowDatabase.EF
{
    public enum LinkedDocumentRetrievalStatus
    {
        NotAttached,
        Started,    // Equivalent to SDRA Status 1 (Queued)
        Ready,       // Equivalent to SDRA Status 0 (success)
        Complete

    }
}
