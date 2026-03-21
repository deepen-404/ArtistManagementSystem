using ArtistManagementSystem.Data;
using ArtistManagementSystem.Middleware;
using ArtistManagementSystem.Models;
using ArtistManagementSystem.Services;

namespace ArtistManagementSystem.Features.Music;

public static class MusicEndpoints
{
    public static RouteGroupBuilder MapMusicEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAllMusic)
              .RequireAnyRole("super_admin", "artist_manager", "artist");
        
        group.MapGet("/{musicId}", GetMusicById)
             .RequireAnyRole("super_admin", "artist_manager", "artist");
        
        group.MapPost("/", CreateMusic)
             .RequireRole("artist");
        
        group.MapPut("/{musicId}", UpdateMusic)
             .RequireRole("artist");
        
        group.MapDelete("/{musicId}", DeleteMusic)
             .RequireRole("artist");
        
        return group;
    }

    private static async Task<IResult> GetAllMusic(int artistId, HttpContext httpContext, MusicRepository musicRepository, ArtistRepository artistRepository)
    {
        var artist = await artistRepository.GetByIdAsync(artistId);
        if (artist == null) return Results.NotFound(new { error = "Artist not found" });

        var page = int.Parse(httpContext.Request.Query["page"].FirstOrDefault() ?? "1");
        var pageSize = int.Parse(httpContext.Request.Query["pageSize"].FirstOrDefault() ?? "10");
        var (musicList, totalCount) = await musicRepository.GetByArtistIdAsync(artistId, page, pageSize);
        return Results.Ok(new { artist, data = musicList, totalCount, page, pageSize });
    }

    private static async Task<IResult> GetMusicById(int artistId, int musicId, MusicRepository musicRepository)
    {
        var music = await musicRepository.GetByIdAsync(musicId);
        if (music == null || music.ArtistId != artistId) return Results.NotFound(new { error = "Music not found" });
        return Results.Ok(music);
    }

    private static async Task<IResult> CreateMusic(int artistId, CreateMusicRequest createRequest, MusicRepository musicRepository, ArtistRepository artistRepository)
    {
        var artist = await artistRepository.GetByIdAsync(artistId);
        if (artist == null) return Results.NotFound(new { error = "Artist not found" });

        if (string.IsNullOrEmpty(createRequest.Title) || string.IsNullOrEmpty(createRequest.Genre))
            return Results.BadRequest(new { error = "Title and genre are required" });

        var createdMusic = await musicRepository.CreateAsync(artistId, createRequest);
        return Results.Created($"/api/artists/{artistId}/music/{createdMusic.Id}", createdMusic);
    }

    private static async Task<IResult> UpdateMusic(int artistId, int musicId, UpdateMusicRequest updateRequest, MusicRepository musicRepository)
    {
        var existingMusic = await musicRepository.GetByIdAsync(musicId);
        if (existingMusic == null || existingMusic.ArtistId != artistId) return Results.NotFound(new { error = "Music not found" });

        var updatedMusic = await musicRepository.UpdateAsync(musicId, updateRequest);
        return Results.Ok(updatedMusic);
    }

    private static async Task<IResult> DeleteMusic(int artistId, int musicId, MusicRepository musicRepository)
    {
        var existingMusic = await musicRepository.GetByIdAsync(musicId);
        if (existingMusic == null || existingMusic.ArtistId != artistId) return Results.NotFound(new { error = "Music not found" });

        var isDeleted = await musicRepository.DeleteAsync(musicId);
        if (!isDeleted) return Results.NotFound(new { error = "Music not found" });
        return Results.Ok(new { message = "Music deleted successfully" });
    }
}
