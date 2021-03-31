using NCNEPortal.Enums;
using NCNEPortal.Helpers;
using NUnit.Framework;

namespace NCNEPortal.UnitTests
{
    [TestFixture]
    public class WorkflowstageTests
    {
        private IWorkflowStageHelper _workWorkflowStageHelper;

        [SetUp]
        public void Setup()
        {
            _workWorkflowStageHelper = new WorkflowStageHelper();

        }

        [TestCase(NcneTaskStageType.With_SDRA, NcneTaskStageType.With_Geodesy, false)]
        [TestCase(NcneTaskStageType.With_Geodesy, NcneTaskStageType.Specification, false)]
        [TestCase(NcneTaskStageType.Specification, NcneTaskStageType.Compile, false)]
        [TestCase(NcneTaskStageType.Compile, NcneTaskStageType.V1, false)]
        [TestCase(NcneTaskStageType.V1, NcneTaskStageType.V2, false)]
        [TestCase(NcneTaskStageType.V1_Rework, NcneTaskStageType.V1, false)]
        [TestCase(NcneTaskStageType.V2_Rework, NcneTaskStageType.V2, false)]
        [TestCase(NcneTaskStageType.V2, NcneTaskStageType.Final_Updating, false)]
        [TestCase(NcneTaskStageType.Final_Updating, NcneTaskStageType.Hundred_Percent_Check, false)]
        [TestCase(NcneTaskStageType.Hundred_Percent_Check, NcneTaskStageType.Commit_To_Print, false)]
        [TestCase(NcneTaskStageType.Commit_To_Print, NcneTaskStageType.CIS, false)]
        [TestCase(NcneTaskStageType.Withdrawal_action, NcneTaskStageType.V1, true)]
        [TestCase(NcneTaskStageType.V1, NcneTaskStageType.CIS, true)]
        [TestCase(NcneTaskStageType.CIS, NcneTaskStageType.PMC_withdrawal, true)]

        public void Validate_Get_NextStep_for_Completion_return_single_step(NcneTaskStageType currentStage, NcneTaskStageType nextStage, bool withdrawal)
        {
            var result = _workWorkflowStageHelper.GetNextStagesForCompletion(currentStage, true, withdrawal);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(nextStage, result[0]);
        }

        [TestCase(NcneTaskStageType.Forms, false)]
        [TestCase(NcneTaskStageType.Publication, false)]
        [TestCase(NcneTaskStageType.Publish_Chart, false)]
        [TestCase(NcneTaskStageType.Clear_Vector, false)]
        [TestCase(NcneTaskStageType.Retire_Old_Version, false)]
        [TestCase(NcneTaskStageType.Consider_Withdrawn_Charts, false)]
        [TestCase(NcneTaskStageType.PMC_withdrawal, true)]
        public void Validate_Get_NextStep_for_Completion_return_no_next_step(NcneTaskStageType currentStage, bool withdrawal)
        {
            var result = _workWorkflowStageHelper.GetNextStagesForCompletion(currentStage, true, withdrawal);

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Validate_Get_NextStep_for_Completion_return_multiple_steps()
        {
            NcneTaskStageType currentStage = NcneTaskStageType.CIS;

            var result = _workWorkflowStageHelper.GetNextStagesForCompletion(currentStage, true, false);

            Assert.AreEqual(5, result.Count);
            CollectionAssert.Contains(result, NcneTaskStageType.Publication);
            CollectionAssert.Contains(result, NcneTaskStageType.Publish_Chart);
            CollectionAssert.Contains(result, NcneTaskStageType.Clear_Vector);
            CollectionAssert.Contains(result, NcneTaskStageType.Retire_Old_Version);
            CollectionAssert.Contains(result, NcneTaskStageType.Consider_Withdrawn_Charts);
        }

        [Test]
        public void Validate_Get_NextStep_for_Completion_skips_V2_if_not_available()
        {
            NcneTaskStageType currentStage = NcneTaskStageType.V1;

            var result = _workWorkflowStageHelper.GetNextStagesForCompletion(currentStage, false, false);

            Assert.AreEqual(1, result.Count);
            CollectionAssert.Contains(result, NcneTaskStageType.Final_Updating);
        }

        [TestCase(NcneTaskStageType.V1, NcneTaskStageType.V1_Rework)]
        [TestCase(NcneTaskStageType.V2, NcneTaskStageType.V2_Rework)]
        public void Validate_Get_NextStep_for_Rework(NcneTaskStageType currentStage, NcneTaskStageType nextStage)
        {
            var result = _workWorkflowStageHelper.GetNextStageForRework(currentStage);

            Assert.AreEqual(nextStage, result);
        }
    }
}
