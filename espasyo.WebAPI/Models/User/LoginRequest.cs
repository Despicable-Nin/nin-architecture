using System.ComponentModel.DataAnnotations;

namespace espasyo.WebAPI.Models.User;

public record LoginRequest
{

    [Required]
    public required string? Email { get; init; }
    
    [Required]
    public required string? Password { get; init; }
}