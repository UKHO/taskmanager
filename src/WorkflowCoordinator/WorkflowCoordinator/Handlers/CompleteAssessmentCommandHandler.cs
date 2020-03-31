using System.Threading.Tasks;
using Common.Helpers;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Serilog.Context;
using WorkflowCoordinator.Messages;
using WorkflowDatabase.EF;

namespace WorkflowCoordinator.Handlers
{
    public class CompleteAssessmentCommandHandler : IHandleMessages<CompleteAssessmentCommand>
    {
        private readonly ILogger<CompleteAssessmentCommandHandler> _logger;
        private readonly WorkflowDbContext _dbContext;

        public CompleteAssessmentCommandHandler(ILogger<CompleteAssessmentCommandHandler> logger, WorkflowDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task Handle(CompleteAssessmentCommand message, IMessageHandlerContext context)
        {
            LogContext.PushProperty("MessageId", context.MessageId);
            LogContext.PushProperty("Message", message.ToJSONSerializedString());
            LogContext.PushProperty("EventName", nameof(CompleteAssessmentCommand));
            LogContext.PushProperty("MessageCorrelationId", message.CorrelationId);
            LogContext.PushProperty("ProcessId", message.ProcessId);

            _logger.LogInformation("Entering {EventName} handler with: {Message}");

        }
    }
}
