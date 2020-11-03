namespace WorkflowDatabase.EF.Interfaces
{
    public interface IProductActionData
    {
        bool ProductActioned { get; set; }
        string ProductActionChangeDetails { get; set; }
        bool SncActioned { get; set; }
        string SncActionChangeDetails { get; set; }
    }
}
