using SiteEngine.Entities;
using SkiaSharp;

namespace WebApp.Services;

// Renders a site's masthead LogoUrl. Two ways to do that:
//
//  - GenerateFromTemplateAsync stamps the site name onto the one global
//    template image an admin uploads and calibrates in Global Settings
//    (PortalConfig.LogoTemplateUrl + the LogoTemplateBox*/Font* fields) —
//    this is the real logo design (built in Canva/PPT/Word), so this is
//    what runs whenever a template has been configured.
//  - GenerateAsync is the original code-drawn fallback (a "PTA" badge +
//    site name + tagline, in PTA-blue) used only when no template has been
//    configured yet. Placeholder composition in the spirit of National
//    PTA's branding, not a reproduction of their actual trademarked logo
//    artwork — swap in the real mark/colors before any public-facing use.
//
// Called only from SiteAdminService (GeneratePtaLogoAsync for an explicit
// admin request, EnsureGeneratedLogoAsync for the lazy first-render
// fallback) and from GlobalSettings.razor (GenerateTemplatePreviewAsync,
// for calibrating the template box) — never rendered on the fly by the
// masthead itself.
public class PtaLogoGenerationService(IWebHostEnvironment env)
{
	private const string GeneratedFolder = "generated-logos";
	private const string PreviewFolder = "template-preview";
	private const string PreviewFileName = "preview.png";
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

	// Stamps siteName onto the configured global template image and saves the
	// result under the same generated-logos folder as GenerateAsync, so
	// DeleteIfGenerated cleans it up the same way. Returns null if no
	// template is configured or the configured file no longer exists on
	// disk — callers should fall back to GenerateAsync in that case.
	public async Task<string?> GenerateFromTemplateAsync(
		Guid siteId, string siteName, PortalConfig config, CancellationToken cancellationToken = default)
	{
		using var data = RenderOntoTemplate(siteName, config);
		if (data is null)
			return null;

		var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", GeneratedFolder);
		Directory.CreateDirectory(uploadsRoot);

		var fileName = $"{siteId:N}-{Guid.NewGuid():N}.png";
		var fullPath = Path.Combine(uploadsRoot, fileName);

		await using var stream = File.Create(fullPath);
		data.SaveTo(stream);

		return $"/uploads/{GeneratedFolder}/{fileName}";
	}

	// Renders a sample onto the template with whatever box/font settings the
	// admin is currently editing (not yet saved), so Global Settings can show
	// a live preview before committing. Always overwrites the same file —
	// nothing here is tied to a site, so nothing to accumulate or clean up.
	// Callers should cache-bust the returned URL (it doesn't change).
	public async Task<string?> GenerateTemplatePreviewAsync(
		string sampleSiteName, PortalConfig config, CancellationToken cancellationToken = default)
	{
		using var data = RenderOntoTemplate(sampleSiteName, config);
		if (data is null)
			return null;

		var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", PreviewFolder);
		Directory.CreateDirectory(uploadsRoot);
		var fullPath = Path.Combine(uploadsRoot, PreviewFileName);

		await using var stream = File.Create(fullPath);
		data.SaveTo(stream);

		return $"/uploads/{PreviewFolder}/{PreviewFileName}";
	}

	// Shared by GenerateFromTemplateAsync and GenerateTemplatePreviewAsync.
	// Box coordinates are percentages of the template image's own
	// width/height, so they stay correct regardless of what size the
	// template was exported at. Font size auto-shrinks to fit the box —
	// a logo needs the whole name, not an ellipsis.
	private SKData? RenderOntoTemplate(string text, PortalConfig config)
	{
		if (string.IsNullOrWhiteSpace(config.LogoTemplateUrl))
			return null;

		var relativePath = config.LogoTemplateUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
		var templatePath = Path.Combine(_env.WebRootPath, relativePath);
		if (!File.Exists(templatePath))
			return null;

		using var templateBitmap = SKBitmap.Decode(templatePath);
		if (templateBitmap is null)
			return null;

		var boxX = (float)(config.LogoTemplateBoxXPct / 100.0 * templateBitmap.Width);
		var boxY = (float)(config.LogoTemplateBoxYPct / 100.0 * templateBitmap.Height);
		var boxWidth = (float)(config.LogoTemplateBoxWidthPct / 100.0 * templateBitmap.Width);
		var boxHeight = (float)(config.LogoTemplateBoxHeightPct / 100.0 * templateBitmap.Height);

		using var surface = SKSurface.Create(new SKImageInfo(templateBitmap.Width, templateBitmap.Height));
		var canvas = surface.Canvas;
		canvas.Clear(SKColors.Transparent);
		canvas.DrawBitmap(templateBitmap, 0, 0, SKSamplingOptions.Default, null);

		var color = SKColor.TryParse(config.LogoTemplateFontColor, out var parsedColor) ? parsedColor : SKColors.Black;

		// A family like "Arial Black" already IS the heavy weight — it has no
		// separate Bold variant to request, so asking for SKFontStyle.Bold on
		// top of it risks the font matcher silently falling back to a
		// different family. Only synthesize bold for families that need it.
		var requestedStyle = config.LogoTemplateFontFamily.Contains("Black", StringComparison.OrdinalIgnoreCase)
			? SKFontStyle.Normal
			: SKFontStyle.Bold;
		using var typeface = SKTypeface.FromFamilyName(config.LogoTemplateFontFamily, requestedStyle)
			?? SKTypeface.FromFamilyName(null, SKFontStyle.Bold);

		using var font = new SKFont(typeface, Math.Max(8f, boxHeight * 0.8f));
		while (font.Size > 6f && font.MeasureText(text) > boxWidth && boxWidth > 0)
			font.Size -= 1f;

		using var paint = new SKPaint { Color = color, IsAntialias = true };

		var align = config.LogoTemplateTextAlign?.Trim().ToLowerInvariant() switch
		{
			"left" => SKTextAlign.Left,
			"right" => SKTextAlign.Right,
			_ => SKTextAlign.Center,
		};

		var textX = align switch
		{
			SKTextAlign.Left => boxX,
			SKTextAlign.Right => boxX + boxWidth,
			_ => boxX + boxWidth / 2f,
		};

		font.GetFontMetrics(out var metrics);
		var textY = boxY + boxHeight / 2f - (metrics.Ascent + metrics.Descent) / 2f;

		canvas.DrawText(text, textX, textY, align, font, paint);

		using var image = surface.Snapshot();
		return image.Encode(SKEncodedImageFormat.Png, 100);
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
