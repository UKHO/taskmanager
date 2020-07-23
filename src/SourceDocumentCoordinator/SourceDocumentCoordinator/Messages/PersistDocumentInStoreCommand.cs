﻿using System;
using Common.Messages;
using Common.Messages.Enums;

namespace SourceDocumentCoordinator.Messages
{
    public class PersistDocumentInStoreCommand : ICorrelate
    {
        public Guid CorrelationId { get; set; }
        public int ProcessId { get; set; }
        public int SourceDocumentId { get; set; }
        public SourceType SourceType { get; set; }
        public string Filepath { get; set; }
        public Guid UniqueId { get; set; }
    }
}
