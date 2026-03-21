namespace ArtistManagementSystem.Models;

public class Music
{
    public int Id { get; set; }
    public int ArtistId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? AlbumName { get; set; }
    public MusicGenre Genre { get; set; } = MusicGenre.rock;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateMusicRequest
{
    public string Title { get; set; } = string.Empty;
    public string? AlbumName { get; set; }
    public string Genre { get; set; } = "rock";
}

public class UpdateMusicRequest
{
    public string? Title { get; set; }
    public string? AlbumName { get; set; }
    public string? Genre { get; set; }
}
