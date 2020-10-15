# Task Manager Database Design

## Tables

[AssessmentData](#assessmentdata)  
[AssignedTaskType](#assignedtasksourcetype)  
[CachedHpdEncProduct](#CachedHpdEncProduct)  
[CachedHpdWorkspace](#CachedHpdWorkspace)  
[CarisProjectDetails](#CarisProjectDetails)  
[Comment](#comment)  
[DatabaseDocumentStatus](#DatabaseDocumentStatus)  
[OnHold](#onhold)  
[TaskNote](#tasknote)  
[DbAssessmentAssignTask](#DbAssessmentAssignTask)  
[DbAssessmentReviewData](#dbassessmentreviewdata)  
[DbAssessmentAssessData](#dbassessmentassessdata)  
[DbAssessmentVerifyData](#dbassessmentverifydata)  
[WorkflowInstance](#workflowinstance)  
[PrimaryDocumentStatus](#primarydocumentstatus)  
[LinkedDocument](#linkeddocument)  
[HpdUsage](#hpdusage)  
[DataImpact](#dataimpact)  
[HpdUser](#hpduser)  
[ProductAction](#productaction)  
[ProductActionType](#productactiontype)  

### AssessmentData

The AssessmentData table is responsible for holding the data that comes back from the SDRA view when we need to start a new
database assessment workflow for an open assessment.

| Column Name       | Datatype      | Allow nulls | Description                                                                                     |
|-------------------|---------------|-------------|-------------------------------------------------------------------------------------------------|
|AssessmentDataId   |INT            |No           |PRIMARY KEY                                                                    |
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

[Go To Tables](#tables)

### AssignedTaskType

The `AssignedTaskType` table is a lookup table that contains the list of Source Types used for Assigned Tasks.

It gets populated via Post Deployment script 
`AssignedTaskType.sql`.

| Column Name       | Datatype      | Allow nulls | Description                                                                                     |
|-------------------|---------------|-------------|-------------------------------------------------------------------------------------------------|
|AssignedTaskTypeId   |INT            |No           |PRIMARY KEY                                                                    |
|Name             |NVARCHAR(50)            |No           |                                                                                                 |

`AssignedTaskTypeId` column is not an Identity column, this is to allow a better control of that column's values. There is a Unique constraint on the Name column.

[Go To Tables](#tables)

### CachedHpdEncProduct

`CachedHpdEncProduct` table caches the ENC Products from CARIS, this done at the `Portal` startup where this table is first emptied then populated from CARIS.

| Column Name            | Datatype      | Allow nulls | Description                                                                                     |
|------------------------|---------------|-------------|-------------------------------------------------------------------------------------------------|
|CachedHpdEncProductId   |INT            |No           |PRIMARY KEY                                                                                      |
|Name                    |NVARCHAR(100)  |No           |                                                                                                 |

There is a Unique Index for `Name`

[Go To Tables](#tables)

### CachedHpdWorkspace

`CachedHpdWorkspace` table caches the Workspaces from CARIS, this done at the `Portal` startup where this table is first emptied then populated from CARIS.

| Column Name       | Datatype      | Allow nulls | Description                                                                                     |
|-------------------|---------------|-------------|-------------------------------------------------------------------------------------------------|
|CachedHpdWorkspaceId   |INT            |No           |PRIMARY KEY                                                                    |
|Name             |NVARCHAR(100)            |No           |                                                                                                 |

There is a Unique Index for `Name`

[Go To Tables](#tables)

### CarisProjectDetails

`CarisProjectDetails` table holds the details of Caris projects that have been created by users as part of the Db Assessment workflow.

| Column Name       | Datatype      | Allow nulls | Description                                                                                     |
|-------------------|---------------|-------------|-------------------------------------------------------------------------------------------------|
|CarisProjectDetailsId   |INT            |No           |PRIMARY KEY                                                                    |
|ProcessId             |INT           |No           |                                                                                                 |
|ProjectId             |INT           |No           |                                                                                                 |
|ProjectName             |NVARCHAR(100)           |No           |                                                                                                 |
|Created             |DATETIME           |No           |                                                                                                 |
|CreatedBy             |NVARCHAR(255)           |No           |                                                                                                 |

There is a Unique constraint for `ProcessId`. `ProcessId` is a foreign key from [WorkflowInstance](#workflowinstance) table.

[Go To Tables](#tables)

### Comment

The Comment table holds user entered comments for each workflow.

| Column Name       | Datatype      | Allow nulls | Description                                                                                     |
|-------------------|---------------|-------------|-------------------------------------------------------------------------------------------------|
|CommentId          |INT            |No           |PRIMARY KEY                                                                    |
|ProcessId          |INT            |No           |The K2 process instance Id                                                                       |
|Text               |NVARCHAR(4000) |No           |                                                                                                 |
|WorkflowInstanceId |INT            |No           |The unique Id for the relevant row in the WorkflowInstance table (FK)                            |
|Username		    |NVARCHAR(255)  |No           |The user that entered the comment                      											|
|Created			|DATETIME       |No           |The date and time that the comment was created							                        |

There is a foreign key constraint to the WorkflowInstance table, on that table's WorkflowInstanceId column.

[Go To Tables](#tables)

### DatabaseDocumentStatus

This table is responsible for persisting Source Document added from `SDRA Database` using `SDOCID`

| Column Name               | Datatype          | Allow nulls | Description                                                                                     |
|-------------------        |-------------------|-------------|-------------------------------------------------------------------------------------------------|
|DatabaseDocumentStatusId  |INT |NO  |PRIMARY KEY  
|ProcessId  |INT  |NO | 
|SdocId  |INT  |NO  | 
|SourceDocumentName  |NVARCHAR(255)  |NO  | 
|SourceDocumentType  |NVARCHAR(4000)  |Yes  |
|ContentServiceId  |UNIQUEIDENTIFIER  |Yes  | 
|Status  |NVARCHAR(25)  |NO  |
|Created  |DATETIME  |NO  | 

`ProcessId` is a foreign key from [WorkflowInstance](#workflowinstance) table

[Go To Tables](#tables)

### OnHold

The OnHold table records when a task has been put on hold as well as whether it has been taken off hold.

| Column Name       | Datatype      | Allow nulls | Description                                                         |
|-------------------|---------------|-------------|---------------------------------------------------------------------|
|OnHoldId           |INT            |No     |PRIMARY KEY                                              |
|WorkflowInstanceId |INT            |No     |The unique Id for the relevant row in the WorkflowInstance table (FK)      |
|ProcessId          |INT            |No     |The K2 process instance Id                                                 |
|OnHoldTime         |Date           |No     |The date (not time) when the task was put on hold                          |
|OffHoldTime        |Date           |Yes    |The date (not time) when the task was taken off hold                       |
|OnHoldUser		    |NVARCHAR(255)  |No     |The user that put the task on hold                                			|
|OffHoldUser		|NVARCHAR(255)  |Yes    |The user that took the task off hold                           			|

There is a foreign key constraint to the WorkflowInstance table, on that table's WorkflowInstanceId column.

[Go To Tables](#tables)

### TaskNote

The TaskNote table holds user entered notes for each workflow.

| Column Name       | Datatype      | Allow nulls | Description                                                                                     |
|-------------------|---------------|-------------|-------------------------------------------------------------------------------------------------|
|TaskNoteId             |INT            |No           |PRIMARY KEY                                                                |
|ProcessId              |INT            |No           |The K2 process instance Id                                                                   |
|Text                   |NVARCHAR(MAX)  |No           |                                                                                             |
|WorkflowInstanceId     |INT            |No           |The unique Id for the relevant row in the WorkflowInstance table (FK)                        |
|CreatedByUsername	    |NVARCHAR(255)  |No           |The user that created the Task Note                      									|
|Created		     	|DATETIME       |No           |The date and time that the Task Note was created							                    |
|LastModified			|DATETIME       |No           |The date and time that the Task Note was last modified							            |
|LastModifiedByUsername |NVARCHAR(255)  |No           |The user that last modified the Task Note                      								|

There is a foreign key constraint to the WorkflowInstance table, on that table's WorkflowInstanceId column.

[Go To Tables](#tables)

### DbAssessmentAssignTask

This table is responsible for persisting `Additional Assigned Tasks`, the `Primary Assigned Tasks` will be stored in [DbAssessmentReviewData](#dbassessmentreviewdata) table.

| Column Name       | Datatype      | Allow nulls | Description                                                                                     |
|-------------------|---------------|-------------|-------------------------------------------------------------------------------------------------|
|DbAssessmentAssignTaskId |INT  |NO  |PRIMARY KEY
|ProcessId  |INT  |NO  |
|Assessor  |NVARCHAR(255)  |Yes | 
|Verifier  |NVARCHAR(255)  |Yes | 
|AssignedTaskSourceType  |NVARCHAR(50) |Yes  |
|WorkspaceAffected  |NVARCHAR(100) |Yes  |
|Notes  |NVARCHAR(4000) |Yes  |


`AssignedTaskSourceType`, `WorkspaceAffected`, and `Notes` are used for the `Primary Assigned Task`


`ProcessId` is a foreign key from [WorkflowInstance](#workflowinstance) table

[Go To Tables](#tables)

### DbAssessmentReviewData

The DbAssessmentReviewData table holds the data that may change on the Review step of a Database Assessment workflow instance.

| Column Name               | Datatype      | Allow nulls | Description                                                                                     |
|-------------------        |---------------|-------------|-------------------------------------------------------------------------------------------------|
|DbAssessmentReviewDataId   |INT            |No           |PRIMARY KEY                                                                    |
|ProcessId                  |INT            |No           |The K2 process instance Id                                                                       |
|Ion                        |NVARCHAR(50)   |Yes          |                                                                                                 |
|ActivityCode               |NVARCHAR(50)   |Yes          |                                                                                                 |
|SourceCategory               |NVARCHAR(255)   |Yes          |                                                                                                 |
|TaskComplexity             |NVARCHAR(50)   |Yes          |                                                                                                 |
|WorkflowInstanceId         |INT            |No           |The unique Id for the relevant row in the WorkflowInstance table (FK)                            |
|Assessor                   |NVARCHAR(255)  |Yes          |                                                                                                 |
|Verifier                   |NVARCHAR(255)  |Yes          |                                                                                                 |
|AssignedTaskSourceType  |NVARCHAR(50)  |Yes  |
|WorkspaceAffected  |NVARCHAR(100) |Yes  |
|Notes  |NVARCHAR(4000) |Yes  |

There is a foreign key constraint to the WorkflowInstance table, on that table's WorkflowInstanceId column.

[Go To Tables](#tables)

### DbAssessmentAssessData

The DbAssessmentAssessData table holds the data that may change on the Assess step of a Database Assessment workflow instance.

| Column Name               | Datatype      | Allow nulls | Description                              |
|---------------------------|---------------|-------------|------------------------------------------|
|DbAssessmentAssessDataId   |INT            |No           |PRIMARY KEY             |
|ProcessId                  |INT            |No           |The K2 process instance Id                |
|Ion                        |NVARCHAR(50)   |Yes          |                                          |
|ActivityCode               |NVARCHAR(50)   |Yes          |                                          |
|SourceCategory               |NVARCHAR(255)   |Yes          |                                                                                                 |
|WorkManager                |NVARCHAR(255)  |Yes          |                                          |
|Assessor                   |NVARCHAR(255)  |Yes          |                                          |
|Verifier                   |NVARCHAR(255)  |Yes          |                                          |
|TaskComplexity             |NVARCHAR(50)   |Yes          |                                          |
|ProductActioned            |BIT            |Yes          |                                          |
|ProductActionChangeDetails |NVARCHAR(Max)  |Yes          |                                          |
|WorkspaceAffected          |NVARCHAR(100)  |Yes          |
|WorkflowInstanceId         |INT            |No           |The unique Id for the relevant row in the WorkflowInstance table (FK)   |

There is a foreign key constraint to the WorkflowInstance table, on that table's WorkflowInstanceId column.

[Go To Tables](#tables)

### DbAssessmentVerifyData

The DbAssessmentVerifyData table holds the data that may change on the Verify step of a Database Assessment workflow instance.

| Column Name               | Datatype      | Allow nulls | Description                              |
|---------------------------|---------------|-------------|------------------------------------------|
|DbAssessmentVerifyDataId   |INT            |No           |PRIMARY KEY             |
|ProcessId                  |INT            |No           |The K2 process instance Id                |
|Ion                        |NVARCHAR(50)   |Yes          |                                          |
|ActivityCode               |NVARCHAR(50)   |Yes          |                                          |
|SourceCategory             |NVARCHAR(255)   |Yes         |                                                                                                 |
|Reviewer                   |NVARCHAR(255)  |Yes          |                                          |
|Assessor                   |NVARCHAR(255)  |Yes          |                                          |
|Verifier                   |NVARCHAR(255)  |Yes          |                                          |
|TaskType                   |NVARCHAR(50)   |Yes          |                                          |
|ProductActioned            |BIT            |Yes          |                                          |
|ProductActionChangeDetails |NVARCHAR(Max)  |Yes          |                                          |
|WorkspaceAffected          |NVARCHAR(100)  |Yes          |
|WorkflowInstanceId         |INT            |No           |The unique Id for the relevant row in the WorkflowInstance table (FK)   |

There is a foreign key constraint to the WorkflowInstance table, on that table's WorkflowInstanceId column.

[Go To Tables](#tables)

### WorkflowInstance

The WorkflowInstance table will hold the instances of a Database Assessment workflow. The ParentProcessId column will be used when an instance is a
sub workflow, and will hold the ProcessId of the parent workflow that the sub was generated from.

| Column Name               | Datatype      | Allow nulls | Description                                                                                     |
|-------------------        |---------------|-------------|-------------------------------------------------------------------------------------------------|
|WorkflowInstanceId         |INT            |No           |PRIMARY KEY                                                                                      |
|ProcessId                  |INT            |No           |The K2 process instance Id                                                                       |
|SerialNumber               |NVARCHAR(255)  |Yes          |                                                                                                 |
|ParentProcessId            |INT            |Yes          |If a sub workflow, the parent workflow that was used to generate the new instance.               |
|ActivityName               |NVARCHAR(50)   |Yes          |                                                                                                 |
|StartedAt                  |DATETIME       |No           |                                                                                                 |
|Status                     |NVARCHAR(25)   |No           |                                                                                                 |

The ProcessId column has a unique constraint, to facilitate the foreign key from the AssessmentData table.

[Go To Tables](#tables)

### PrimaryDocumentStatus

The PrimaryDocumentStatus table holds the status of a source document retrieval operation from SDRA. We will initially set this to Started when we initiate the retrieval,
and then update the row with subsequent statuses.

| Column Name               | Datatype          | Allow nulls | Description                                                                                     |
|-------------------        |-------------------|-------------|-------------------------------------------------------------------------------------------------|
|PrimaryDocumentStatusId    |INT                |No           |PRIMARY KEY                                                                    |
|ProcessId                  |INT                |No           |The K2 process instance Id                                                                       |
|SdocId                     |INT                |No           |                                                                                                 |
|ContentServiceId           |UniqueIdentifier   |Yes          |Once stored in the Content Service, the Content Service unique identifier of the source document.|
|Status                     |NVARCHAR(25)       |No           |                                                                                                 |
|StartedAt                  |DATETIME           |No           |                                                                                                 |
|CorrelationId              |UniqueIdentifier   |Yes          |                                                                                                 |

The ProcessId column has a foreign key constraint to the WorkflowInstance table.

[Go To Tables](#tables)

### LinkedDocument

The LinkedDocument table holds linked documents from SDRA for open assessments.

| Column Name               | Datatype          | Allow nulls | Description                                                                                     |
|-------------------        |-------------------|-------------|-------------------------------------------------------------------------------------------------|
|LinkedDocumentId           |INT                |No           |PRIMARY KEY                                                                    |
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

[Go To Tables](#tables)

### HpdUsage

The HpdUsage table holds the usages from HPD.

| Column Name       | Datatype          | Allow nulls | Description                          |
|-------------------|-------------------|-------------|--------------------------------------|
|HpdUsageId         |INT                |No           |PRIMARY KEY                           |
|Name               |NVARCHAR(255)      |No           |                                      |
|SortIndex          |TINYINT            |No           |                                      |

The Name column has a unique constraint.
The SortIndex column has a unique constraint.

[Go To Tables](#tables)

### DataImpact

The DataImpact table can hold zero/one/multiple Data Impact records per ProcessId.
The same HpdUsageId should not be used multiple times per ProcessId.

| Column Name       | Datatype          | Allow nulls | Description                          |
|-------------------|-------------------|-------------|--------------------------------------|
|DataImpactId       |INT                |No           |PRIMARY KEY         |
|ProcessId          |INT                |No           |The K2 process instance Id            |
|HpdUsageId         |INT                |No           |                                      |
|Edited             |BIT                |No           |                                      |
|Comments           |NVARCHAR(4000)     |Yes          |                                      |
|Features Submitted |BIT                |No           |                                      |
|Features Verified  |BIT                |No           |                                      |
|StsUsage           |BIT                |No           |                                      |

The ProcessId column has a foreign key constraint to the WorkflowInstance table.
The HpdUsageId column has a foreign key constraint to the HpdUsage table.

[Go To Tables](#tables)

### HpdUser

The HpdUser table holds mappings for the AD user to the HPD user.

| Column Name       | Datatype          | Allow nulls | Description                          |
|-------------------|-------------------|-------------|--------------------------------------|
|HpdUserId          |INT                |No           |PRIMARY KEY         |
|AdUsername         |NVARCHAR(255)      |No           |                                      |
|HpdUsername        |NVARCHAR(255)      |No           |                                      |

The AdUsername column has a unique constraint.
The HpdUsername column has a unique constraint.

[Go To Tables](#tables)

### ProductAction

The ProductAction table holds records of product actions used in the Database Assessment workflow Assess & Verify steps.

| Column Name       | Datatype          | Allow nulls | Description                          |
|-------------------|-------------------|-------------|--------------------------------------|
|ProductActionId    |INT                |No           |PRIMARY KEY         |
|ProcessId          |INT                |No           |The K2 process instance Id            |
|ImpactedProduct    |NVARCHAR(100)      |No           |                                      |
|ProductActionTypeId|INT                |No           |                                      |
|Verified           |BIT                |No           |                                      |

The ProcessId column has a foreign key constraint to the WorkflowInstance table.
The ProductActionTypeId column has a foreign key constraint to the ProductActionType table.
The ProcessId & ProductActionTypeId columns have a unique constraint.

[Go To Tables](#tables)

### ProductActionType

The ProductActionType table is a lookup table used by ProductAction.

| Column Name       | Datatype          | Allow nulls | Description                          |
|-------------------|-------------------|-------------|--------------------------------------|
|ProductActionTypeId|INT                |No           |PRIMARY KEY         |
|Name               |NVARCHAR(255)      |No           |                                      |

The Name column has a unique constraint.

[Go To Tables](#tables)
