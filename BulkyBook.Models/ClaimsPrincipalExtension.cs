using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.Models
{
    public static class ClaimsPrincipalExtension
    {
        public static string GetName(this ClaimsPrincipal principal)
        {
            var Name = principal.Claims.FirstOrDefault(c => c.Type == "Name");
            return Name?.Value;
        }
    }

}
