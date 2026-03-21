# Artist Management System

A .NET 8 Web API with React-like frontend for managing artists and their music.

## Tech Stack

- Backend: .NET 8, PostgreSQL, Npgsql
- Frontend: Vanilla JavaScript
- Docker: PostgreSQL 16

## Prerequisites

- .NET 8 SDK
- PostgreSQL (for local development without Docker)
- Docker & Docker Compose (for Docker setup)

## Running the Application

### Option 1: With Docker (Recommended)

1. Make sure Docker is running
2. Navigate to the project directory
3. Build and start the containers:

```bash
cd ArtistManagementSystem
docker-compose up --build
```

4. The application will be available at: `http://localhost:5013/index.html`
5. PostgreSQL will be running on port `5433` (to avoid conflict with local port 5432)

**Default Users (seeded on first run):**
| Role | Email | Password |
|------|-------|----------|
| Super Admin | super@admin.com | 1234 |
| Artist Manager | artist@manager.com | 1234 |
| Artist | artist@artist.com | 1234 |

### Option 2: Without Docker (Local)

1. Ensure PostgreSQL is running locally on port 5432
2. Create a database named `artist_management`
3. Update connection string in `appsettings.json` if needed
4. Run the application:

```bash
cd ArtistManagementSystem
dotnet restore
dotnet run
```

5. Access the frontend at: `http://localhost:5013/index.html`

## CSV Import

A sample CSV file `sample_artists_import.csv` is provided in the parent directory for testing the import feature.

### CSV Format

```csv
name,first_release_year,dob,gender,address,no_of_albums_released
John Doe,2020,1990-01-15,m,New York,5
Jane Smith,2018,1992-05-20,f,Los Angeles,10
```

### Columns

| Column | Required | Description |
|--------|----------|-------------|
| name | Yes | Artist's full name |
| first_release_year | Yes | Year of first release |
| dob | No | Date of birth (YYYY-MM-DD) |
| gender | No | m (male), f (female), o (other) |
| address | No | Contact address |
| no_of_albums_released | No | Number of albums released (default: 0) |

## Note on Requirement 3.1 — Login Page Registration Option
The requirement states the login screen should have an option for new registration. This has intentionally not been implemented because this is an internal role-based system — allowing open registration would let anyone create an account and assign themselves any role including super_admin, which is a security risk. Instead, all user accounts are created and managed exclusively by the Super Admin via the User Management section after logging in.


## Note on Artist Import

- Existing artists (matched by name, case-insensitive) will be skipped during import
- The first two rows (header + first data row) in the sample file are valid for import
