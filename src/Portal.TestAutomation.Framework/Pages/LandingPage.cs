using Common.Helpers;
using Common.TestAutomation.Framework.PageElements;
using Common.TestAutomation.Framework.Pages;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Portal.TestAutomation.Framework.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Portal.TestAutomation.Framework.Pages
{
    public class LandingPage : PageBase, IPage
    {
        public LandingPage(IWebDriver driver, WebDriverWait wait, UrlsConfig urlsConfig)
            : base(driver,
                wait,
                ConfigHelpers.IsAzureDevOpsBuild
                    ? urlsConfig.LandingPageUrl
                    : urlsConfig.LocalDevLandingPageUrl)
        {
            InFlightTaskTable = new DataTable(Driver, Wait, "inFlightTasks");
        }

        private IWebElement UkhoLogo => Driver.FindElement(By.Id("ukhoLogo"));
        private IWebElement UnassignedTaskTable => Driver.FindElement(By.Id("unassignedTasks"));
        private Table InFlightTaskTable { get; }
        private IWebElement GlobalSearchField => Driver.FindElement(By.Id("txtGlobalSearch"));

        private IWebElement ClickOnTask =>
            Driver.FindElement(By.XPath("//*[@id='inFlightTasks']/tbody/tr[1]/td[1]/a"));

        private List<IWebElement> UnassignedTaskTableActualDataRows =>
            UnassignedTaskTable.FindElements(By.XPath("//*[@id='unassignedTasks']/tbody/tr"))
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

        public IList<TaskListEntry> InFlightTasks => InFlightTaskTable.GetRows<TaskListEntry>();
        public string UserName => Driver.FindElement(By.Id("userFullName")).Text.Replace("Hello ", "");

        public void FilterRowsByProcessIdInGlobalSearch(int processId)
        {
            GlobalSearchField.SendKeys(processId.ToString());
        }
    }
}