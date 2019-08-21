using BoDi;
using Microsoft.EntityFrameworkCore;
using TechTalk.SpecFlow;
using WorkflowDatabase.EF;

namespace Portal.TestAutomation.Framework.Driver
{
    [Binding]
    internal sealed class SetupTestDbContext
    {
        private readonly IObjectContainer _objectContainer;

        public SetupTestDbContext(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;
        }

        [BeforeScenario(Order = 1)]
        public void InitializeDbContext()
        {

            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseSqlServer(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=taskmanager-dev-workflowdatabase;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False")
                .Options;

            var dbContext = new WorkflowDbContext(dbContextOptions);

            _objectContainer.RegisterInstanceAs<WorkflowDbContext>(dbContext);
        }
    }
}