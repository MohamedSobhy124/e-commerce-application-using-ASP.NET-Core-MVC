using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using IdealWeightNutrition.Utility;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;

namespace IdealWeightNutrition.Helpers
{
    /// <summary>
    /// HTML Helper extensions for generating URLs with encrypted IDs
    /// </summary>
    public static class EncryptedIdHtmlHelper
    {
        /// <summary>
        /// Generates an encrypted ID for use in URLs
        /// </summary>
        public static string EncryptedId(this IHtmlHelper htmlHelper, int id)
        {
            return IdEncryptionHelper.EncryptId(id);
        }

        /// <summary>
        /// Generates an action link with encrypted ID
        /// </summary>
        public static Microsoft.AspNetCore.Html.IHtmlContent EncryptedActionLink(
            this IHtmlHelper htmlHelper,
            string linkText,
            string actionName,
            string controllerName,
            int id,
            object? htmlAttributes = null,
            string? area = null)
        {
            var encryptedId = IdEncryptionHelper.EncryptId(id);
            var routeValues = new { id = encryptedId };
            
            var tagBuilder = new TagBuilder("a");
            tagBuilder.InnerHtml.AppendHtml(linkText);
            
            var urlHelperFactory = htmlHelper.ViewContext.HttpContext.RequestServices.GetRequiredService<IUrlHelperFactory>();
            var urlHelper = urlHelperFactory.GetUrlHelper(htmlHelper.ViewContext);
            string url;
            
            if (!string.IsNullOrEmpty(area))
            {
                url = urlHelper.Action(actionName, controllerName, new { area = area, id = encryptedId });
            }
            else
            {
                url = urlHelper.Action(actionName, controllerName, new { id = encryptedId });
            }
            
            tagBuilder.Attributes.Add("href", url);
            
            if (htmlAttributes != null)
            {
                foreach (var prop in htmlAttributes.GetType().GetProperties())
                {
                    var value = prop.GetValue(htmlAttributes)?.ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        tagBuilder.Attributes.Add(prop.Name, value);
                    }
                }
            }
            
            return tagBuilder;
        }
    }
}

