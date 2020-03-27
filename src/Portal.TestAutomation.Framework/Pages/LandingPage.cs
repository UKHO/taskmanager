using System;
using System.Collections.Generic;
using System.Linq;
using Common.Helpers;
using Common.TestAutomation.Framework.Pages;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Portal.TestAutomation.Framework.Configuration;

namespace Portal.TestAutomation.Framework.Pages
{
    public class LandingPage : PageBase
    {
        public LandingPage(IWebDriver driver, WebDriverWait wait, UrlsConfig urlsConfig)
            : base(driver,
                wait,
                ConfigHelpers.IsAzureDevOpsBuild
                    ? urlsConfig.LandingPageUrl
                    : urlsConfig.LocalDevLandingPageUrl)
        {
        }

        private IWebElement UkhoLogo => Driver.FindElement(By.Id("ukhoLogo"));
        private IWebElement UnassignedTaskTable => Driver.FindElement(By.Id("unassignedTasks"));
        private IWebElement InFlightTaskTable => Driver.FindElement(By.Id("inFlightTasks"));
        private IWebElement GlobalSearchField => Driver.FindElement(By.Id("txtGlobalSearch"));

        private IWebElement ClickOnTask =>
            Driver.FindElement(By.XPath("//*[@id='inFlightTasks']/tbody/tr[1]/td[1]/a"));

        private List<IWebElement> UnassignedTaskTableActualDataRows =>
            UnassignedTaskTable.FindElements(By.XPath("//*[@id='unassignedTasks']/tbody/tr"))
                .Where(r => r.FindElements(By.TagName("td")).Count > 1)
                .ToList();

        private List<IWebElement> InFlightTaskTableActualDataRows =>
            InFlightTaskTable.FindElements(By.XPath("//*[@id='inFlightTasks']/tbody/tr"))
                .Where(r => r.FindElements(By.TagName("td")).Count > 1)
                .ToList();

        public override bool HasLoaded
        {
            get
            {
                try
                {
                    Wait.Until(driver => UkhoLogo.Displayed);
                    return true;
                }
                catch (NoSuchElementException e)
                {
                    Console.WriteLine(e);
                    return false;
                }
            }
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