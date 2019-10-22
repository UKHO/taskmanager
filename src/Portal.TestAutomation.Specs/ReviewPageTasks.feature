Feature: ReviewPageTasks

@mytag
Scenario: The review page loads
	Given I navigate to the review page 
	 Then The review page has loaded

@mytag
Scenario: The source document on the review page is present
	 Given The review page has loaded with the first process Id
	  Then The source document with the corresponding process Id in the database matches the sdocId on the UI






