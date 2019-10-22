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
        private IWebElement AssignedTaskTable => _driver.FindElement(By.Id("inFlightTasks"));
        private IWebElement GlobalSearchField => _driver.FindElement(By.Id("txtGlobalSearch"));
        private IWebElement ClickOnTask => _driver.FindElement(By.XPath("//*[@id='inFlightTasks']/tbody/tr[1]/td[1]/a"));
        private List<IWebElement> UnassignedTaskTableRows => UnassignedTaskTable.FindElements(By.TagName("tr")).ToList();
        private List<IWebElement> AssignedTaskTableRows => AssignedTaskTable.FindElements(By.TagName("tr")).ToList();


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
            // Ensure we only have the one row (including the header row) for each filtered table
            if (UnassignedTaskTableRows.Count > 2 || AssignedTaskTableRows.Count > 3)
            {
                return false;
            }

            // Checks both unassigned and assigned tables for correct process id
            return UnassignedTaskTableRows[1].FindElements(By.TagName("td")).Count != 0 && int.Parse(UnassignedTaskTableRows[1].FindElements(By.TagName("td"))[0].Text) == processId
                   && AssignedTaskTableRows[1].FindElements(By.TagName("td")).Count != 0 && int.Parse(AssignedTaskTableRows[1].FindElements(By.TagName("td"))[0].Text) == processId;
        }
    }
}
