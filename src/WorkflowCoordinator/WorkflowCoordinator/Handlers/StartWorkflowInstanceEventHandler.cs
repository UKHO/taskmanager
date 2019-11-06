using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Common.Messages.Events;
using NServiceBus;

namespace WorkflowCoordinator.Handlers
{
    public class StartWorkflowInstanceEventHandler : IHandleMessages<StartWorkflowInstanceEvent>
    {
        public Task Handle(StartWorkflowInstanceEvent message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
    }
}
