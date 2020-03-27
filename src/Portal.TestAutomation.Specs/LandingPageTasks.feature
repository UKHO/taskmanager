Feature: LandingPageTasks

Scenario: The landing page loads
	When I navigate to the landing page 
	Then The landing page has loaded

Scenario: The landing page has some tasks
	When I navigate to the landing page 
	Then The landing page has loaded
	When I enter Process Id of "321"
	Then Task with process id 321 appears in both the assigned and unassigned tasks tables