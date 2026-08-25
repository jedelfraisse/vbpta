using Microsoft.AspNetCore.Components.Forms;
using SkiaSharp;

namespace WebApp.Services;

// Backs every logo/branding upload field (PTA logo, district logo, school
// crest, partner logo). Files are saved under wwwroot/uploads/{subfolder}
// with a generated name — the caller never trusts the browser-supplied
// filename beyond its extension — and served back via UseStaticFiles.
public class FileUploadService(IWebHostEnvironment env)
{
	private const long MaxFileSizeBytes = 2 * 1024 * 1024;

	private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
	{
		".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp",
	};

	private readonly IWebHostEnvironment _env = env;

	public async Task<string> SaveImageAsync(IBrowserFile file, string subfolder, CancellationToken cancellationToken = default)
	{
		if (file.Size > MaxFileSizeBytes)
			throw new InvalidOperationException("Image must be 2 MB or smaller.");

		var extension = Path.GetExtension(file.Name);
		if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
			throw new InvalidOperationException("Unsupported image type. Use PNG, JPG, GIF, SVG, or WEBP.");

		var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", subfolder);
		Directory.CreateDirectory(uploadsRoot);

		var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
		var fullPath = Path.Combine(uploadsRoot, fileName);

		await using var sourceStream = file.OpenReadStream(MaxFileSizeBytes, cancellationToken);
		await using var destinationStream = File.Create(fullPath);
		await sourceStream.CopyToAsync(destinationStream, cancellationToken);

		return $"/uploads/{subfolder}/{fileName}";
	}

	// Backs auto-filling the masthead logo Width/Height fields with an
	// uploaded image's own pixel size (see SiteDetail.razor's Branding edit
	// card) — a sensible starting point beats leaving them blank/defaulted.
	// Uses SKCodec (header-only) rather than decoding full pixel data — these
	// files are small, but no reason to pay for a full decode just to read
	// two numbers. Returns null for anything that isn't a raster format
	// SkiaSharp can read (SVG, most notably — allowed as an upload but has
	// no fixed pixel size to report), a missing file, or any decode failure;
	// callers should treat null as "couldn't determine it," not an error.
	public (int Width, int Height)? TryGetImageDimensions(string? relativeUrl)
	{
		if (string.IsNullOrWhiteSpace(relativeUrl))
			return null;

		var relativePath = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
		var fullPath = Path.Combine(_env.WebRootPath, relativePath);
		if (!File.Exists(fullPath))
			return null;

		try
		{
			using var stream = File.OpenRead(fullPath);
			using var codec = SKCodec.Create(stream);
			return codec is null ? null : (codec.Info.Width, codec.Info.Height);
		}
		catch
		{
			return null;
		}
	}
}
