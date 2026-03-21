using ArtistManagementSystem.Data;
using ArtistManagementSystem.Models;
using Npgsql;

namespace ArtistManagementSystem.Services;

public class ArtistRepository(DbConnection dbConnection)
{
    public async Task<Artist?> GetByIdAsync(int artistId)
    {
        await using var connection = dbConnection.GetConnection();
        await connection.OpenAsync();

        var query = "SELECT * FROM artists WHERE id = @artistId";
        await using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("artistId", artistId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapArtist(reader);
        }
        return null;
    }

    public async Task<(List<Artist> Artists, int TotalCount)> GetAllAsync(int page, int pageSize)
    {
        await using var connection = dbConnection.GetConnection();
        await connection.OpenAsync();

        var countQuery = "SELECT COUNT(*) FROM artists";
        await using var countCmd = new NpgsqlCommand(countQuery, connection);
        var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var query = @"SELECT id, name, dob, gender, address, first_release_year, no_of_albums_released, created_at, updated_at 
                      FROM artists 
                      ORDER BY id DESC 
                      LIMIT @pageSize OFFSET @offset";
        await using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("pageSize", pageSize);
        cmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);

        var artists = new List<Artist>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            artists.Add(MapArtist(reader));
        }

        return (artists, totalCount);
    }

    public async Task<Artist> CreateAsync(CreateArtistRequest createRequest)
    {
        await using var connection = dbConnection.GetConnection();
        await connection.OpenAsync();

        var genderValue = createRequest.Gender != null ? $"'{createRequest.Gender}'::gender_type" : "NULL";

        var query = $@"INSERT INTO artists (name, dob, gender, address, first_release_year, no_of_albums_released, created_at, updated_at)
                      VALUES (@name, @dob, {genderValue}, @address, @firstReleaseYear, @noOfAlbumsReleased, @createdAt, @updatedAt)
                      RETURNING id";

        await using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("name", createRequest.Name);
        cmd.Parameters.AddWithValue("dob", (object?)createRequest.Dob ?? DBNull.Value);
        cmd.Parameters.AddWithValue("address", (object?)createRequest.Address ?? DBNull.Value);
        cmd.Parameters.AddWithValue("firstReleaseYear", createRequest.FirstReleaseYear);
        cmd.Parameters.AddWithValue("noOfAlbumsReleased", createRequest.NoOfAlbumsReleased);
        cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

        var newArtistId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        
        return (await GetByIdAsync(newArtistId))!;
    }

    public async Task<Artist> UpdateAsync(int artistId, UpdateArtistRequest updateRequest)
    {
        await using var connection = dbConnection.GetConnection();
        await connection.OpenAsync();

        var updates = new List<string>();
        if (updateRequest.Name != null) updates.Add("name = @name");
        if (updateRequest.Dob != null) updates.Add("dob = @dob");
        if (updateRequest.Gender != null) updates.Add("gender = @gender::gender_type");
        if (updateRequest.Address != null) updates.Add("address = @address");
        if (updateRequest.FirstReleaseYear != null) updates.Add("first_release_year = @firstReleaseYear");
        if (updateRequest.NoOfAlbumsReleased != null) updates.Add("no_of_albums_released = @noOfAlbumsReleased");
        updates.Add("updated_at = @updatedAt");

        var query = $"UPDATE artists SET {string.Join(", ", updates)} WHERE id = @artistId";

        await using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("artistId", artistId);
        if (updateRequest.Name != null) cmd.Parameters.AddWithValue("name", updateRequest.Name);
        if (updateRequest.Dob != null) cmd.Parameters.AddWithValue("dob", updateRequest.Dob);
        if (updateRequest.Gender != null) cmd.Parameters.AddWithValue("gender", updateRequest.Gender);
        if (updateRequest.Address != null) cmd.Parameters.AddWithValue("address", updateRequest.Address);
        if (updateRequest.FirstReleaseYear != null) cmd.Parameters.AddWithValue("firstReleaseYear", updateRequest.FirstReleaseYear);
        if (updateRequest.NoOfAlbumsReleased != null) cmd.Parameters.AddWithValue("noOfAlbumsReleased", updateRequest.NoOfAlbumsReleased);
        cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

        await cmd.ExecuteNonQueryAsync();

        return (await GetByIdAsync(artistId))!;
    }

    public async Task<bool> DeleteAsync(int artistId)
    {
        await using var connection = dbConnection.GetConnection();
        await connection.OpenAsync();

        var query = "DELETE FROM artists WHERE id = @artistId";
        await using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("artistId", artistId);

        var rowsAffected = await cmd.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<List<Artist>> CreateManyAsync(List<CreateArtistRequest> artistRequests)
    {
        var createdArtists = new List<Artist>();
        foreach (var request in artistRequests)
        {
            var created = await CreateAsync(request);
            createdArtists.Add(created);
        }
        return createdArtists;
    }

    private static Artist MapArtist(NpgsqlDataReader reader)
    {
        Gender? gender = null;
        var genderOrdinal = reader.GetOrdinal("gender");
        if (!reader.IsDBNull(genderOrdinal))
        {
            var genderValue = reader.GetValue(genderOrdinal);
            var genderString = genderValue?.ToString();
            if (!string.IsNullOrEmpty(genderString) && Enum.TryParse<Gender>(genderString, out var parsedGender))
            {
                gender = parsedGender;
            }
        }

        return new Artist
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Dob = reader.IsDBNull(reader.GetOrdinal("dob")) ? null : reader.GetDateTime(reader.GetOrdinal("dob")),
            Gender = gender,
            Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
            FirstReleaseYear = reader.GetInt32(reader.GetOrdinal("first_release_year")),
            NoOfAlbumsReleased = reader.GetInt32(reader.GetOrdinal("no_of_albums_released")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
        };
    }
}
