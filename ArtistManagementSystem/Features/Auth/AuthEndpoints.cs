using ArtistManagementSystem.Data;
using ArtistManagementSystem.Middleware;
using ArtistManagementSystem.Models;
using ArtistManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;

namespace ArtistManagementSystem.Features.Auth;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/register", Register);
        group.MapPost("/login", Login);
        group.MapPost("/logout", Logout);
        group.MapGet("/me", GetCurrentUser);
        return group;
    }

    [AllowAnonymous]
    private static async Task<IResult> GetCurrentUser(HttpContext httpContext, UserRepository userRepository)
    {
        var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Results.Unauthorized();

        var user = await userRepository.GetByIdAsync(userId);
        if (user == null)
            return Results.Unauthorized();

        return Results.Ok(new
        {
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            Role = user.Role.ToString()
        });
    }

    [AllowAnonymous]
    private static async Task<IResult> Register(RegisterRequest registerRequest, UserRepository userRepository)
    {
        if (string.IsNullOrEmpty(registerRequest.Email) || string.IsNullOrEmpty(registerRequest.Password) ||
            string.IsNullOrEmpty(registerRequest.FirstName) || string.IsNullOrEmpty(registerRequest.LastName))
            return Results.BadRequest(new { error = "Email, password, first name, and last name are required" });

        if (await userRepository.EmailExistsAsync(registerRequest.Email))
            return Results.BadRequest(new { error = "Email already exists" });

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerRequest.Password);
        
        var createdUser = await userRepository.CreateAsync(registerRequest, hashedPassword);
        return Results.Ok(new { message = "Registration successful. Please login.", userId = createdUser.Id });
    }

    [AllowAnonymous]
    private static async Task<IResult> Login(LoginRequest loginRequest, HttpContext httpContext, UserRepository userRepository, TokenService tokenService)
    {
        var existingUser = await userRepository.GetByEmailAsync(loginRequest.Email);
        if (existingUser == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, existingUser.Password))
            return Results.Unauthorized();

        var token = tokenService.GenerateToken(existingUser);
        
        httpContext.Response.Cookies.Append("token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });

        return Results.Ok(new 
        { 
            Id = existingUser.Id,
            Email = existingUser.Email,
            FirstName = existingUser.FirstName,
            LastName = existingUser.LastName,
             Role = existingUser.Role.ToString(),
            Message = "Login successful"
        });
    }

    [AllowAnonymous]
    private static Task<IResult> Logout(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete("token");
        return Task.FromResult(Results.Ok(new { message = "Logged out successfully" }));
    }
}
