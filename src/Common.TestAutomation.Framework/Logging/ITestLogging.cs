namespace Common.TestAutomation.Framework.Logging
{
    public interface ITestLogging
    {
        void LogLineBreak();
        void Log(string message = "");
    }
}