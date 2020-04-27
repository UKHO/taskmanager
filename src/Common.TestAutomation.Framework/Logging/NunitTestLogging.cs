using NUnit.Framework;

namespace Common.TestAutomation.Framework.Logging
{
    public class NunitTestLogging : ITestLogging
    {
        public virtual void LogLineBreak()
        {
            Log();
        }

        public void Log(string message = "")
        {
            TestContext.Out.WriteLine(message);
        }
    }
}