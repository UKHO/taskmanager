using System;
using System.IO;
using System.Linq;
using BoDi;
using NUnit.Framework;
using OpenQA.Selenium;
using TechTalk.SpecFlow;

namespace Common.TestAutomation.Framework
{
    [Binding]
    public class Logging
    {
        private readonly FeatureContext _featureContext;
        private readonly IObjectContainer _objectContainer;
        private readonly ScenarioContext _scenarioContext;

        public Logging(FeatureContext featureContext, ScenarioContext scenarioContext, IObjectContainer objectContainer)
        {
            _featureContext = featureContext;
            _scenarioContext = scenarioContext;
            _objectContainer = objectContainer;
        }

        [BeforeScenario(Order = 0)]
        public void LogScenarioStart()
        {
            LogLineBreak();
            Log("--------------------------------------------------------------");
            Log($"Feature:  {_featureContext.FeatureInfo.Title}");
            Log($"Scenario: --{_scenarioContext.ScenarioInfo.Title}" +
                (_scenarioContext.ScenarioInfo.Tags.Any()
                    ? $" [{string.Join(", ", _scenarioContext.ScenarioInfo.Tags)}]"
                    : ""));
            Log("Executing BeforeScenario steps...........");
        }

        [BeforeStep]
        public void LogStep()
        {
            var stepInstance = _scenarioContext.StepContext.StepInfo.StepInstance;
            Log($"Executing Step: ----{stepInstance.Keyword} {stepInstance.Text}");
        }

        [AfterStep]
        public void LogLineBreak()
        {
            Log();
        }

        [AfterScenario]
        public void LogScenarioError()
        {
            if (_scenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.TestError)
            {
                Log("Error encountered: " + _scenarioContext.TestError.Message);
                Log(_scenarioContext.TestError.StackTrace);
            }
            else if (TestContext.CurrentContext.Result.FailCount > 0)
            {
                Log("Error encountered: " + TestContext.CurrentContext.Result.Message);
                Log(TestContext.CurrentContext.Result.StackTrace);
            }
        }

        [AfterScenario]
        public void AttachScreenShotOnError()
        {
            if (_objectContainer.IsRegistered<ITakesScreenshot>()
                && (_scenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.TestError
                    || TestContext.CurrentContext.Result.FailCount > 0))
            {
                var driver = _objectContainer.Resolve<ITakesScreenshot>();

                var screenShot = driver.GetScreenshot();
                var screenshotFileName =
                    $"{DateTime.Now:yyyy-MM-dd-hh-mm-ss}_{TestContext.CurrentContext.Test.Name}.png";

                var tempFilePath = $"{Path.GetTempPath()}{screenshotFileName}";
                screenShot.SaveAsFile(tempFilePath);
                TestContext.AddTestAttachment(tempFilePath);
            }
        }

        public void Log(string message = "")
        {
            TestContext.Out.WriteLine(message);
        }
    }
}