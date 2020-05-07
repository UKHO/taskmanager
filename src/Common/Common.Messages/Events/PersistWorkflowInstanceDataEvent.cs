﻿using System;
using WorkflowDatabase.EF;

namespace Common.Messages.Events
{
    // TODO: Remove when Review, Assess, and Verify progression stories are completed
    public class PersistWorkflowInstanceDataEvent : ICorrelate
    {
        public Guid CorrelationId { get; set; }
        public int ProcessId { get; set; }
        public WorkflowStage FromActivity { get; set; }
        public WorkflowStage ToActivity { get; set; }
    }
}
