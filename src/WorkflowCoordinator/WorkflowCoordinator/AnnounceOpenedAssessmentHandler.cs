using Common.Messages;
using NServiceBus;
using NServiceBus.Logging;
using System.Threading.Tasks;

namespace WorkflowCoordinator
{
    class AnnounceOpenedAssessmentHandler : IHandleMessages<AnnounceOpenedAssessmentMessage>
    {
        ILog log = LogManager.GetLogger<AnnounceOpenedAssessmentHandler>();

        public Task Handle(AnnounceOpenedAssessmentMessage message, IMessageHandlerContext context)
        {
            log.Info($"[Defer Message Delivery] for {nameof(AnnounceOpenedAssessmentMessage)} with id: {message.CorrelationId}");
            return Task.CompletedTask;
        }
    }
}
