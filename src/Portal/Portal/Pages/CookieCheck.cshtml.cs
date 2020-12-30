using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Portal.Pages
{
    public class CookieCheckModel : PageModel
    {
        public IActionResult OnGet()
        {
            var blah = this.User.Identity.IsAuthenticated;

            if (blah)
            {
                return new OkResult();
            }
            else
            {
                return new UnauthorizedResult();
            }
        }
    }
}
