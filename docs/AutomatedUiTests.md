# Automated UI testing in Portal and NCNE Portal

## Running tests locally

You will need to configure four environment variables:

* ENVIRONMENT
* AZURE_APP_CONFIGURATION_CONNECTION_STRING
* KEY_VAULT_ADDRESS
* ChromeWebDriver

AZURE_APP_CONFIGURATION_CONNECTION_STRING and KEY_VAULT_ADDRESS can be obtained from the Azure resources. It is advisable to use the UAT environment variables as this is the dedicated autotest environment and so is unlikely to be being used for manual testing.

### ENVIRONMENT variable values

* AzureDevOpsBuild - for running against a deployed environment (like UAT)
* AzureDevelopment - for debugging against Azure itself
* LocalDevelopment - for running against a local environment

### Chrome Driver

Chrome driver is used by Selenium to interact with your local browser. The environment variable is used to point Selenium at the folder where your local copy resides, e.g. `C:\ChromeDriver`.

Chrome driver must always be the correct version for your copy of Chrome. If you do not have a copy of chrome driver (or if it is too far out of date) then the tests will not run. You can get the latest version of chrome driver by visiting <https://chromedriver.chromium.org/downloads>.

## Common Test Automation Framework

All test automation projects share a common base framework called `Common.TestAutomation.Framework`. This framework is responsible for Portal-agnostic tasks including:

* setting up the Selenium WebDriver
* automatically logging into a system using Microsoft authentication
* running Axe against a page
* performing common Selenium tasks (such as entering text into a field)
* logging SpecFlow steps
* attaching screenshots in error scenarios

To use this in a new framework, you need to inclde a project reference to it and if it is a SpecFlow project add it to the `specflow.json` file.

## Page Object Models

All page object model (POM) page classes should inherit from `PageBase` found in the `Common.TestAutomation.Framework`. This reduces code duplication and allows classes to utilise common selenium task methods.

Each POM should register an `ILandingPage`, this is used by the authentication page to navigate to the Microsoft Authentication site.

## Logging

`Common.TestAutomation.Framework.Logging` contains SpecFlow hooks to automatically log Features, Scenarios and Steps as they are being run. It also takes screenshots on error and attaches them to the test run.

## Axe

Axe is an open source Web Accessibility analysis tool supported by Deque Systems. Documentation for it can be found here: <https://github.com/dequelabs/axe-core>.

Use the `AxePageEvaluator` in the `Common.TestAutomation.Framework` to run the tool against a page.

```csharp
            var axeResult = _axePageEvaluator.GetAxeResults();

            _axeResultAnalyser.AssertAxeViolations(axeResult);
```

Be aware that the tool only evaluates visible elements on the current page and that it cannot pick up all accessibility violations. Manual testing will still be required to ensure that any given page is compliant with government standards.
