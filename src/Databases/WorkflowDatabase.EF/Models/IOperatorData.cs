namespace WorkflowDatabase.EF.Models
{
    public interface IOperatorData
    {
        string Reviewer { get; set; }

        string Verifier { get; set; }

        string Assessor { get; set; }
    }
}