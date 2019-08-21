Feature: LandingPageTasks

@mytag
Scenario: The Landing page loads
	Given I navigate to the landing page 
	Then The landing page has loaded

Scenario: The Landing has some tasks
	Given I navigate to the landing page 
	Then The landing page has loaded
	And Task with process id 123 appears