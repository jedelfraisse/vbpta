using Microsoft.AspNetCore.Identity;
using SiteEngine.Entities;

namespace SiteEngine.Identity;

public class SiteUser : IdentityUser
{
	public ICollection<SiteUserRole> SiteRoles { get; set; } = new List<SiteUserRole>();
}
