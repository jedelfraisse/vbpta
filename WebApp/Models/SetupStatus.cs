namespace WebApp.Models;

public class SetupStatus
{
	// ------------------------------------------------------------
	// STEP 1: SQL
	// ------------------------------------------------------------
	public bool HasConnectionString { get; set; }
	public bool CanConnectToDatabase { get; set; }

	// Summary fields (non-sensitive)
	public string SqlServer { get; set; } = "";
	public string DatabaseName { get; set; } = "";
	public string SqlUsername { get; set; } = "";

	// ------------------------------------------------------------
	// STEP 2: SMTP
	// ------------------------------------------------------------
	public bool HasEmailSettings { get; set; }
	public bool IsEmailWorking { get; set; }

	// Summary fields (non-sensitive)
	public string SmtpHost { get; set; } = "";
	public int SmtpPort { get; set; }
	public string SmtpUsername { get; set; } = "";
	public string SmtpFrom { get; set; } = "";
	public bool SmtpUseSsl { get; set; }

	// ------------------------------------------------------------
	// STEP 3: Admin
	// ------------------------------------------------------------
	public bool HasAdminUser { get; set; }

	// Summary fields
	public string AdminEmail { get; set; } = "";
	public string AdminName { get; set; } = "";

	// ------------------------------------------------------------
	// STEP 4: Portal Info
	// ------------------------------------------------------------
	public bool HasPortalInfo { get; set; }

	// Summary fields
	public string PortalName { get; set; } = "";
	public string PortalDomain { get; set; } = "";

	// ------------------------------------------------------------
	// Derived flags
	// ------------------------------------------------------------
	public bool NeedsSql { get; set; }
	public bool NeedsSmtp { get; set; }
	public bool NeedsAdmin { get; set; }
	public bool NeedsPortalInfo { get; set; }

	public bool IsFullyConfigured { get; set; }

	public override string ToString()
	{
		return $"HasConnectionString={HasConnectionString}, " +
			   $"CanConnectToDatabase={CanConnectToDatabase}, " +
			   $"HasEmailSettings={HasEmailSettings}, " +
			   $"IsEmailWorking={IsEmailWorking}, " +
			   $"HasAdminUser={HasAdminUser}, " +
			   $"HasPortalInfo={HasPortalInfo}, " +
			   $"IsFullyConfigured={IsFullyConfigured}";
	}

}
