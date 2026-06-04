using System.ComponentModel.DataAnnotations;

namespace SiteEngine.Services;

public sealed class AdminCreateUserRequest
{
	[Required]
	[EmailAddress]
	[StringLength(256)]
	public string Email { get; set; } = string.Empty;

	public bool IsGlobalAdmin { get; set; }
}
