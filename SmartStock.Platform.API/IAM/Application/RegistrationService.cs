using Microsoft.Extensions.Logging;
using SmartStock.Platform.API.Shared.Contracts;
using SmartStock.Platform.API.Shared.Domain;

namespace SmartStock.Platform.API.IAM;

/// <summary>US01, R1: registers a business and its owner. (Registra un negocio y su dueño.)</summary>
public class RegistrationService(
    IUserRepository users,
    ITokenService tokens,
    IUnitOfWork unitOfWork,
    IEmailSender emailSender,
    ILogger<RegistrationService> logger) : IRegistrationService
{
    public async Task<SessionResource> HandleAsync(RegisterCommand command)
    {
        var data = RegistrationRules.Validate(command);

        // R1: the email is unique (el correo es único)
        if (await users.FindByEmailAsync(data.Email) is not null)
            throw DomainException.Conflict("EMAIL_TAKEN");

        var business = new Business(data.BusinessName, data.BusinessType);
        var user = new User(
            business,
            data.Email,
            BCrypt.Net.BCrypt.HashPassword(data.Password));

        // Adding the user also saves its business
        await users.AddAsync(user);
        await unitOfWork.CompleteAsync();

        await SendConfirmationAsync(user.Email, business.Name);

        return new SessionResource(
            tokens.CreateToken(user, business),
            user.Email,
            business.Name,
            business.BusinessType);
    }

    // The account already exists: an email failure must not break registration.
    private async Task SendConfirmationAsync(string email, string businessName)
    {
        try
        {
            await emailSender.SendAsync(
                email,
                RegistrationEmail.Subject,
                RegistrationEmail.Body(businessName));
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "The registration email could not be sent");
        }
    }
}
