using System;

namespace Common.Messages
{
    public interface ICorrelate
    {
        Guid CorrelationId { get; set; }
    }
}