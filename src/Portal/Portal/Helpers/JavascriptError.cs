using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing.Template;

namespace Portal.Helpers
{
    public class JavascriptError : IExceptionFilter
    {
        public JavascriptError()
        {
            
        }

        public void OnException(ExceptionContext context)
        {
            var response = context.HttpContext.Response;
            response.StatusCode = 500;
            response.Headers.Add("Error", context.Exception.Message);
            context.ExceptionHandled = true;
        }
    }
}
