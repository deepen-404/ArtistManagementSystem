using ArtistManagementSystem.Data;
using ArtistManagementSystem.Models;
using Npgsql;

namespace ArtistManagementSystem.Services;

public class MusicRepository(DbConnection dbConnection)
{
    public async Task<Music?> GetByIdAsync(int musicId)
    {
        await using var connection = dbConnection.GetConnection();
        await connection.OpenAsync();

        var query = "SELECT * FROM music WHERE id = @musicId";
        await using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("musicId", musicId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapMusic(reader);
        }
        return null;
    }

    public async Task<(List<Music> MusicList, int TotalCount)> GetByArtistIdAsync(int artistId, int page, int pageSize)
    {
        await using var connection = dbConnection.GetConnection();
        await connection.OpenAsync();

        var countQuery = "SELECT COUNT(*) FROM music WHERE artist_id = @artistId";
        await using var countCmd = new NpgsqlCommand(countQuery, connection);
        countCmd.Parameters.AddWithValue("artistId", artistId);
        var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        var query = @"SELECT id, artist_id, title, album_name, genre, created_at, updated_at 
                      FROM music 
                      WHERE artist_id = @artistId
                      ORDER BY id DESC 
                      LIMIT @pageSize OFFSET @offset";
        await using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("artistId", artistId);
        cmd.Parameters.AddWithValue("pageSize", pageSize);
        cmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);

        var musicList = new List<Music>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            musicList.Add(MapMusic(reader));
        }

        return (musicList, totalCount);
    }

    public async Task<Music> CreateAsync(int artistId, CreateMusicRequest createRequest)
    {
        await using var connection = dbConnection.GetConnection();
        await connection.OpenAsync();

        var query = @"INSERT INTO music (artist_id, title, album_name, genre, created_at, updated_at)
                      VALUES (@artistId, @title, @albumName, @genre::genre_type, @createdAt, @updatedAt)
                      RETURNING id";

        await using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("artistId", artistId);
        cmd.Parameters.AddWithValue("title", createRequest.Title);
        cmd.Parameters.AddWithValue("albumName", (object?)createRequest.AlbumName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("genre", createRequest.Genre);
        cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);        var newMusicId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        
        return (await GetByIdAsync(newMusicId))!;
    }

    public async Task<Music> UpdateAsync(int musicId, UpdateMusicRequest updateRequest)
    {
        await using var connection = dbConnection.GetConnection();
        await connection.OpenAsync();

        var updates = new List<string>();
        if (updateRequest.Title != null) updates.Add("title = @title");
        if (updateRequest.AlbumName != null) updates.Add("album_name = @albumName");
        if (updateRequest.Genre != null) updates.Add("genre = @genre::genre_type");
        updates.Add("updated_at = @updatedAt");

        var query = $"UPDATE music SET {string.Join(", ", updates)} WHERE id = @musicId";

        await using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("musicId", musicId);
        if (updateRequest.Title != null) cmd.Parameters.AddWithValue("title", updateRequest.Title);
        if (updateRequest.AlbumName != null) cmd.Parameters.AddWithValue("albumName", updateRequest.AlbumName);
        if (updateRequest.Genre != null) cmd.Parameters.AddWithValue("genre", updateRequest.Genre);
        cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

        await cmd.ExecuteNonQueryAsync();

        return (await GetByIdAsync(musicId))!;
    }

    public async Task<bool> DeleteAsync(int musicId)
    {
        await using var connection = dbConnection.GetConnection();
        await connection.OpenAsync();

        var query = "DELETE FROM music WHERE id = @musicId";
        await using var cmd = new NpgsqlCommand(query, connection);
        cmd.Parameters.AddWithValue("musicId", musicId);

        var rowsAffected = await cmd.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    private static Music MapMusic(NpgsqlDataReader reader)
    {
        var genreOrdinal = reader.GetOrdinal("genre");
        MusicGenre genre = MusicGenre.rock;
        if (!reader.IsDBNull(genreOrdinal))
        {
            var genreValue = reader.GetValue(genreOrdinal);
            var genreString = genreValue?.ToString();
            if (!string.IsNullOrEmpty(genreString) && Enum.TryParse<MusicGenre>(genreString, out var parsedGenre))
            {
                genre = parsedGenre;
            }
        }

        return new Music
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            ArtistId = reader.GetInt32(reader.GetOrdinal("artist_id")),
            Title = reader.GetString(reader.GetOrdinal("title")),
            AlbumName = reader.IsDBNull(reader.GetOrdinal("album_name")) ? null : reader.GetString(reader.GetOrdinal("album_name")),
            Genre = genre,
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
        };
    }
}
