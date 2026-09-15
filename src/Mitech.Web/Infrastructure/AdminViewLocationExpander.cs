using Microsoft.AspNetCore.Mvc.Razor;

namespace Mitech.Web.Infrastructure;

public class AdminViewLocationExpander : IViewLocationExpander
{
    public void PopulateValues(ViewLocationExpanderContext context) { }

    public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
    {
        var adminControllers = new[]
        {
            "Dashboard", "ProductsAdmin", "NewsAdmin", "PagesAdmin", "ContactAdmin", "Login",
            "HomeSettingsAdmin", "FooterAdmin", "NavAdmin", "JobsAdmin", "MaintenanceAdmin",
            "RecruitmentAdmin"
        };

        if (adminControllers.Contains(context.ControllerName))
        {
            var adminFolder = context.ControllerName switch
            {
                "ProductsAdmin"      => "Products",
                "NewsAdmin"          => "News",
                "PagesAdmin"         => "Pages",
                "ContactAdmin"       => "Contact",
                "HomeSettingsAdmin"  => "HomeSettings",
                "FooterAdmin"        => "Footer",
                "NavAdmin"           => "Nav",
                "JobsAdmin"          => "Jobs",
                "MaintenanceAdmin"   => "Maintenance",
                "RecruitmentAdmin"   => "Recruitment",
                _ => context.ControllerName
            };

            yield return $"/Views/Admin/{adminFolder}/{{0}}.cshtml";
        }

        foreach (var location in viewLocations)
            yield return location;
    }
}
