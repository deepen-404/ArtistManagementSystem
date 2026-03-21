using ArtistManagementSystem.Data;
using ArtistManagementSystem.Middleware;
using ArtistManagementSystem.Models;
using ArtistManagementSystem.Services;
using ArtistManagementSystem.Features.Auth;
using ArtistManagementSystem.Features.Users;
using ArtistManagementSystem.Features.Artist;
using ArtistManagementSystem.Features.Music;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(["http://localhost:3000", "http://127.0.0.1:3000", "http://localhost:5500", "http://127.0.0.1:5500", "http://localhost:8080", "http://127.0.0.1:8080"])
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "ArtistManagementSystem",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "ArtistManagementSystem",
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "DefaultSecretKey12345"))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies["token"];
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("cookieAuth", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Cookie,
        Name = "token",
        Description = "JWT token stored in HttpOnly cookie. Login via /api/auth/login first to set the cookie automatically."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "cookieAuth"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddSingleton<DbConnection>(sp => new DbConnection(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddSingleton<UserRepository>();
builder.Services.AddSingleton<ArtistRepository>();
builder.Services.AddSingleton<MusicRepository>();
builder.Services.AddSingleton<TokenService>();

var app = builder.Build();

app.Services.GetRequiredService<DatabaseInitializer>().Initialize();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();

app.MapGet("/", () => new { message = "Artist Management System API" });

app.MapGroup("/api/auth").WithTags("Auth").WithOpenApi().MapAuthEndpoints();
app.MapGroup("/api/users").WithTags("Users").WithOpenApi().MapUserEndpoints();
app.MapGroup("/api/artists").WithTags("Artists").WithOpenApi().MapArtistEndpoints();
app.MapGroup("/api/artists/{artistId}/music").WithTags("Music").WithOpenApi().MapMusicEndpoints();

app.Run();
