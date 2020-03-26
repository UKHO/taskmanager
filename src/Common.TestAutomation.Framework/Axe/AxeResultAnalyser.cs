using System;
using System.Linq;
using System.Text;
using Common.TestAutomation.Framework.Axe.AxeModel;
using NUnit.Framework;

namespace Common.TestAutomation.Framework.Axe
{
    public class AxeResultAnalyser
    {
        public void AssertAxeViolations(AxeResult axeResult)
        {
            var seriousViolations = axeResult.Violations
                .Where(v => v.Impact.Equals("serious", StringComparison.InvariantCultureIgnoreCase)).ToArray();
            var otherViolations = axeResult.Violations.Except(seriousViolations).ToArray();

            var seriousViolationErrorMessage = BuildViolationErrorMessage(seriousViolations, "serious");
            var otherViolationMessage = BuildViolationErrorMessage(otherViolations, "other");

            TestContext.Out.WriteLine("Full Axe result json: ");
            TestContext.Out.Write(axeResult.ToString());

            if (seriousViolations.Any())
                Assert.Fail(seriousViolationErrorMessage.AppendLine().Append(otherViolationMessage).ToString());
            else if (otherViolations.Any())
                Assert.Inconclusive(otherViolationMessage.ToString());
            else
                Assert.Pass("No accessibility violations found with Axe");
        }

        private StringBuilder BuildViolationErrorMessage(AxeResultItem[] violations, string violationType)
        {
            var violationErrorMessage = new StringBuilder();

            if (violations.Any())
            {
                violationErrorMessage.AppendLine(
                    $"Found {violations.Length} {violationType} accessibility violation(s):");

                for (var i = 0; i < violations.Length; i++)
                {
                    var violation = violations[i];

                    violationErrorMessage.AppendLine(
                        $"{i + 1}) {violation.Id}: {violation.Help} ({violation.HelpUrl})");
                }

                TestContext.Out.WriteLine();
                TestContext.Out.WriteLine(violationErrorMessage.ToString());
                TestContext.Out.WriteLine("See Axe result json at the bottom for full details");
            }
            else
            {
                TestContext.Out.WriteLine($"No {violationType} accessibility violations found");
            }

            TestContext.Out.WriteLine();
            TestContext.Out.WriteLine();
            return violationErrorMessage;
        }
    }
}