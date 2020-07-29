using System;
using System.Collections.ObjectModel;
using System.Drawing;
using BoDi;
using Common.TestAutomation.Framework.Logging;
using Common.TestAutomation.Framework.Pages;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.Events;
using OpenQA.Selenium.Support.UI;
using TechTalk.SpecFlow;

namespace Common.TestAutomation.Framework
{
    [Binding]
    public sealed class WebDriverSupport : IDisposable
    {
        private readonly Lazy<MicrosoftAuthPage> _lazyAuthPage;
        private readonly IObjectContainer _objectContainer;
        private bool _skipLogin;
        private IWebDriver _webDriver;

        public WebDriverSupport(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;
            _lazyAuthPage = new Lazy<MicrosoftAuthPage>(() => _objectContainer.Resolve<MicrosoftAuthPage>());
        }

        private MicrosoftAuthPage AuthPage => _lazyAuthPage.Value;

        public void Dispose()
        {
            DisposeWebdriver();
        }

        [BeforeScenario(Order = 10)]
        public void InitializeWebDriver()
        {
            var chromeDriverDirectory = Environment.GetEnvironmentVariable("ChromeWebDriver");
            if (string.IsNullOrEmpty(chromeDriverDirectory))
                throw new ApplicationException("Missing environment variable: ChromeWebDriver");

            try
            {
                var logger = _objectContainer.Resolve<ITestLogging>();

                var eventDriver = new EventFiringWebDriver(new ChromeDriver(chromeDriverDirectory));
                eventDriver.Navigating += (sender, e) => logger.Log($"    Navigating to {e.Url}");

                _webDriver = eventDriver;
                _webDriver.Manage().Window.FullScreen();
                _webDriver.Manage().Window.Maximize();

                _objectContainer.RegisterInstanceAs(_webDriver);
                _objectContainer.RegisterInstanceAs((IJavaScriptExecutor) _webDriver);
                _objectContainer.RegisterInstanceAs((ITakesScreenshot) _webDriver);
                _objectContainer.RegisterInstanceAs(new WebDriverWait(_webDriver, TimeSpan.FromSeconds(10)));
            }
            catch
            {
                DisposeWebdriver();
                throw;
            }
        }

        [BeforeScenario("skipLogin", Order = 20)]
        public void SkipLogin()
        {
            _skipLogin = true;
        }

        [BeforeScenario(Order = 21)]
        public void SetLoginCookies()
        {
            if (_skipLogin)
                return;

            if (CookieStore.Cookies == null)
                DoLogin();
            else
                RestoreCookies();

            NavigateToDefaultSeleniumStartupPage();
        }

        private void NavigateToDefaultSeleniumStartupPage()
        {
            _webDriver.Navigate().GoToUrl("data:,");
        }

        private void RestoreCookies()
        {
            //Need to be on the auth page so we are in the same domain as the cookies
            AuthPage.NavigateTo();

            foreach (var cookie in CookieStore.Cookies)
            {
                if (cookie.Expiry.HasValue && cookie.Expiry.Value < DateTime.Now.AddMinutes(5))
                {
                    DoLogin();
                    break;
                }

                _webDriver.Manage().Cookies.AddCookie(cookie);
            }
        }

        private void DoLogin()
        {
            AuthPage.NavigateTo();

            if (AuthPage.HasLoaded)
                AuthPage.Login();
            else
                throw new ApplicationException("Could not get to Microsoft authentication page");
        }

        [AfterScenario]
        public void DisposeWebdriver()
        {
            _webDriver?.Quit();
            _webDriver?.Dispose();
            _webDriver = null;
        }
    }

    internal static class CookieStore
    {
        public static ReadOnlyCollection<Cookie> Cookies { get; private set; }

        public static void SaveCookies(ICookieJar cookieJar)
        {
            Cookies = cookieJar.AllCookies;
        }
    }
}