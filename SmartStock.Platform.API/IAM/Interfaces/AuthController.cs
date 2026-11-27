namespace SmartStock.Platform.API.IAM;

/// <summary>TS04, US01 to US03 - authentication.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/auth")]
[Tags("IAM")]
public class AuthController(
    IRegistrationService registration,
    ILoginService login,
    IPasswordRecoveryService recovery) : ControllerBase
{
    [HttpPost("register")]
    [EndpointSummary("Registers a business and its owner (US01)")]
    [EndpointDescription("400 INCOMPLETE if data is missing or invalid; 409 EMAIL_TAKEN if the email exists (R1).")]
    [ProducesResponseType(typeof(SessionResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command) =>
        StatusCode(StatusCodes.Status201Created, await registration.HandleAsync(command));

    [HttpPost("login")]
    [EndpointSummary("Signs in and returns a token (US02, TS04)")]
    [EndpointDescription("401 INVALID_CREDENTIALS. It never says which field failed (R3).")]
    [ProducesResponseType(typeof(SessionResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command) =>
        Ok(await login.HandleAsync(command));

    [HttpPost("forgot-password")]
    [EndpointSummary("Sends a password reset link (US03)")]
    [EndpointDescription("The link expires in 24 hours (R4). Same answer if the email is unknown.")]
    [ProducesResponseType(typeof(ForgotPasswordResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command) =>
        Ok(await recovery.HandleAsync(command));

    [HttpPost("reset-password")]
    [EndpointSummary("Saves a new password using the reset token (US03)")]
    [EndpointDescription("400 INVALID_RESET_TOKEN if the link is not valid or was used; 400 RESET_LINK_EXPIRED after 24 hours.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        await recovery.HandleAsync(command);
        return NoContent();
    }
}