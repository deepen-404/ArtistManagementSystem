using ArtistManagementSystem.Data;
using ArtistManagementSystem.Middleware;
using ArtistManagementSystem.Models;
using ArtistManagementSystem.Services;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace ArtistManagementSystem.Features.Artist;

public static class ArtistEndpoints
{
    public static RouteGroupBuilder MapArtistEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAllArtists)
              .RequireAnyRole("super_admin", "artist_manager", "artist");
        
        group.MapGet("/{artistId}", GetArtistById)
             .RequireAnyRole("super_admin", "artist_manager");
        
        group.MapGet("/export", ExportArtists)
             .RequireRole("artist_manager");
        
        group.MapPost("/", CreateArtist)
             .RequireRole("artist_manager");
        
        group.MapPut("/{artistId}", UpdateArtist)
             .RequireRole("artist_manager");
        
        group.MapDelete("/{artistId}", DeleteArtist)
             .RequireRole("artist_manager");
        
        group.MapPost("/import", ImportArtists)
             .RequireRole("artist_manager");
        
        return group;
    }

    private static async Task<IResult> GetAllArtists(HttpContext httpContext, ArtistRepository artistRepository)
    {
        var page = int.Parse(httpContext.Request.Query["page"].FirstOrDefault() ?? "1");
        var pageSize = int.Parse(httpContext.Request.Query["pageSize"].FirstOrDefault() ?? "10");
        var (artists, totalCount) = await artistRepository.GetAllAsync(page, pageSize);
        return Results.Ok(new { data = artists, totalCount, page, pageSize });
    }

    private static async Task<IResult> GetArtistById(int artistId, ArtistRepository artistRepository)
    {
        var artist = await artistRepository.GetByIdAsync(artistId);
        if (artist == null) return Results.NotFound(new { error = "Artist not found" });
        return Results.Ok(artist);
    }

    private static async Task<IResult> CreateArtist(CreateArtistRequest createRequest, ArtistRepository artistRepository)
    {
        if (string.IsNullOrEmpty(createRequest.Name) || createRequest.FirstReleaseYear <= 0)
            return Results.BadRequest(new { error = "Name and first_release_year are required" });

        var createdArtist = await artistRepository.CreateAsync(createRequest);
        return Results.Created($"/api/artists/{createdArtist.Id}", createdArtist);
    }

    private static async Task<IResult> UpdateArtist(int artistId, UpdateArtistRequest updateRequest, ArtistRepository artistRepository)
    {
        var existingArtist = await artistRepository.GetByIdAsync(artistId);
        if (existingArtist == null) return Results.NotFound(new { error = "Artist not found" });

        var updatedArtist = await artistRepository.UpdateAsync(artistId, updateRequest);
        return Results.Ok(updatedArtist);
    }

    private static async Task<IResult> DeleteArtist(int artistId, ArtistRepository artistRepository)
    {
        var isDeleted = await artistRepository.DeleteAsync(artistId);
        if (!isDeleted) return Results.NotFound(new { error = "Artist not found" });
        return Results.Ok(new { message = "Artist deleted successfully" });
    }

    private static async Task<IResult> ImportArtists(HttpContext httpContext, ArtistRepository artistRepository)
    {
        var form = await httpContext.Request.ReadFormAsync();
        var uploadedFile = form.Files.FirstOrDefault();
        if (uploadedFile == null || uploadedFile.Length == 0)
            return Results.BadRequest(new { error = "No file uploaded" });

        using var memoryStream = new MemoryStream();
        await uploadedFile.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        
        using var fileReader = new StreamReader(memoryStream);
        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            HeaderValidated = null,
            MissingFieldFound = null,
                PrepareHeaderForMatch = args => args.Header.ToLower().Replace("_", "")
        };
        
        using var csvReader = new CsvReader(fileReader, csvConfig);
        
        await csvReader.ReadAsync();
        csvReader.ReadHeader();
        
        if (csvReader.HeaderRecord == null || csvReader.HeaderRecord.Length == 0)
            return Results.BadRequest(new { error = "No header record found in CSV" });
        
        var records = new List<CreateArtistRequest>();
        var skippedRows = new List<string>();
        var rowNumber = 1;
        
        try
        {
            while (await csvReader.ReadAsync())
            {
                rowNumber++;
                try
                {
                    var name = csvReader.GetField<string>("name");
                    var firstReleaseYear = csvReader.GetField<int?>("firstreleaseyear");
                    
                    if (string.IsNullOrEmpty(name) || firstReleaseYear == null || firstReleaseYear <= 0)
                    {
                        skippedRows.Add($"Row {rowNumber}: Missing required field 'name' or 'first_release_year'");
                        continue;
                    }

                    var record = new CreateArtistRequest
                    {
                        Name = name,
                        FirstReleaseYear = firstReleaseYear.Value,
                        Dob = csvReader.GetField<DateTime?>("dob"),
                        Gender = csvReader.GetField<string>("gender"),
                        Address = csvReader.GetField<string>("address"),
                        NoOfAlbumsReleased = csvReader.GetField<int?>("noofalbumsreleased") ?? 0
                    };
                    records.Add(record);
                }
                catch (Exception ex)
                {
                    skippedRows.Add($"Row {rowNumber}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = $"CSV parsing error: {ex.Message}" });
        }

        if (records.Count == 0)
            return Results.BadRequest(new { error = "No valid records found in CSV", skippedRows });

        var createdArtists = await artistRepository.CreateManyAsync(records);
        
        return Results.Ok(new { 
            totalRows = records.Count + skippedRows.Count, 
            importedCount = createdArtists.Count, 
            skippedCount = skippedRows.Count, 
            skippedRows,
            data = createdArtists 
        });
    }

    private static async Task<IResult> ExportArtists(ArtistRepository artistRepository)
    {
        var (artists, _) = await artistRepository.GetAllAsync(1, int.MaxValue);
        
        using var memoryStream = new MemoryStream();
        await using var streamWriter = new StreamWriter(memoryStream);
        await using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);
        
        csvWriter.WriteField("name");
        csvWriter.WriteField("dob");
        csvWriter.WriteField("gender");
        csvWriter.WriteField("address");
        csvWriter.WriteField("first_release_year");
        csvWriter.WriteField("no_of_albums_released");
        await csvWriter.NextRecordAsync();
        
        foreach (var artist in artists)
        {
            csvWriter.WriteField(artist.Name);
            csvWriter.WriteField(artist.Dob?.ToString("yyyy-MM-dd"));
            csvWriter.WriteField(artist.Gender?.ToString());
            csvWriter.WriteField(artist.Address);
            csvWriter.WriteField(artist.FirstReleaseYear);
            csvWriter.WriteField(artist.NoOfAlbumsReleased);
            await csvWriter.NextRecordAsync();
        }
        
        await streamWriter.FlushAsync();
        memoryStream.Position = 0;
        
        return Results.File(memoryStream.ToArray(), "text/csv", "artists.csv");
    }
}
