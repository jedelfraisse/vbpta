using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiteEngine.Identity;

public class CustomRole
{
	public Guid Id { get; set; }
	public Guid SiteId { get; set; }
	public string SchoolYear { get; set; } = string.Empty; // "2026-2027"
	public string Name { get; set; } = string.Empty;
}
