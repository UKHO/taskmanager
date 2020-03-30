# Automated UI testing in Portal and NCNE Portal

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

`Common.TestAutomation.Framework.Logging` contains SpecFlow hooks to automatically log Features, Scenarios and Steps as they are being run. It also takes screenshots on error and attaches them to the test run. Logs can be accessed from the test result.

A `Log` method is provided for any further information that may need logging during test execution.