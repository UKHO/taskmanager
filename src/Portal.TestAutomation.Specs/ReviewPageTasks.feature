Feature: ReviewPageTasks

@mytag
Scenario: The review page loads
	Given I navigate to the review page 
	 Then The review page has loaded

@mytag
Scenario: The linked documents on the review page are present
	 Given The review page has loaded with the first process Id
	 When I expand the source document details
	 Then the linked documents are displayed on the screen
	 Then the linked documents displayed on the screen are the same as in the database






