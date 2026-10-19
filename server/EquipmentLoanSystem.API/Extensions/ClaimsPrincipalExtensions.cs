using System.Security.Claims;
using EquipmentLoanSystem.Core.Constants;

namespace EquipmentLoanSystem.API.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetMemberId(this ClaimsPrincipal user)
        {
            var value = user.FindFirst(AppClaimTypes.MemberId)?.Value;
            return int.TryParse(value, out var id) ? id : 0;
        }

        public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(Roles.Admin);
    }
}
