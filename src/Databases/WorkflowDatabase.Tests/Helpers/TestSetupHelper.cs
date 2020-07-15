using Microsoft.EntityFrameworkCore;
using WorkflowDatabase.EF;

namespace WorkflowDatabase.Tests.Helpers
{
    internal static class TestSetupHelper
    {
        internal static WorkflowDbContext CreateWorkflowDbContext()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
               .UseSqlServer(
                   @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=taskmanager-dev-workflowdatabase;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False")
               .Options;

            return new WorkflowDbContext(dbContextOptions);
        }
    }
}
