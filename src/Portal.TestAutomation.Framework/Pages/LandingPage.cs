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
        private List<IWebElement> UnassignedTaskTableRows => UnassignedTaskTable.FindElements(By.TagName("tr")).ToList();


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
        }

        public IWebElement FindTaskByProcessId(int processId)
        {
            var moo = UnassignedTaskTableRows;

            foreach (var row in UnassignedTaskTableRows)
            {
                var cells = row.FindElements(By.TagName("td"));

                if (!cells.Any()) continue;

                if (int.Parse(cells[0].Text) == processId)
                {
                    return row;
                }
            }

            return null;
        }
    }
}
