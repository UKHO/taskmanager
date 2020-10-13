﻿namespace WorkflowDatabase.EF.Interfaces
{
    public interface ITaskData
    {
        string Ion { get; set; }
        string Complexity { get; set; }
        string ActivityCode { get; set; }
        string SourceCategory { get; set; }
        string TaskType { get; set; }
    }
}
