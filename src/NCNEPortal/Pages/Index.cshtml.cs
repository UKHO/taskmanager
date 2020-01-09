using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NCNEPortal.Auth;
using Portal.ViewModels;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;


namespace NCNEPortal.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IUserIdentityService _userIdentityService;

        private string _userFullName;
        public string UserFullName
        {
            get => string.IsNullOrEmpty(_userFullName) ? "Unknown user" : _userFullName;
            private set => _userFullName = value;
        }


        public IndexModel(IUserIdentityService userIdentityService)
        {
            _userIdentityService = userIdentityService;
        }

        public async Task OnGetAsync()
        {

           // UserFullName = await _userIdentityService.GetFullNameForUser(this.User);
            
        }

    }
}
