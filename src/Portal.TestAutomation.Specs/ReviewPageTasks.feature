Feature: ReviewPageTasks

@mytag
Scenario: The review page loads
	Given I navigate to the review page 
	 Then The review page has loaded

@mytag
Scenario: The linked documents on the review page are present
	Given I navigate to the landing page
	 When I click an assessment
	 Then The review page has loaded
	 When I expand the source document details
	 Then the linked documents are displayed on the screen
	 #Then the linked documents displayed on the screen are the same as in the database






