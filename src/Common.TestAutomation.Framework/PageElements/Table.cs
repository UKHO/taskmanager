using System;
using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium;

namespace Common.TestAutomation.Framework.PageElements
{
    public class Table
    {
        private readonly IWebDriver _driver;
        private readonly By _tableSelector;

        public Table(IWebDriver driver, By tableSelector)
        {
            _driver = driver;
            _tableSelector = tableSelector;
        }

        private IEnumerable<IWebElement> TableRows => TableElement.FindElements(By.CssSelector("tbody > tr"));
        private IEnumerable<IWebElement> TableHeaders => TableElement.FindElements(By.CssSelector("th"));
        private IWebElement TableElement => _driver.FindElement(_tableSelector);

        public IEnumerable<string> this[string columnName]
        {
            get
            {
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
    }
}