using NCNEPortal.Enums;
using System.Collections.Generic;

namespace NCNEPortal.Helpers
{
    public class WorkflowStageHelper : IWorkflowStageHelper
    {
        public List<NcneTaskStageType> GetNextStagesForCompletion(NcneTaskStageType currentStage, bool v2Available)
        {
            List<NcneTaskStageType> result = new List<NcneTaskStageType>();

            switch (currentStage)
            {
                case NcneTaskStageType.With_SDRA:
                    result.Add(NcneTaskStageType.With_Geodesy);
                    break;
                case NcneTaskStageType.With_Geodesy:
                    result.Add(NcneTaskStageType.Specification);
                    break;
                case NcneTaskStageType.Specification:
                    result.Add(NcneTaskStageType.Compile);
                    break;
                case NcneTaskStageType.Compile:
                    result.Add(NcneTaskStageType.V1);
                    break;
                case NcneTaskStageType.V1:
                    result.Add(v2Available ? NcneTaskStageType.V2 : NcneTaskStageType.Final_Updating);
                    break;
                case NcneTaskStageType.V1_Rework:
                    result.Add(NcneTaskStageType.V1);
                    break;
                case NcneTaskStageType.V2:
                    result.Add(NcneTaskStageType.Final_Updating);
                    break;
                case NcneTaskStageType.V2_Rework:
                    result.Add(NcneTaskStageType.V2);
                    break;
                case NcneTaskStageType.Forms:
                    break;
                case NcneTaskStageType.Final_Updating:
                    result.Add(NcneTaskStageType.Hundred_Percent_Check);
                    break;
                case NcneTaskStageType.Hundred_Percent_Check:
                    result.Add(NcneTaskStageType.Commit_To_Print);
                    break;
                case NcneTaskStageType.Commit_To_Print:
                    result.Add(NcneTaskStageType.CIS);
                    break;
                case NcneTaskStageType.CIS:
                    result.AddRange(new List<NcneTaskStageType>()
                    {
                        NcneTaskStageType.Publication,
                        NcneTaskStageType.Publish_Chart,
                        NcneTaskStageType.Clear_Vector,
                        NcneTaskStageType.Retire_Old_Version,
                        NcneTaskStageType.Consider_Withdrawn_Charts
                    });
                    break;
                case NcneTaskStageType.Publication:
                    break;
                case NcneTaskStageType.Publish_Chart:
                    break;
                case NcneTaskStageType.Clear_Vector:
                    break;
                case NcneTaskStageType.Retire_Old_Version:
                    break;
                case NcneTaskStageType.Consider_Withdrawn_Charts:
                    break;
            }

            return result;
        }
    }
}
