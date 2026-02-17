using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Services.Implementation;
using Xunit;

namespace FlightTracker.Application.Tests.Services;

/// <summary>
/// Unit tests for <see cref="UsernameValidationService"/>.
/// Tests cover allowed characters regex, prohibited terms filtering, and edge cases.
/// </summary>
public class UsernameValidationServiceTests
{
    private readonly UsernameValidationService _service = new();

    #region Null/Empty/Whitespace Tests

    [Fact]
    public async Task ValidateAsync_ReturnsInvalid_WhenUsernameIsNull()
    {
        var result = await _service.ValidateAsync(null!);

        Assert.False(result.IsValid);
        Assert.Equal("Username is required.", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsInvalid_WhenUsernameIsEmpty()
    {
        var result = await _service.ValidateAsync("");

        Assert.False(result.IsValid);
        Assert.Equal("Username is required.", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsInvalid_WhenUsernameIsWhitespace()
    {
        var result = await _service.ValidateAsync("   ");

        Assert.False(result.IsValid);
        Assert.Equal("Username is required.", result.ErrorMessage);
    }

    #endregion

    #region Allowed Characters Tests

    [Theory]
    [InlineData("john_doe")]
    [InlineData("user.name")]
    [InlineData("user-123")]
    [InlineData("Alice123")]
    [InlineData("test_user.name-123")]
    [InlineData("a")]
    [InlineData("Z")]
    [InlineData("0")]
    [InlineData("9")]
    [InlineData("_")]
    [InlineData(".")]
    [InlineData("-")]
    public async Task ValidateAsync_ReturnsValid_WhenUsernameContainsAllowedCharacters(string username)
    {
        var result = await _service.ValidateAsync(username);

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Theory]
    [InlineData("user@domain")]
    [InlineData("user name")]
    [InlineData("user#123")]
    [InlineData("user$name")]
    [InlineData("user%name")]
    [InlineData("user&name")]
    [InlineData("user!")]
    [InlineData("user(name)")]
    [InlineData("user[name]")]
    [InlineData("user{name}")]
    [InlineData("user/name")]
    [InlineData("user\\name")]
    [InlineData("user*name")]
    [InlineData("user+name")]
    [InlineData("user=name")]
    [InlineData("user?name")]
    [InlineData("user<name")]
    [InlineData("user>name")]
    [InlineData("user:name")]
    [InlineData("user;name")]
    [InlineData("user,name")]
    [InlineData("user|name")]
    [InlineData("user`name")]
    [InlineData("user~name")]
    [InlineData("user^name")]
    [InlineData("user'name")]
    [InlineData("user\"name")]
    public async Task ValidateAsync_ReturnsInvalid_WhenUsernameContainsForbiddenCharacters(string username)
    {
        var result = await _service.ValidateAsync(username);

        Assert.False(result.IsValid);
        Assert.Equal("Username can only contain letters, numbers, underscore, hyphen, or dot.", result.ErrorMessage);
    }

    #endregion

    #region Prohibited Terms Tests

    [Theory]
    [InlineData("admin")]
    [InlineData("Admin")]
    [InlineData("ADMIN")]
    [InlineData("AdMiN")]
    [InlineData("moderator")]
    [InlineData("MODERATOR")]
    [InlineData("support")]
    [InlineData("SUPPORT")]
    [InlineData("helpdesk")]
    [InlineData("HELPDESK")]
    [InlineData("contact")]
    [InlineData("CONTACT")]
    [InlineData("root")]
    [InlineData("ROOT")]
    [InlineData("badword")]
    [InlineData("BADWORD")]
    [InlineData("profanity")]
    [InlineData("PROFANITY")]
    [InlineData("offensive")]
    [InlineData("OFFENSIVE")]
    public async Task ValidateAsync_ReturnsInvalid_WhenUsernameContainsProhibitedTerms(string username)
    {
        var result = await _service.ValidateAsync(username);

        Assert.False(result.IsValid);
        Assert.Equal("Username contains prohibited terms.", result.ErrorMessage);
    }

    [Theory]
    [InlineData("admin_user")]
    [InlineData("user_admin")]
    [InlineData("my.admin.name")]
    [InlineData("admin-123")]
    [InlineData("123admin")]
    [InlineData("support-team")]
    [InlineData("moderator_account")]
    [InlineData("rootuser")]
    [InlineData("badwordtest")]
    [InlineData("notaprofanity")]
    [InlineData("unoffensive")]
    public async Task ValidateAsync_ReturnsInvalid_WhenUsernameContainsProhibitedTermsAsSubstring(string username)
    {
        var result = await _service.ValidateAsync(username);

        Assert.False(result.IsValid);
        Assert.Equal("Username contains prohibited terms.", result.ErrorMessage);
    }

    #endregion

    #region CancellationToken Tests

    [Fact]
    public async Task ValidateAsync_RespeactsCancellationToken()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Service uses synchronous operations, so cancellation token won't actually cancel,
        // but we verify the method accepts it without throwing
        var result = await _service.ValidateAsync("validuser", cts.Token);

        Assert.True(result.IsValid);
    }

    #endregion

    #region Edge Cases

    [Theory]
    [InlineData("a.b-c_d")]
    [InlineData("123")]
    [InlineData("a1b2c3")]
    [InlineData("_._.-")]
    [InlineData("test.user-name_123")]
    public async Task ValidateAsync_ReturnsValid_ForValidComplexUsernames(string username)
    {
        var result = await _service.ValidateAsync(username);

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsValid_ForLongButValidUsername()
    {
        var username = new string('a', 100); // 100 character username with only allowed chars
        var result = await _service.ValidateAsync(username);

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAsync_CaseSensitiveForAllowedCharacters()
    {
        // Regex allows both uppercase and lowercase letters
        var result1 = await _service.ValidateAsync("User");
        var result2 = await _service.ValidateAsync("USER");
        var result3 = await _service.ValidateAsync("user");

        Assert.True(result1.IsValid);
        Assert.True(result2.IsValid);
        Assert.True(result3.IsValid);
    }

    [Theory]
    [InlineData("user_123")]
    [InlineData("test-name")]
    [InlineData("first.last")]
    [InlineData("john_doe-123.test")]
    public async Task ValidateAsync_ReturnsValid_ForUsernameMixingAllowedCharacters(string username)
    {
        var result = await _service.ValidateAsync(username);

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    #endregion

    #region ValidationResult Tests

    [Fact]
    public async Task ValidateAsync_ReturnsNullErrorMessage_WhenValid()
    {
        var result = await _service.ValidateAsync("validuser123");

        Assert.Null(result.ErrorMessage);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsNonNullErrorMessage_WhenInvalid()
    {
        var result = await _service.ValidateAsync("user@domain");

        Assert.NotNull(result.ErrorMessage);
        Assert.False(result.IsValid);
    }

    #endregion
}
