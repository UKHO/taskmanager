using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Common.TestAutomation.Framework.PageElements
{
    public class DataTable : Table
    {
        private readonly string _tableHeadersTableXpathSelector;

        public DataTable(IWebDriver driver, WebDriverWait wait, string tableId) : base(driver, wait, $"#{tableId}")
        {
            _tableHeadersTableXpathSelector =
                $"//*[@id='{tableId}']/../../div[contains(@class, 'dataTables_scrollHead')]/div/table";
        }

        protected override IEnumerable<IWebElement> TableHeaders =>
            TableHeadersTableElement.FindElements(By.CssSelector("th"));

        private IWebElement TableHeadersTableElement => Driver.FindElement(By.XPath(_tableHeadersTableXpathSelector));
    }
}