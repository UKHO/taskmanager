using Common.Helpers.Auth;
using DbUpdatePortal.Auth;
using DbUpdateWorkflowDatabase.EF;
using DbUpdateWorkflowDatabase.EF.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;


namespace DbUpdatePortal.Pages
{
    //[Authorize]
    public class IndexModel : PageModel
    {
       // private readonly IDbUpdateUserDbService _dbUpdateUserDbService;
        private readonly DbUpdateWorkflowDbContext _dbContext;
        private readonly ILogger<IndexModel> _logger;

        //private readonly IAdDirectoryService _adDirectoryService;

        private (string DisplayName, string UserPrincipalName) _currentUser;
        //public (string DisplayName, string UserPrincipalName) CurrentUser
        //{
        //    get
        //    {
        //        if (_currentUser == default) _currentUser = _adDirectoryService.GetUserDetails(this.User);
        //        return _currentUser;
        //    }
        //}
        [BindProperty(SupportsGet = true)]
        public List<TaskInfo> DbUpdateTasks { get; set; }


        public IndexModel(//IDbUpdateUserDbService dbUpdateUserDbService,
                          DbUpdateWorkflowDbContext dbContext,
                          ILogger<IndexModel> logger
                          //,IAdDirectoryService adDirectoryService
                          )
        {
           // _dbUpdateUserDbService = dbUpdateUserDbService;
            _dbContext = dbContext;
            _logger = logger;
            //_adDirectoryService = adDirectoryService;
        }

        public void OnGet()
        {

        }
    }
}
