namespace WorkflowDatabase.EF.Models
{
    public interface IOperatorData
    {
        AdUser Reviewer { get; set; }

        AdUser Verifier { get; set; }

        AdUser Assessor { get; set; }
    }
}