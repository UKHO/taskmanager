﻿using NCNEPortal.Enums;
using System.Collections.Generic;

namespace NCNEPortal.Helpers
{
    public interface IWorkflowStageHelper
    {

        List<NcneTaskStageType> GetNextStagesForCompletion(NcneTaskStageType currentStage, bool v2Available,
            bool withdrawal);

        NcneTaskStageType GetNextStageForRework(NcneTaskStageType currentStage);
    }
}
