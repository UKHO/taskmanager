using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.RegularExpressions;

namespace NCNEPortal.Helpers
{
    public class JavascriptError : IExceptionFilter
    {
        public JavascriptError()
        {

        }

        public void OnException(ExceptionContext context)
        {
            var errorMessage = (context.Exception.InnerException == null) ? context.Exception.Message : context.Exception.InnerException.Message;
            errorMessage = Regex.Replace(errorMessage, @"\s+", " "); // replace \r \n \t ... with normal space

            var response = context.HttpContext.Response;
            response.StatusCode = 500;
            response.Headers.Add("Error", errorMessage);
            context.ExceptionHandled = true;
        }
    }
}
