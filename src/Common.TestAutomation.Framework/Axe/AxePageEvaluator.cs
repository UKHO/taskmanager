using System;
using System.IO;
using Common.TestAutomation.Framework.Axe.AxeModel;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;

namespace Common.TestAutomation.Framework.Axe
{
    public class AxePageEvaluator
    {
        private readonly IJavaScriptExecutor _javaScriptExecutor;
        private readonly string _jsToIncludeAxeLibrary;

        public AxePageEvaluator(IJavaScriptExecutor javaScriptExecutor)
        {
            _javaScriptExecutor = javaScriptExecutor;

            var axeMinJsResourceLocation = "Common.TestAutomation.Framework.node_modules.axe_core.axe.min.js";
            using (var manifestResourceStream = GetType().Assembly.GetManifestResourceStream(axeMinJsResourceLocation))
            {
                using (var sr = new StreamReader(manifestResourceStream ?? throw new ApplicationException("axe.min.js not found")))
                {
                    _jsToIncludeAxeLibrary = sr.ReadToEnd();
                }
            }
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