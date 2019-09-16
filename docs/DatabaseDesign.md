# Task Manager Database Design

## Tables

### AssessmentData

The AssessmentData table is responsible for holding the data that comes back from the SDRA view when we need to start a new
database assessment workflow for an open assessment.

| Column Name       | Datatype      | Allow nulls | Description                                                                                     |
|-------------------|---------------|-------------|-------------------------------------------------------------------------------------------------|
|AssessmentDataId   |INT            |No           |The primary key of this table                                                                    |
|SdocId             |INT            |No           |                                                                                                 |
|RsdraNumber        |NVARCHAR(50)   |No           |                                                                                                 |
|SourceDocumentName |NVARCHAR(255)  |No           |                                                                                                 |
|ReceiptDate        |DATETIME       |No           |                                                                                                 |
|ToSdoDate          |DATETIME       |Yes          |                                                                                                 |
|EffectiveStartDate |DATETIME       |Yes          |                                                                                                 |
|TeamDistributedTo  |NVARCHAR(20)   |Yes          |                                                                                                 |
|SourceDocumentType |NVARCHAR(4000)  |Yes          |                                                                                                 |
|SourceNature       |NVARCHAR(255)   |Yes          |                                                                                                 |
|Datum              |NVARCHAR(2000)   |Yes          |                                                                                                 |
|ProcessId          |INT            |No           |The K2 process instance Id (FK)                                                                  |

The ProcessId column has a unique constraint.
There is a foreign key constraint to the WorkflowInstance table, on that table's ProcessId column.

### Comment

The Comment table holds user entered comments for each workflow.

| Column Name       | Datatype      | Allow nulls | Description                                                                                     |
|-------------------|---------------|-------------|-------------------------------------------------------------------------------------------------|
|CommentId          |INT            |No           |The primary key of this table                                                                    |
|ProcessId          |INT            |No           |The K2 process instance Id                                                                       |
|Comment            |NVARCHAR(4000) |No           |                                                                                                 |
|WorkflowInstanceId |INT            |No           |The unique Id for the relevant row in the WorkflowInstance table (FK)                            |

There is a foreign key constraint to the WorkflowInstance table, on that table's WorkflowInstanceId column.

### DbAssessmentReviewData

The DbAssessmentReviewData table holds the data that may change on the Review step of a Database Assessment workflow instance.

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

The WorkflowInstance table will hold the instances of a Database Assessment workflow. The ParentProcessId column will be used when an instance is a 
sub workflow, and will hold the ProcessId of the parent workflow that the sub was generated from.

| Column Name               | Datatype      | Allow nulls | Description                                                                                     |
|-------------------        |---------------|-------------|-------------------------------------------------------------------------------------------------|
|WorkflowInstanceId         |INT            |No           |The primary key of this table                                                                    |
|ProcessId                  |INT            |No           |The K2 process instance Id                                                                       |
|SerialNumber               |NVARCHAR(255)  |Yes          |                                                                                                 |
|ParentProcessId            |INT            |Yes          |If a sub workflow, the parent workflow that was used to generate the new instance.               |
|WorkflowType               |NVARCHAR(50)   |Yes          |                                                                                                 |
|ActivityName               |NVARCHAR(50)   |Yes          |                                                                                                 |
|StartedAt                  |DATETIME       |No           |                                                                                                 |
|Status                     |NVARCHAR(25)   |No           |                                                                                                 |

The ProcessId column has a unique constraint, to facilitate the foreign key from the AssessmentData table.

### SourceDocumentStatus

The SourceDocumentStatus table holds the status of a source document retrieval operation from SDRA. We will initially set this to Started when we initiate the retrieval,
and then update the row with subsequent statuses.

| Column Name               | Datatype          | Allow nulls | Description                                                                                     |
|-------------------        |-------------------|-------------|-------------------------------------------------------------------------------------------------|
|SourceDocumentStatusId     |INT                |No           |The primary key of this table                                                                    |
|ProcessId                  |INT                |No           |The K2 process instance Id                                                                       |
|SdocId                     |INT                |No           |                                                                                                 |
|ContentServiceId           |UniqueIdentifier   |Yes          |Once stored in the Content Service, the Content Service unique identifier of the source document.|
|Status                     |NVARCHAR(25)       |No           |                                                                                                 |
|StartedAt                  |DATETIME           |No           |                                                                                                 |

The ProcessId column has a foreign key constraint to the WorkflowInstance table.
