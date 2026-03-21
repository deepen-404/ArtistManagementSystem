using ArtistManagementSystem.Data;
using ArtistManagementSystem.Models;
using Npgsql;

namespace ArtistManagementSystem.Services;

public class UserRepository(DbConnection dbConnection)
{
    public async Task<User?> GetByEmailAsync(string email)
    {
        await using var connection = dbConnection.GetConnection();
        await connection.OpenAsync();

        var query = "SELECT id, first_name, last_name, email, password, phone, dob, gender::text, address, role::text, created_at, updated_at FROM users WHERE email = @email";
        await using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("email", email);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapUser(reader);
        }
        return null;
    }

    public async Task<User?> GetByIdAsync(int userId)
    {
        await using var connection = dbConnection.GetConnection();
        await connection.OpenAsync();

        var query = "SELECT id, first_name, last_name, email, password, phone, dob, gender::text, address, role::text, created_at, updated_at FROM users WHERE id = @userId";
        await using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("userId", userId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapUser(reader);
        }
        return null;
    }

    public async Task<(List<User> Users, int TotalCount)> GetAllAsync(int page, int pageSize)
    {
        await using var connection = dbConnection.GetConnection();
        await connection.OpenAsync();

        var countQuery = "SELECT COUNT(*) FROM users";
        await using var countCmd = new NpgsqlCommand(countQuery, connection);
        var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var query = @"SELECT id, first_name, last_name, email, password, phone, dob, gender::text, address, role::text, created_at, updated_at 
                      FROM users 
                      ORDER BY id DESC 
                      LIMIT @pageSize OFFSET @offset";
        await using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("pageSize", pageSize);
        cmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);

        var users = new List<User>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            users.Add(MapUser(reader));
        }

        return (users, totalCount);
    }

    public async Task<User> CreateAsync(RegisterRequest registerRequest, string hashedPassword)
    {
        await using var connection = dbConnection.GetConnection();
        await connection.OpenAsync();

        var genderValue = registerRequest.Gender != null ? $"'{registerRequest.Gender}'::gender_type" : "NULL";
        
        var query = $@"INSERT INTO users (first_name, last_name, email, password, phone, dob, gender, address, role, created_at, updated_at)
                      VALUES (@firstName, @lastName, @email, @password, @phone, @dob, {genderValue}, @address, @role::user_role, @createdAt, @updatedAt)
                      RETURNING id";

        await using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("firstName", registerRequest.FirstName);
        cmd.Parameters.AddWithValue("lastName", registerRequest.LastName);
        cmd.Parameters.AddWithValue("email", registerRequest.Email);
        cmd.Parameters.AddWithValue("password", hashedPassword);
        cmd.Parameters.AddWithValue("phone", (object?)registerRequest.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("dob", (object?)registerRequest.Dob ?? DBNull.Value);
        cmd.Parameters.AddWithValue("address", (object?)registerRequest.Address ?? DBNull.Value);
        cmd.Parameters.AddWithValue("role", registerRequest.Role);
        cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

        var newUserId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        
        return (await GetByIdAsync(newUserId))!;
    }

    public async Task<User> UpdateAsync(int userId, UpdateUserRequest updateRequest)
    {
        await using var connection = dbConnection.GetConnection();
        await connection.OpenAsync();

        var updates = new List<string>();
        if (updateRequest.FirstName != null) updates.Add("first_name = @firstName");
        if (updateRequest.LastName != null) updates.Add("last_name = @lastName");
        if (updateRequest.Phone != null) updates.Add("phone = @phone");
        if (updateRequest.Dob != null) updates.Add("dob = @dob");
        if (updateRequest.Gender != null) updates.Add("gender = @gender::gender_type");
        if (updateRequest.Address != null) updates.Add("address = @address");
        if (updateRequest.Role != null) updates.Add("role = @role::user_role");
        updates.Add("updated_at = @updatedAt");

        var query = $"UPDATE users SET {string.Join(", ", updates)} WHERE id = @userId";

        await using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("userId", userId);
        if (updateRequest.FirstName != null) cmd.Parameters.AddWithValue("firstName", updateRequest.FirstName);
        if (updateRequest.LastName != null) cmd.Parameters.AddWithValue("lastName", updateRequest.LastName);
        if (updateRequest.Phone != null) cmd.Parameters.AddWithValue("phone", updateRequest.Phone);
        if (updateRequest.Dob != null) cmd.Parameters.AddWithValue("dob", updateRequest.Dob);
        if (updateRequest.Gender != null) cmd.Parameters.AddWithValue("gender", updateRequest.Gender);
        if (updateRequest.Address != null) cmd.Parameters.AddWithValue("address", updateRequest.Address);
        if (updateRequest.Role != null) cmd.Parameters.AddWithValue("role", updateRequest.Role);
        cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

        await cmd.ExecuteNonQueryAsync();

        return (await GetByIdAsync(userId))!;
    }

    public async Task<bool> DeleteAsync(int userId)
    {
        await using var connection = dbConnection.GetConnection();
        await connection.OpenAsync();

        var query = "DELETE FROM users WHERE id = @userId";
        await using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("userId", userId);

        var rowsAffected = await cmd.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        await using var connection = dbConnection.GetConnection();
        await connection.OpenAsync();

        var query = "SELECT COUNT(*) FROM users WHERE email = @email";
        await using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("email", email);

        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return count > 0;
    }

    private static User MapUser(NpgsqlDataReader reader)
    {
        var roleOrdinal = reader.GetOrdinal("role");
        UserRole role = UserRole.artist;
        if (!reader.IsDBNull(roleOrdinal))
        {
            var roleString = reader.GetString(roleOrdinal);
            if (!string.IsNullOrEmpty(roleString) && Enum.TryParse<UserRole>(roleString, out var parsedRole))
            {
                role = parsedRole;
            }
        }

        Gender? gender = null;
        var genderOrdinal = reader.GetOrdinal("gender");
        if (!reader.IsDBNull(genderOrdinal))
        {
            var genderString = reader.GetString(genderOrdinal);
            if (!string.IsNullOrEmpty(genderString) && Enum.TryParse<Gender>(genderString, out var parsedGender))
            {
                gender = parsedGender;
            }
        }

        return new User
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            FirstName = reader.GetString(reader.GetOrdinal("first_name")),
            LastName = reader.GetString(reader.GetOrdinal("last_name")),
            Email = reader.GetString(reader.GetOrdinal("email")),
            Password = reader.GetString(reader.GetOrdinal("password")),
            Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString(reader.GetOrdinal("phone")),
            Dob = reader.IsDBNull(reader.GetOrdinal("dob")) ? null : reader.GetDateTime(reader.GetOrdinal("dob")),
            Gender = gender,
            Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
            Role = role,
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
        };
    }
}
