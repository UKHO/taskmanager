namespace WorkflowDatabase.EF
{
    public enum SourceDocumentRetrievalStatus
    {
        NotAttached,
        Started,    // Equivalent to SDRA Status 1 (Queued)
        Ready,       // Equivalent to SDRA Status 0 (success)
        FileGenerated,
        Assessed,
        Completed
    }
}
