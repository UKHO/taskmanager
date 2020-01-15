using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NCNEPortal.Auth;
using NCNEWorkflowDatabase.EF;
using System.Threading.Tasks;


namespace NCNEPortal.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IUserIdentityService _userIdentityService;
        private readonly NcneWorkflowDbContext _ncneWorkflowDbContext;

        private string _userFullName;
        public string UserFullName
        {
            get => string.IsNullOrEmpty(_userFullName) ? "Unknown user" : _userFullName;
            private set => _userFullName = value;
        }


        public IndexModel(IUserIdentityService userIdentityService, NcneWorkflowDbContext ncneWorkflowDbContext
                         )
        {
            _userIdentityService = userIdentityService;
            _ncneWorkflowDbContext = ncneWorkflowDbContext;
        }

        public async Task OnGetAsync()
        {

             UserFullName = await _userIdentityService.GetFullNameForUser(this.User);

        }

    }
}
