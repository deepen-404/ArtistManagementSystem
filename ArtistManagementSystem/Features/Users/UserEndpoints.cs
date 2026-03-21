using ArtistManagementSystem.Data;
using ArtistManagementSystem.Middleware;
using ArtistManagementSystem.Models;
using ArtistManagementSystem.Services;

namespace ArtistManagementSystem.Features.Users;

public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAllUsers)
              .RequireRole("super_admin");
        
        group.MapPost("/", CreateUser)
             .RequireRole("super_admin");
        
        group.MapGet("/{userId}", GetUserById)
             .RequireRole("super_admin");
        
        group.MapPut("/{userId}", UpdateUser)
             .RequireRole("super_admin");
        
        group.MapDelete("/{userId}", DeleteUser)
             .RequireRole("super_admin");
        
        return group;
    }

    private static async Task<IResult> GetAllUsers(HttpContext httpContext, UserRepository userRepository)
    {
        var page = int.Parse(httpContext.Request.Query["page"].FirstOrDefault() ?? "1");
        var pageSize = int.Parse(httpContext.Request.Query["pageSize"].FirstOrDefault() ?? "10");
        var (users, totalCount) = await userRepository.GetAllAsync(page, pageSize);
        return Results.Ok(new { 
            data = users.Select(u => new { 
                u.Id, 
                u.FirstName, 
                u.LastName, 
                u.Email, 
                u.Phone, 
                u.Dob, 
                u.Gender, 
                u.Address, 
                u.Role, 
                u.CreatedAt, 
                u.UpdatedAt 
            }), 
            totalCount, 
            page, 
            pageSize 
        });
    }

    private static async Task<IResult> CreateUser(RegisterRequest registerRequest, UserRepository userRepository)
    {
        if (string.IsNullOrEmpty(registerRequest.Email) || string.IsNullOrEmpty(registerRequest.Password) ||
            string.IsNullOrEmpty(registerRequest.FirstName) || string.IsNullOrEmpty(registerRequest.LastName))
            return Results.BadRequest(new { error = "Email, password, first name, and last name are required" });

        if (await userRepository.EmailExistsAsync(registerRequest.Email))
            return Results.BadRequest(new { error = "Email already exists" });

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerRequest.Password);
        
        var createdUser = await userRepository.CreateAsync(registerRequest, hashedPassword);
        return Results.Created($"/api/users/{createdUser.Id}", new { 
            createdUser.Id, 
            createdUser.FirstName, 
            createdUser.LastName, 
            createdUser.Email, 
            createdUser.Phone, 
            createdUser.Dob, 
            createdUser.Gender, 
            createdUser.Address, 
            createdUser.Role 
        });
    }

    private static async Task<IResult> GetUserById(int userId, UserRepository userRepository)
    {
        var user = await userRepository.GetByIdAsync(userId);
        if (user == null) return Results.NotFound(new { error = "User not found" });
        return Results.Ok(new { 
            user.Id, 
            user.FirstName, 
            user.LastName, 
            user.Email, 
            user.Phone, 
            user.Dob, 
            user.Gender, 
            user.Address, 
            user.Role 
        });
    }

    private static async Task<IResult> UpdateUser(int userId, UpdateUserRequest updateRequest, UserRepository userRepository)
    {
        var existingUser = await userRepository.GetByIdAsync(userId);
        if (existingUser == null) return Results.NotFound(new { error = "User not found" });

        var updatedUser = await userRepository.UpdateAsync(userId, updateRequest);
        return Results.Ok(new { 
            updatedUser.Id, 
            updatedUser.FirstName, 
            updatedUser.LastName, 
            updatedUser.Email, 
            updatedUser.Phone, 
            updatedUser.Dob, 
            updatedUser.Gender, 
            updatedUser.Address, 
            updatedUser.Role 
        });
    }

    private static async Task<IResult> DeleteUser(int userId, UserRepository userRepository)
    {
        var isDeleted = await userRepository.DeleteAsync(userId);
        if (!isDeleted) return Results.NotFound(new { error = "User not found" });
        return Results.Ok(new { message = "User deleted successfully" });
    }
}
