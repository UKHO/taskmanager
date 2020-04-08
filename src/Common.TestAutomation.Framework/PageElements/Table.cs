using System;
using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Common.TestAutomation.Framework.PageElements
{
    public class Table
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;
        private readonly string _tableCssSelector;

        public Table(IWebDriver driver, WebDriverWait wait, string tableCssSelector)
        {
            _driver = driver;
            _wait = wait;
            _tableCssSelector = tableCssSelector;
        }

        private IEnumerable<IWebElement> TableRows => TableElement.FindElements(By.CssSelector("tbody > tr"));
        private IEnumerable<IWebElement> TableHeaders => TableElement.FindElements(By.CssSelector("th"));
        private IWebElement TableElement => _driver.FindElement(By.CssSelector(_tableCssSelector));

        public IEnumerable<string> this[string columnName]
        {
            get
            {
                if (!HasLoaded)
                    throw new ApplicationException($"Cannot query table {_tableCssSelector} as it has not loaded");

                var columnIndex = TableHeaders.ToList().FindIndex(e =>
                    e.Text.Equals(columnName, StringComparison.InvariantCultureIgnoreCase));

                var values = TableRows.Select(row =>
                {
                    var cells = row.FindElements(By.CssSelector("td"));
                    return cells[columnIndex].Text;
                });

                return values;
            }
        }

        private bool HasLoaded
        {
            get
            {
                try
                {
                    _wait.Until(driver => TableElement.Displayed);

                    return true;
                }
                catch (NoSuchElementException)
                {
                    return false;
                }
            }
        }
    }
}