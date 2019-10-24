using System;
using NServiceBus;

namespace Common.Messages.Events
{
    public class GregTestEvent : IEvent, ICorrelate
    {
        public Guid CorrelationId { get; set; }
        public string Gregio { get; set; } = "mooo";
    }
}
