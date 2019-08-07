using System;

namespace Common.Messages
{
    public interface ICorrelation
    {
        Guid CorrelationId { get; set; }
    }
}