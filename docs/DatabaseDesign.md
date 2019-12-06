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
|ReceiptDate        |DATETIME2      |No           |                                                                                                 |
|ToSdoDate          |DATETIME2      |Yes          |                                                                                                 |
|EffectiveStartDate |DATETIME2      |Yes          |                                                                                                 |
|TeamDistributedTo  |NVARCHAR(20)   |Yes          |                                                                                                 |
|SourceDocumentType |NVARCHAR(4000) |Yes          |                                                                                                 |
|SourceNature       |NVARCHAR(255)  |Yes          |                                                                                                 |
|Datum              |NVARCHAR(2000) |Yes          |                                                                                                 |
|ProcessId          |INT            |No           |The K2 process instance Id (FK)                                                                  |

The ProcessId column has a unique constraint.
There is a foreign key constraint to the WorkflowInstance table, on that table's ProcessId column.
The SdocId column has a unique constraint.

### Comment

The Comment table holds user entered comments for each workflow.

| Column Name       | Datatype      | Allow nulls | Description                                                                                     |
|-------------------|---------------|-------------|-------------------------------------------------------------------------------------------------|
|CommentId          |INT            |No           |The primary key of this table                                                                    |
|ProcessId          |INT            |No           |The K2 process instance Id                                                                       |
|Text               |NVARCHAR(4000) |No           |                                                                                                 |
|WorkflowInstanceId |INT            |No           |The unique Id for the relevant row in the WorkflowInstance table (FK)                            |
|Username		    |NVARCHAR(255)  |No           |The user that entered the comment                      											|
|Created			|DATETIME       |No           |The date and time that the comment was created							                        |

There is a foreign key constraint to the WorkflowInstance table, on that table's WorkflowInstanceId column.

### OnHold

The OnHold table records when a task has been put on hold as well as whether it has been taken off hold.

| Column Name       | Datatype      | Allow nulls | Description                                                         |
|-------------------|---------------|-------------|---------------------------------------------------------------------|
|OnHoldId           |INT            |No     |The primary key of this table                                              |
|WorkflowInstanceId |INT            |No     |The unique Id for the relevant row in the WorkflowInstance table (FK)      |
|ProcessId          |INT            |No     |The K2 process instance Id                                                 |
|OnHoldTime         |Date           |No     |The date (not time) when the task was put on hold                          |
|OffHoldTime        |Date           |Yes    |The date (not time) when the task was taken off hold                       |
|OnHoldUser		    |NVARCHAR(255)  |No     |The user that put the task on hold                                			|
|OffHoldUser		|NVARCHAR(255)  |Yes    |The user that took the task off hold                           			|

There is a foreign key constraint to the WorkflowInstance table, on that table's WorkflowInstanceId column.

### TaskNote

The TaskNote table holds user entered notes for each workflow.

| Column Name       | Datatype      | Allow nulls | Description                                                                                     |
|-------------------|---------------|-------------|-------------------------------------------------------------------------------------------------|
|TaskNoteId             |INT            |No           |The primary key of this table                                                                |
|ProcessId              |INT            |No           |The K2 process instance Id                                                                   |
|Text                   |NVARCHAR(MAX)  |No           |                                                                                             |
|WorkflowInstanceId     |INT            |No           |The unique Id for the relevant row in the WorkflowInstance table (FK)                        |
|CreatedByUsername	    |NVARCHAR(255)  |No           |The user that created the Task Note                      									|
|Created		     	|DATETIME       |No           |The date and time that the Task Note was created							                    |
|LastModified			|DATETIME       |No           |The date and time that the Task Note was last modified							            |
|LastModifiedByUsername |NVARCHAR(255)  |No           |The user that last modified the Task Note                      								|

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

### PrimaryDocumentStatus

The PrimaryDocumentStatus table holds the status of a source document retrieval operation from SDRA. We will initially set this to Started when we initiate the retrieval,
and then update the row with subsequent statuses.

| Column Name               | Datatype          | Allow nulls | Description                                                                                     |
|-------------------        |-------------------|-------------|-------------------------------------------------------------------------------------------------|
|PrimaryDocumentStatusId    |INT                |No           |The primary key of this table                                                                    |
|ProcessId                  |INT                |No           |The K2 process instance Id                                                                       |
|SdocId                     |INT                |No           |                                                                                                 |
|ContentServiceId           |UniqueIdentifier   |Yes          |Once stored in the Content Service, the Content Service unique identifier of the source document.|
|Status                     |NVARCHAR(25)       |No           |                                                                                                 |
|StartedAt                  |DATETIME           |No           |                                                                                                 |
|CorrelationId              |UniqueIdentifier   |Yes          |                                                                                                 |

The ProcessId column has a foreign key constraint to the WorkflowInstance table.

### LinkedDocument

The LinkedDocument table holds linked documents from SDRA for open assessments.

| Column Name               | Datatype          | Allow nulls | Description                                                                                     |
|-------------------        |-------------------|-------------|-------------------------------------------------------------------------------------------------|
|LinkedDocumentId           |INT                |No           |The primary key of this table                                                                    |
|SdocId                     |INT                |No           |                                                                                                 |
|RsdraNumber                |NVARCHAR(50)       |No           |                                                                                                 |
|SourceDocumentName         |NVARCHAR(255)      |No           | |
|ReceiptDate                |DateTime2          |Yes          | |
|SourceDocumentType         |NVARCHAR(4000)     |Yes          | |
|SourceNature               |NVARCHAR(255)      |Yes          | |
|Datum                      |NVARCHAR(2000)     |Yes          | |
|LinkType                   |NVARCHAR(10)       |No           |Can be Forward, Backward or SEP    |
|LinkedSdocId               |INT                |No           |          |
|ContentServiceId           |UNIQUEIDENTIFIER   |Yes          |The guid for the linked document once stored in the Content service                              |
|Status                     |INT                |No           |The status of retrieving the linked document from SDRA                                           |
|Created                    |DATETIME           |No           |                                                                                                 |

The SdocId column has a foreign key constraint to the AssessmentData table.

### HpdUsage

The HpdUsage table holds the usages from HPD.

| Column Name       | Datatype          | Allow nulls | Description                          |
|-------------------|-------------------|-------------|--------------------------------------|
|HpdUsageId         |INT                |No           |The primary key of this table         |
|Name               |NVARCHAR(25)       |No           |                                      |

The Name column has a unique constraint.

### DataImpact

The DataImpact table can hold zero/one/multiple Data Impact records per ProcessId.
The same HpdUsageId should not be used multiple times per ProcessId.

| Column Name       | Datatype          | Allow nulls | Description                          |
|-------------------|-------------------|-------------|--------------------------------------|
|DataImpactId       |INT                |No           |The primary key of this table         |
|ProcessId          |INT                |No           |The K2 process instance Id            |
|HpdUsageId         |INT                |No           |                                      |
|Edited             |BIT                |No           |                                      |
|Comments           |NVARCHAR(4000)     |Yes          |                                      |
|Verified           |BIT                |No           |                                      |

The ProcessId column has a foreign key constraint to the WorkflowInstance table.
The HpdUsageId column has a foreign key constraint to the HpdUsage table.

### HpdUser

The HpdUser table holds mappings for the AD user to the HPD user.

| Column Name       | Datatype          | Allow nulls | Description                          |
|-------------------|-------------------|-------------|--------------------------------------|
|HpdUserId          |INT                |No           |The primary key of this table         |
|AdUsername         |NVARCHAR(255)      |No           |                                      |
|HpdUsername        |NVARCHAR(255)      |No           |                                      |

The AdUsername column has a unique constraint.
The HpdUsername column has a unique constraint.