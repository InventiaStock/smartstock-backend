namespace SmartStock.Platform.API.IAM;

public class PasswordRecoveryService(
    IUserRepository users,
    IPasswordResetRepository resets,
    IUnitOfWork unitOfWork,
    IEmailSender emailSender,
    IConfiguration configuration,
    ILogger<PasswordRecoveryService> logger) : IPasswordRecoveryService
{
    private static readonly int LifetimeHours = (int)ResetTokens.Lifetime.TotalHours;

    public async Task<ForgotPasswordResource> HandleAsync(ForgotPasswordCommand command)
    {
        var email = EmailRules.Normalize(command.Email);
        if (!EmailRules.IsValid(email))
            throw DomainException.BadRequest("INCOMPLETE");

        var user = await users.FindByEmailAsync(email);

        // Return the same response for registered and unregistered addresses.
        if (user is not null)
        {
            var token = ResetTokens.Create();
            await resets.AddAsync(new PasswordReset(user.Id, ResetTokens.Hash(token), ResetTokens.Lifetime));
            await unitOfWork.CompleteAsync();
            await SendLinkAsync(user.Email, token);
        }

        return new ForgotPasswordResource(true, email, LifetimeHours);
    }

    public async Task HandleAsync(ResetPasswordCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Token) || !PasswordPolicy.IsValid(command.Password))
            throw DomainException.BadRequest("INCOMPLETE");

        var reset = await resets.FindByTokenHashAsync(ResetTokens.Hash(command.Token));
        if (reset is null || reset.UsedAt is not null)
            throw DomainException.BadRequest("INVALID_RESET_TOKEN");

        var now = Clock.UtcNow;
        if (!reset.IsUsable(now))
            throw DomainException.BadRequest("RESET_LINK_EXPIRED");

        var user = await users.FindByIdAsync(reset.UserId)
            ?? throw DomainException.BadRequest("INVALID_RESET_TOKEN");

        user.ChangePassword(BCrypt.Net.BCrypt.HashPassword(command.Password!));
        reset.MarkUsed(now);
        await unitOfWork.CompleteAsync();
    }

    private async Task SendLinkAsync(string email, string token)
    {
        try
        {
            var baseUrl = configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
            var link = PasswordResetEmail.Link(baseUrl, token);
            await emailSender.SendAsync(email, PasswordResetEmail.Subject, PasswordResetEmail.Body(link));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "The password reset email could not be sent");
        }
    }
}