using SkiaSharp;

namespace WebApp.Services;

// Renders a site's masthead LogoUrl: a generated PNG combining a "PTA" badge,
// the site name, and the "everychild. onevoice." tagline, in PTA-blue —
// never the theme's PrimaryColor/AccentColor. This is a placeholder
// composition in the spirit of National PTA's branding, not a reproduction
// of their actual trademarked logo artwork (no such asset is available
// here) — swap in the real mark/colors from National PTA's brand guide
// before using this in a public-facing deployment.
//
// Called only from SiteAdminService (GeneratePtaLogoAsync for an explicit
// admin request, EnsureGeneratedLogoAsync for the lazy first-render
// fallback) — never rendered on the fly by the masthead itself.
public class PtaLogoGenerationService(IWebHostEnvironment env)
{
	private const string GeneratedFolder = "generated-logos";
	private const int Width = 640;
	private const int Height = 160;

	// Approximate "PTA Blue" — verify against National PTA's official brand
	// guidelines before production use.
	private static readonly SKColor PtaBlue = new(0x00, 0x33, 0x66);
	private static readonly SKColor PtaBlueDark = new(0x00, 0x1F, 0x3D);

	private readonly IWebHostEnvironment _env = env;

	public async Task<string> GenerateAsync(Guid siteId, string siteName, CancellationToken cancellationToken = default)
	{
		using var bitmap = new SKBitmap(Width, Height);
		using (var canvas = new SKCanvas(bitmap))
		{
			canvas.Clear(SKColors.Transparent);

			using var boldTypeface = SKTypeface.FromFamilyName(null, SKFontStyle.Bold);
			using var regularTypeface = SKTypeface.FromFamilyName(null, SKFontStyle.Normal);

			using var badgePaint = new SKPaint { Color = PtaBlue, IsAntialias = true };
			canvas.DrawOval(new SKRect(4, 4, Height - 4, Height - 4), badgePaint);

			using var badgeFont = new SKFont(boldTypeface, 42);
			using var badgeTextPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
			canvas.DrawText("PTA", Height / 2f, Height / 2f + 15, SKTextAlign.Center, badgeFont, badgeTextPaint);

			using var nameFont = new SKFont(boldTypeface, 34);
			using var namePaint = new SKPaint { Color = PtaBlueDark, IsAntialias = true };
			var nameText = TrimToFit(nameFont, siteName, Width - Height - 24);
			canvas.DrawText(nameText, Height + 24, Height / 2f - 6, SKTextAlign.Left, nameFont, namePaint);

			using var taglineFont = new SKFont(regularTypeface, 20);
			using var taglinePaint = new SKPaint { Color = PtaBlue, IsAntialias = true };
			canvas.DrawText("everychild. onevoice.", Height + 24, Height / 2f + 28, SKTextAlign.Left, taglineFont, taglinePaint);
		}

		var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", GeneratedFolder);
		Directory.CreateDirectory(uploadsRoot);

		var fileName = $"{siteId:N}-{Guid.NewGuid():N}.png";
		var fullPath = Path.Combine(uploadsRoot, fileName);

		using var image = SKImage.FromBitmap(bitmap);
		using var data = image.Encode(SKEncodedImageFormat.Png, 100);
		await using var stream = File.Create(fullPath);
		data.SaveTo(stream);

		return $"/uploads/{GeneratedFolder}/{fileName}";
	}

	// Best-effort cleanup of the file a regenerated LogoUrl replaces. Never
	// touches admin-uploaded fields (PTALogoVariantUrl etc.) — only files
	// this service itself wrote, identified by their folder.
	public void DeleteIfGenerated(string? url)
	{
		if (string.IsNullOrWhiteSpace(url) || !url.StartsWith($"/uploads/{GeneratedFolder}/", StringComparison.Ordinal))
			return;

		var relativePath = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
		var fullPath = Path.Combine(_env.WebRootPath, relativePath);

		if (File.Exists(fullPath))
			File.Delete(fullPath);
	}

	private static string TrimToFit(SKFont font, string text, float maxWidth)
	{
		if (font.MeasureText(text) <= maxWidth)
			return text;

		const string ellipsis = "…";
		var trimmed = text;
		while (trimmed.Length > 1 && font.MeasureText(trimmed + ellipsis) > maxWidth)
			trimmed = trimmed[..^1];

		return trimmed + ellipsis;
	}
}
