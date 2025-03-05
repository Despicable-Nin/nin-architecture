using System.ComponentModel.DataAnnotations;

namespace espasyo.WebAPI.Models.User;

public record LoginRequest
{

    [Required]
    public required string? UserName { get; init; }
    
    [Required]
    public required string? Password { get; init; }
}