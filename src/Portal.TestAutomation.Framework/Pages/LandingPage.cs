using System;
using System.Collections.Generic;
using System.Linq;
using Common.Helpers;
using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Portal.TestAutomation.Framework.Configuration;

namespace Portal.TestAutomation.Framework.Pages
{
    public class LandingPage : BasePage
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;
        private readonly LandingPageConfig _config = new LandingPageConfig();

        private Uri LandingPageUrl;

        private IWebElement UkhoLogo => _driver.FindElement(By.Id("ukhoLogo"));
        private IWebElement UnassignedTaskTable => _driver.FindElement(By.Id("unassignedTasks"));
        private IWebElement InFlightTaskTable => _driver.FindElement(By.Id("inFlightTasks"));
        private IWebElement GlobalSearchField => _driver.FindElement(By.Id("txtGlobalSearch"));
        private IWebElement ClickOnTask => _driver.FindElement(By.XPath("//*[@id='inFlightTasks']/tbody/tr[1]/td[1]/a"));

        private List<IWebElement> UnassignedTaskTableActualDataRows =>
            UnassignedTaskTable.FindElements(By.XPath("//*[@id='unassignedTasks']/tbody/tr"))
                .Where(r => r.FindElements(By.TagName("td")).Count > 1)
                .ToList();
        private List<IWebElement> InFlightTaskTableActualDataRows =>
            InFlightTaskTable.FindElements(By.XPath("//*[@id='inFlightTasks']/tbody/tr"))
            .Where(r => r.FindElements(By.TagName("td")).Count > 1)
            .ToList();

        public LandingPage(IWebDriver driver, int seconds)
        {
            var configRoot = AzureAppConfigConfigurationRoot.Instance;
            configRoot.GetSection("urls").Bind(_config);

            LandingPageUrl = ConfigHelpers.IsAzureDevOpsBuild ? _config.LandingPageUrl : _config.LocalDevLandingPageUrl;

            _driver = driver;
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(seconds));
        }

        public override bool HasLoaded()
        {
            try
            {
                _wait.Until(driver => UkhoLogo.Displayed);
                return true;
            }
            catch (NoSuchElementException e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        public void NavigateTo()
        {
            _driver.Navigate().GoToUrl(LandingPageUrl);
            _driver.Manage().Window.Maximize();
        }

        public void FilterRowsByProcessIdInGlobalSearch(int processId)
        {
            GlobalSearchField.SendKeys(processId.ToString());
        }

        public bool FindTaskByProcessId(int processId)
        {
            if (UnassignedTaskTableActualDataRows.Count > 0 && InFlightTaskTableActualDataRows.Count > 0) return false;

            foreach (var row in UnassignedTaskTableActualDataRows)
            {
                int.TryParse(row.FindElements(By.TagName("td"))[0].Text, out var found);
                if (found == processId) return true;
            }

            foreach (var row in InFlightTaskTableActualDataRows)
            {
                int.TryParse(row.FindElements(By.TagName("td"))[1].Text, out var found);
                if (found == processId) return true;
            }

            return false;
        }
    }
}
