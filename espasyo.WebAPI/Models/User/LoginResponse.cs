namespace espasyo.WebAPI.Models.User;

public record LoginResponse
{
    public string? Username { get; init; }
    public string? Token { get; init; }
    public string? RefreshToken { get; init; }
}
