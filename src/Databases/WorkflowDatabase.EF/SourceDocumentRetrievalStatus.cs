namespace WorkflowDatabase.EF
{
    public enum SourceDocumentRetrievalStatus
    {
        Started,    // Equivalent to SDRA Status 1 (Queued)
        Ready,       // Equivalent to SDRA Status 0 (success)
        FileGenerated
    }
}
