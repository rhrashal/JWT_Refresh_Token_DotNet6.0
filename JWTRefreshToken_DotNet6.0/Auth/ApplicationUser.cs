using Microsoft.AspNetCore.Identity;

namespace JWTRefreshToken_DotNet6._0.Auth
{
    public class ApplicationUser : IdentityUser
    {
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }
    }
}
