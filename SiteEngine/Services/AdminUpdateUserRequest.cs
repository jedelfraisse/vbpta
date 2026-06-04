using System.ComponentModel.DataAnnotations;

namespace SiteEngine.Services;

public sealed class AdminUpdateUserRequest
{
	[Required]
	[EmailAddress]
	[StringLength(256)]
	public string Email { get; set; } = string.Empty;

	public bool IsGlobalAdmin { get; set; }
}
