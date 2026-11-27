using SmartStock.Platform.API.IAM;
using Xunit;

namespace SmartStock.Platform.API.Tests;

// Unit tests of the authentication rules. They do not need a database.
// (Pruebas unitarias de las reglas de autenticación. No necesitan base de datos.)

public class PasswordResetRulesTests
{
    private static PasswordReset NewReset() => new(1, ResetTokens.Hash("sample-token"), ResetTokens.Lifetime);

    [Fact]
    public void R4_reset_link_expires_24_hours_after_it_is_issued()
    {
        var reset = NewReset();

        Assert.Equal(reset.IssuedAt.AddHours(24), reset.ExpiresAt);
    }

    [Fact]
    public void R4_reset_link_is_usable_before_24_hours()
    {
        var reset = NewReset();

        Assert.True(reset.IsUsable(reset.IssuedAt.AddHours(23)));
    }

    [Theory]
    [InlineData(24)]
    [InlineData(25)]
    public void R4_reset_link_is_not_usable_from_24_hours_on(int hours)
    {
        var reset = NewReset();

        Assert.False(reset.IsUsable(reset.IssuedAt.AddHours(hours)));
    }

    [Fact]
    public void R4_reset_link_can_be_used_only_once()
    {
        var reset = NewReset();

        reset.MarkUsed(reset.IssuedAt.AddMinutes(5));

        Assert.False(reset.IsUsable(reset.IssuedAt.AddMinutes(10)));
    }
}

public class CredentialRulesTests
{
    [Theory]
    [InlineData("", false)]
    [InlineData("short12", false)]
    [InlineData("        ", false)]
    [InlineData("LongEnough1", true)]
    public void Password_needs_at_least_8_characters(string password, bool expected) =>
        Assert.Equal(expected, PasswordPolicy.IsValid(password));

    [Theory]
    [InlineData("owner@example.com", true)]
    [InlineData("owner", false)]
    [InlineData("owner@example", false)]
    [InlineData("a@b@example.com", false)]
    public void Email_must_have_a_valid_format(string email, bool expected) =>
        Assert.Equal(expected, EmailRules.IsValid(email));

    [Fact]
    public void Reset_token_is_stored_as_a_64_character_hash()
    {
        var token = ResetTokens.Create();

        var hash = ResetTokens.Hash(token);

        Assert.Equal(64, hash.Length);
        Assert.NotEqual(token, hash);
        Assert.Equal(hash, ResetTokens.Hash(token));
    }
}
