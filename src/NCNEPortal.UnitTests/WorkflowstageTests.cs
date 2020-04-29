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

        [TestCase(NcneTaskStageType.With_SDRA, NcneTaskStageType.With_Geodesy)]
        [TestCase(NcneTaskStageType.With_Geodesy, NcneTaskStageType.Specification)]
        [TestCase(NcneTaskStageType.Specification, NcneTaskStageType.Compile)]
        [TestCase(NcneTaskStageType.Compile, NcneTaskStageType.V1)]
        [TestCase(NcneTaskStageType.V1, NcneTaskStageType.V2)]
        [TestCase(NcneTaskStageType.V1_Rework, NcneTaskStageType.V1)]
        [TestCase(NcneTaskStageType.V2_Rework, NcneTaskStageType.V2)]
        [TestCase(NcneTaskStageType.V2, NcneTaskStageType.Final_Updating)]
        [TestCase(NcneTaskStageType.Final_Updating, NcneTaskStageType.Hundred_Percent_Check)]
        [TestCase(NcneTaskStageType.Hundred_Percent_Check, NcneTaskStageType.Commit_To_Print)]
        [TestCase(NcneTaskStageType.Commit_To_Print, NcneTaskStageType.CIS)]


        public void Validate_Get_NextStep_for_Completion_return_single_step(NcneTaskStageType currentStage, NcneTaskStageType nextStage)
        {
            var result = _workWorkflowStageHelper.GetNextStagesForCompletion(currentStage, true);

            Assert.AreEqual(result.Count, 1);
            Assert.AreEqual(result[0], nextStage);
        }

        [TestCase(NcneTaskStageType.Forms)]
        [TestCase(NcneTaskStageType.Publication)]
        [TestCase(NcneTaskStageType.Publish_Chart)]
        [TestCase(NcneTaskStageType.Clear_Vector)]
        [TestCase(NcneTaskStageType.Retire_Old_Version)]
        [TestCase(NcneTaskStageType.Consider_Withdrawn_Charts)]
        public void Validate_Get_NextStep_for_Completion_return_no_next_step(NcneTaskStageType currentStage)
        {
            var result = _workWorkflowStageHelper.GetNextStagesForCompletion(currentStage, true);

            Assert.AreEqual(result.Count, 0);

        }

        [Test]
        public void Validate_Get_NextStep_for_Completion_return_multiple_steps()
        {

            NcneTaskStageType currentStage = NcneTaskStageType.CIS;

            var result = _workWorkflowStageHelper.GetNextStagesForCompletion(currentStage, true);

            Assert.AreEqual(result.Count, 5);
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

            var result = _workWorkflowStageHelper.GetNextStagesForCompletion(currentStage, false);

            Assert.AreEqual(result.Count, 1);
            CollectionAssert.Contains(result, NcneTaskStageType.Final_Updating);
        }
    }
}
