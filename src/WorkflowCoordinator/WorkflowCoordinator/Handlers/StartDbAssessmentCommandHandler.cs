using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;
using WorkflowCoordinator.Messages;

namespace WorkflowCoordinator.Handlers
{
    public class StartDbAssessmentCommandHandler : IHandleMessages<StartDbAssessmentCommand>
    {
        ILog log = LogManager.GetLogger<SdraPollingMessage>();

        public async Task Handle(StartDbAssessmentCommand command, IMessageHandlerContext context)
        {
            await Task.CompletedTask;
        }
    }
}
