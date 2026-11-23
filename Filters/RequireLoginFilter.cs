using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Camstep.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AllowGuestAttribute : Attribute { }

public sealed class RequireLoginFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var isPublic = context.ActionDescriptor.EndpointMetadata
            .OfType<AllowGuestAttribute>()
            .Any();

        if (isPublic) return;

        if (context.HttpContext.Session.GetInt32("UserId") == null)
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
