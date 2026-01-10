using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using IdealWeightNutrition.Utility;

namespace IdealWeightNutrition.Helpers
{
    /// <summary>
    /// Extension methods for IUrlHelper to generate URLs with encrypted IDs
    /// </summary>
    public static class UrlHelperExtensions
    {
        /// <summary>
        /// Generates an action URL with encrypted ID
        /// </summary>
        public static string? EncryptedAction(
            this IUrlHelper urlHelper,
            string? actionName,
            string? controllerName = null,
            int? id = null,
            object? routeValues = null,
            string? protocol = null,
            string? host = null,
            string? fragment = null)
        {
            RouteValueDictionary values;
            
            if (routeValues != null)
            {
                if (routeValues is RouteValueDictionary rvd)
                {
                    values = new RouteValueDictionary(rvd);
                }
                else
                {
                    values = new RouteValueDictionary(routeValues);
                }
            }
            else
            {
                values = new RouteValueDictionary();
            }

            if (id.HasValue)
            {
                values["id"] = IdEncryptionHelper.EncryptId(id.Value);
            }

            return urlHelper.Action(actionName, controllerName, values, protocol, host, fragment);
        }

        /// <summary>
        /// Generates an encrypted ID string
        /// </summary>
        public static string EncryptId(int id)
        {
            return IdEncryptionHelper.EncryptId(id);
        }
    }
}

