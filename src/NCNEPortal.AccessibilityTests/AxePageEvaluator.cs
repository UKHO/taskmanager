using System.IO;
using NCNEPortal.AccessibilityTests.AxeModel;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;

namespace NCNEPortal.AccessibilityTests
{
    public class AxePageEvaluator
    {
        private readonly IJavaScriptExecutor _javaScriptExecutor;
        private readonly string _jsToIncludeAxeLibrary;

        public AxePageEvaluator(IJavaScriptExecutor javaScriptExecutor)
        {
            _javaScriptExecutor = javaScriptExecutor;

            var testsDllDirectory = new DirectoryInfo(Path.GetDirectoryName(GetType().Assembly.Location));
            var pathToAxeLibrary = Path.Combine(
                testsDllDirectory.Parent.Parent.Parent.FullName,
                "node_modules",
                "axe-core",
                "axe.min.js");
            _jsToIncludeAxeLibrary = File.ReadAllText(pathToAxeLibrary);
        }

        public AxeResult GetAxeResults()
        {
            _javaScriptExecutor.ExecuteScript(_jsToIncludeAxeLibrary);
            var result = _javaScriptExecutor.ExecuteScript("return await axe.run();");

            var axeResult = new AxeResult(JObject.FromObject(result));
            return axeResult;
        }
    }
}