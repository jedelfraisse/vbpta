using Microsoft.AspNetCore.Components;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Components.PortalPages;

public partial class SetupSetup : ComponentBase
{
	[Inject] private SetupStateService SetupState { get; set; } = default!;
	[Inject] private SetupService SetupService { get; set; } = default!;
	[Inject] private NavigationManager Nav { get; set; } = default!;
	[Inject] private HttpClient Http { get; set; } = default!;

	protected override void OnInitialized()
	{
		_status = SetupState.GetStatus();

		if (_status.IsFullyConfigured)
			_finalReady = true;
	}

	// ------------------------------------------------------------
	// STEP 1 — SQL
	// ------------------------------------------------------------
	private async Task TestSqlAndContinueAsync()
	{
		ResetMessage();
		_isWorking = true;

		try
		{
			await SetupService.TestSqlConnectionAsync(_model);
			await SetupService.RunMigrationsAsync(_model);
			await SetupService.SaveStepAsync(_model);

			RefreshStatus();
		}
		catch (Exception ex)
		{
			Fail(ex);
		}

		_isWorking = false;
	}

	// ------------------------------------------------------------
	// STEP 2 — SMTP
	// ------------------------------------------------------------
	private async Task TestSmtpAndContinueAsync()
	{
		ResetMessage();
		_isWorking = true;

		try
		{
			await SetupService.TestSmtpAsync(_model);
			await SetupService.SaveStepAsync(_model);

			RefreshStatus();
		}
		catch (Exception ex)
		{
			Fail(ex);
		}

		_isWorking = false;
	}

	// ------------------------------------------------------------
	// STEP 3 — ADMIN
	// ------------------------------------------------------------
	private async Task SendAdminCodeAsync() => await SendOrResendAdminCodeAsync(isResend: false);

	// Lets the admin request a fresh code if the first one never arrived,
	// went to a mistyped address, or simply expired. Reads _model.AdminEmail
	// at call time, so editing the email field before clicking this picks up
	// the correction automatically.
	private async Task ResendAdminCodeAsync() => await SendOrResendAdminCodeAsync(isResend: true);

	private async Task SendOrResendAdminCodeAsync(bool isResend)
	{
		ResetMessage();
		_isWorking = true;

		try
		{
			// If the email changed since the last send, retire the old code so
			// a stale one can't still be redeemed for the mistyped address.
			if (isResend &&
				!string.IsNullOrWhiteSpace(_lastCodeSentToEmail) &&
				!string.Equals(_lastCodeSentToEmail, _model.AdminEmail, StringComparison.OrdinalIgnoreCase))
			{
				SetupService.InvalidateAdminCode(_lastCodeSentToEmail);
			}

			await SetupService.SendAdminVerificationCodeAsync(_model.AdminEmail);

			_adminCodeSent = true;
			_lastCodeSentToEmail = _model.AdminEmail;
			_adminCodeInput = "";

			Success(isResend
				? $"New verification code sent to {_model.AdminEmail}."
				: "Verification code sent.");
		}
		catch (Exception ex)
		{
			Fail(ex);
		}

		_isWorking = false;
	}

	private async Task VerifyAdminCodeAsync()
	{
		ResetMessage();
		_isWorking = true;

		try
		{
			if (!SetupService.ValidateAdminCode(_model.AdminEmail, _adminCodeInput))
				throw new Exception("Invalid verification code.");

			await SetupService.CreateAdminUserAsync(_model.AdminEmail, _model.AdminName);
			await SetupService.SaveStepAsync(_model);

			RefreshStatus();
		}
		catch (Exception ex)
		{
			Fail(ex);
		}

		_isWorking = false;
	}

	// ------------------------------------------------------------
	// STEP 4 — PORTAL INFO
	// ------------------------------------------------------------
	private async Task SavePortalInfoAsync()
	{
		ResetMessage();
		_isWorking = true;

		try
		{
			await SetupService.SaveStepAsync(_model);

			// RefreshStatus() itself triggers the restart flow once this
			// makes the wizard fully configured — see its comment.
			RefreshStatus();
		}
		catch (Exception ex)
		{
			Fail(ex);
		}

		_isWorking = false;
	}

	// ------------------------------------------------------------
	// RESTART
	// ------------------------------------------------------------
	private async Task RestartNow()
	{
		_isRestarting = true;

		SetupService.TriggerRestart();

		for (int i = 0; i < 90; i++)
		{
			try
			{
				var result = await Http.GetAsync("health");
				if (result.IsSuccessStatusCode)
				{
					Nav.NavigateTo("/", forceLoad: true);
					return;
				}
			}
			catch { }

			await Task.Delay(1000);
		}
	}

	// ------------------------------------------------------------
	// HELPERS
	// ------------------------------------------------------------
	private void RefreshStatus()
	{
		SetupState.Refresh();
		_status = SetupState.GetStatus();

		// Normally only Step 4 (Portal Info) can make IsFullyConfigured true —
		// a fresh database has no SMTP/admin/portal info until each step fills
		// it in. But pointing Step 1 at an *existing*, already-configured
		// database (SMTP, admin user, and portal info all already present)
		// can jump straight to "fully configured" after just one step. Without
		// this check here, none of the four step conditions in SetupSetup.razor
		// match anymore and nothing renders — a blank screen instead of the
		// restart flow. Centralizing the check here (rather than duplicating
		// it in every step handler) means whichever step happens to complete
		// the wizard triggers the same restart.
		if (_status.IsFullyConfigured && !_isFinishing && !_finalReady)
		{
			_isFinishing = true;
			_finishMessage = "Thank you! Your PTA Portal setup is complete. The system will now restart.";
			_ = RestartNow();
		}
	}

	private void ResetMessage()
	{
		_message = "";
		_messageClass = "";
	}

	private void Fail(string msg)
	{
		_message = msg;
		_messageClass = "error-text";
	}

	// EF Core's DbUpdateException.Message is always the same generic wrapper
	// text ("An error occurred while saving the entity changes...") — the
	// actual cause (constraint violation, etc.) is in InnerException. Unwrap
	// to the innermost message so the wizard shows something actionable.
	private void Fail(Exception ex)
	{
		var innermost = ex;
		while (innermost.InnerException != null)
			innermost = innermost.InnerException;

		Fail(innermost.Message);
	}

	private void Success(string msg)
	{
		_message = msg;
		_messageClass = "success-text";
	}
}
