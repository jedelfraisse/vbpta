using Microsoft.AspNetCore.Components.Forms;

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
}
