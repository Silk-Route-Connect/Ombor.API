using Microsoft.AspNetCore.Identity;
using Ombor.Domain.Enums;
namespace Ombor.Domain.Entities;

public class UserAccount : IdentityUser<int>
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string TelegramAccount { get; set; }
    public UserAccess Access { get; set; }

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; }

    public UserAccount()
    {
        RefreshTokens = [];
    }
}
