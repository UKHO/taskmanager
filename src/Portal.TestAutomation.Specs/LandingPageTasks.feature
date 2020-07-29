Feature: LandingPageTasks

Scenario: The landing page shows my tasks
	Given I am on the landing page
	When The landing page has loaded
	Then I should see all of the tasks assigned to me
		#And I should see all of the unassigned tasks
		#And I shouldn't see tasks assigned to other people

#Scenario: I can search My Task List
#	Given I am on the landing page
#	When I search for a task
#	Then I should only see tasks that match my search
