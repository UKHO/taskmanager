using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace Common.TestAutomation.Framework.PageElements
{
    public class Table
    {
        private readonly string _tableCssSelector;
        private readonly WebDriverWait _wait;
        protected readonly IWebDriver Driver;

        public Table(IWebDriver driver, WebDriverWait wait, string tableCssSelector)
        {
            Driver = driver;
            _wait = wait;
            _tableCssSelector = tableCssSelector;
        }

        private IEnumerable<IWebElement> TableRows => TableElement.FindElements(By.CssSelector("tbody > tr"));
        protected virtual IEnumerable<IWebElement> TableHeaders => TableElement.FindElements(By.CssSelector("th"));
        private IWebElement TableElement => Driver.FindElement(By.CssSelector(_tableCssSelector));

        public IEnumerable<string> this[string columnName]
        {
            get
            {
                if (!HasLoaded)
                    throw new ApplicationException($"Cannot query table {_tableCssSelector} as it has not loaded");

                var columnIndex = GetColumnIndex(columnName);

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

        private int GetColumnIndex(string columnName)
        {
            var regexFilter = new Regex("[^-_a-zA-Z0-9]");
            var cleanedColumnName = regexFilter.Replace(columnName, "");

            var columnIndex = TableHeaders.ToList().FindIndex(e =>
            {
                ScrollToElement(e);
                var cleanedWebElementText = regexFilter.Replace(e.Text, "");
                return cleanedWebElementText.Equals(cleanedColumnName, StringComparison.InvariantCultureIgnoreCase);
            });

            return columnIndex;
        }

        public IList<T> GetRows<T>() where T : new()
        {
            return TableRows.Select(MapWebRowToType<T>).ToList();
        }

        private T MapWebRowToType<T>(IWebElement rowWebElement) where T : new()
        {
            var newRow = new T();
            var cellWebElements = rowWebElement.FindElements(By.CssSelector("td"));

            foreach (var property in typeof(T).GetProperties())
            {
                var columnIndex = GetColumnIndex(property.Name);
                if (columnIndex < 0) continue;

                property.SetValue(newRow, cellWebElements[columnIndex].Text);
            }

            return newRow;
        }

        private void ScrollToElement(IWebElement element)
        {
            if (!element.Displayed)
            {
                var actions = new Actions(Driver);
                actions.MoveToElement(element);
                actions.Perform();
            }
        }
    }
}