namespace SiteEngine.Services;

public sealed class AdminSiteDetail
{
	public required Guid SiteId { get; init; }
	public required string PtaId { get; init; }
	public required string Hostname { get; init; }
	public required string Domain { get; init; }
	public required string SiteName { get; init; }
	public required bool IsAdminPortal { get; init; }
	public required bool IsCityWide { get; init; }
	public required string LogoUrl { get; init; }
	public required string BannerUrl { get; init; }
	public required string PrimaryColor { get; init; }
	public required string AccentColor { get; init; }
	public required string WelcomeText { get; init; }
	public required int AnnouncementCount { get; init; }
	public required int EventCount { get; init; }
	public required int AssignedUsers { get; init; }
	public required int AdminCount { get; init; }
	public required int BoardMemberCount { get; init; }
	public required int VolunteerCount { get; init; }
}
