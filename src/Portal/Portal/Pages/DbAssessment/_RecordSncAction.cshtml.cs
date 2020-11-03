using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Portal.Helpers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    [Authorize]
    public class _RecordSncActionModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly ITaskDataHelper _taskDataHelper;

        [BindProperty(SupportsGet = true)]
        public int ProcessId { get; set; }

        [DisplayName("Action:")]
        public bool SncActioned { get; set; }

        [DisplayName("Change:")]
        public string SncActionChangeDetails { get; set; }

        public List<SncAction> SncActions { get; set; }

        public SelectList SncActionTypes { get; set; }

        public _RecordSncActionModel(WorkflowDbContext dbContext, ITaskDataHelper taskDataHelper)
        {
            _dbContext = dbContext;
            _taskDataHelper = taskDataHelper;
        }


        public async Task OnGetAsync(int processId, string taskStage)
        {
            ProcessId = processId;
            await PopulateSncActionTypes();
            await SetSncActionFromDb();
            await SetSncActionDataFromDb(taskStage);
        }


        private async Task PopulateSncActionTypes()
        {
            var sncActionTypes = await _dbContext.SncActionType.ToListAsync();
            SncActionTypes = new SelectList(sncActionTypes, nameof(SncActionType.SncActionTypeId), nameof(SncActionType.Name));
        }

        private async Task SetSncActionFromDb()
        {
            SncActions = await _dbContext.SncAction
                .Include(x => x.SncActionType)
                .Where(p => p.ProcessId == ProcessId)
                .ToListAsync();

            if (SncActions == null || SncActions.Count == 0)
            {
                SncActions = new List<SncAction>()
                {
                    new SncAction()
                };
            }
        }

        private async Task SetSncActionDataFromDb(string taskStage)
        {
            var dbAssessmentData = await _taskDataHelper.GetProductActionData(taskStage, ProcessId);

            if (dbAssessmentData != null)
            {
                SncActioned = dbAssessmentData.SncActioned;
                SncActionChangeDetails = dbAssessmentData.SncActionChangeDetails;
            }
        }
    }
}