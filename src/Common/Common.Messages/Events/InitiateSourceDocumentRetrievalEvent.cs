﻿using System;
using Common.Messages.Enums;

namespace Common.Messages.Events
{
    public class InitiateSourceDocumentRetrievalEvent : ICorrelate
    {
        public int SourceDocumentId { get; set; }
        public Guid CorrelationId { get; set; }
        public int ProcessId { get; set; }
        public bool GeoReferenced { get; set; }
        public SourceType SourceType { get; set; }
        public Guid SdocRetrievalId { get; set; }
        public Guid UniqueId { get; set; }
    }
}
