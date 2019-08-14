# Task Manager Database Design

## Tables

### AssessmentData


| Column Name       | Datatype      | Allow nulls | Description                                                                                     |
|-------------------|---------------|-------------|-------------------------------------------------------------------------------------------------|
|AssessmentDataId   |INT            |No           |The primary key of this table                                                                    |
|SdocId             |INT            |No           |                                                                                                 |
|RsdraNumber        |NVARCHAR(50)   |No           |                                                                                                 |
|SourceDocumentName |NVARCHAR(255)  |No           |                                                                                                 |
|ReceiptDate        |DATETIME       |No           |                                                                                                 |
|ToSdoDate          |DATETIME       |Yes          |                                                                                                 |
|EffectiveStartDate |DATETIME       |Yes          |                                                                                                 |
|TeamDistributedTo  |NVARCHAR(10)   |Yes          |                                                                                                 |
|SourceDocumentType |NVARCHAR(255)  |Yes          |                                                                                                 |
|SourceNature       |NVARCHAR(20)   |Yes          |                                                                                                 |
|Datum              |NVARCHAR(20)   |Yes          |                                                                                                 |
|ProcessId          |INT            |No           |The K2 process instance Id (FK)                                                                  |

The ProcessId column has a unique constraint.
There is a foreign key constraint to the WorkflowInstance table, on that table's ProcessId column.

### Comment

| Column Name       | Datatype      | Allow nulls | Description                                                                                     |
|-------------------|---------------|-------------|-------------------------------------------------------------------------------------------------|
|CommentId          |INT            |No           |The primary key of this table                                                                    |
|ProcessId          |INT            |No           |The K2 process instance Id                                                                       |
|Comment            |NVARCHAR(4000) |No           |                                                                                                 |
|WorkflowInstanceId |INT            |No           |The unique Id for the relevant row in the WorkflowInstance table (FK)                            |

There is a foreign key constraint to the WorkflowInstance table, on that table's WorkflowInstanceId column.

### DbAssessmentReviewData

| Column Name               | Datatype      | Allow nulls | Description                                                                                     |
|-------------------        |---------------|-------------|-------------------------------------------------------------------------------------------------|
|DbAssessmentReviewDataId   |INT            |No           |The primary key of this table                                                                    |
|ProcessId                  |INT            |No           |The K2 process instance Id                                                                       |
|Ion                        |NVARCHAR(50)   |Yes          |                                                                                                 |
|ActivityCode               |NVARCHAR(50)   |Yes          |                                                                                                 |
|Assessor                   |NVARCHAR(255)  |Yes          |                                                                                                 |
|Verifier                   |NVARCHAR(255)  |Yes          |                                                                                                 |
|TaskComplexity             |NVARCHAR(50)   |Yes          |                                                                                                 |
|WorkflowInstanceId         |INT            |No           |The unique Id for the relevant row in the WorkflowInstance table (FK)                            |

There is a foreign key constraint to the WorkflowInstance table, on that table's WorkflowInstanceId column.

### WorkflowInstance

| Column Name               | Datatype      | Allow nulls | Description                                                                                     |
|-------------------        |---------------|-------------|-------------------------------------------------------------------------------------------------|
|WorkflowInstanceId         |INT            |No           |The primary key of this table                                                                    |
|ProcessId                  |INT            |No           |The K2 process instance Id                                                                       |
|SerialNumber               |NVARCHAR(255)  |Yes          |                                                                                                 |
|ParentProcessId            |INT            |Yes          |If a sub workflow, the parent workflow that was used to generate the new instance.               |
|WorkflowType               |NVARCHAR(50)   |Yes          |                                                                                                 |
|ActivityName               |NVARCHAR(50)   |Yes          |                                                                                                 |

