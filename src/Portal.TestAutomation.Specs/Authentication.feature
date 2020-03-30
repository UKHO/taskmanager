Feature: Authentication

@skipLogin
Scenario: Redirect to login when unauthenticated
	Given I am an unauthenticated user
	When I navigate to the landing page 
	Then I am redirected to the login page
	When I log in 
	Then I am redirected to the landing page

Scenario: Not asked to log in when already authenticated
	Given I am an authenticated user
	When I navigate to the landing page
	Then I am not prompted to log in