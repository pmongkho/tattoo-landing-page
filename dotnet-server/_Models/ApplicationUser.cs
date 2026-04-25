using Microsoft.AspNetCore.Identity;

namespace dotnet_server._Models;

public class ApplicationUser : IdentityUser
{
    public ICollection<TattooDeal> TattooDeals { get; set; } = new List<TattooDeal>();
}
