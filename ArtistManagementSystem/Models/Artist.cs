namespace ArtistManagementSystem.Models;

public class Artist
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? Dob { get; set; }
    public Gender? Gender { get; set; }
    public string? Address { get; set; }
    public int FirstReleaseYear { get; set; }
    public int NoOfAlbumsReleased { get; set; } = 0;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateArtistRequest
{
    public string Name { get; set; } = string.Empty;
    public DateTime? Dob { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public int FirstReleaseYear { get; set; }
    public int NoOfAlbumsReleased { get; set; } = 0;
}

public class UpdateArtistRequest
{
    public string? Name { get; set; }
    public DateTime? Dob { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public int? FirstReleaseYear { get; set; }
    public int? NoOfAlbumsReleased { get; set; }
}
