using Npgsql;

namespace ArtistManagementSystem.Data;

public class DatabaseInitializer(DbConnection dbConnection)
{
    public void Initialize()
    {
        using var connection = dbConnection.GetConnection();
        connection.Open();

        var createUserRoleTypeScript = @"
            DO $$ 
            BEGIN
                CREATE TYPE user_role AS ENUM ('super_admin', 'artist_manager', 'artist');
            EXCEPTION 
                WHEN duplicate_object THEN null;
            END $$";
        
        var createGenderTypeScript = @"
            DO $$ 
            BEGIN
                CREATE TYPE gender_type AS ENUM ('m', 'f', 'o');
            EXCEPTION 
                WHEN duplicate_object THEN null;
            END $$";

        var createGenreTypeScript = @"
            DO $$ 
            BEGIN
                CREATE TYPE genre_type AS ENUM ('rnb', 'country', 'classic', 'rock', 'jazz');
            EXCEPTION 
                WHEN duplicate_object THEN null;
            END $$";

        var createUsersTableScript = @"
            CREATE TABLE IF NOT EXISTS users (
                id SERIAL PRIMARY KEY,
                first_name VARCHAR(255) NOT NULL,
                last_name VARCHAR(255) NOT NULL,
                email VARCHAR(255) NOT NULL UNIQUE,
                password VARCHAR(500) NOT NULL,
                phone VARCHAR(20),
                dob TIMESTAMP,
                gender gender_type,
                address VARCHAR(255),
                role user_role NOT NULL DEFAULT 'artist',
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );";

        var createArtistsTableScript = @"
            CREATE TABLE IF NOT EXISTS artists (
                id SERIAL PRIMARY KEY,
                name VARCHAR(255) NOT NULL,
                dob TIMESTAMP,
                gender gender_type,
                address VARCHAR(255),
                first_release_year INTEGER NOT NULL,
                no_of_albums_released INTEGER NOT NULL DEFAULT 0,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );";

        var createMusicTableScript = @"
            CREATE TABLE IF NOT EXISTS music (
                id SERIAL PRIMARY KEY,
                artist_id INTEGER NOT NULL REFERENCES artists(id) ON DELETE CASCADE,
                title VARCHAR(255) NOT NULL,
                album_name VARCHAR(255),
                genre genre_type NOT NULL DEFAULT 'rock',
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );";

        var checkUsersScript = "SELECT COUNT(*) FROM users";
        var seedUsersScript = @"
                INSERT INTO public.users (first_name, last_name, email, password, phone, dob, gender, address, role, created_at, updated_at) VALUES
                ('Super', 'Admin', 'super@admin.com', '$2a$11$bNX8K1r3kVtrauLZbEHoaeftUeN4N825O9BpIhHm0DUg4r8SgsLqy', '1234567890', '2000-01-01 00:00:00.000000', 'm', 'User Address', 'super_admin', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
                ('Artist', 'Manager', 'artist@manager.com', '$2a$11$bNX8K1r3kVtrauLZbEHoaeftUeN4N825O9BpIhHm0DUg4r8SgsLqy', '1234567890', '2000-01-01 00:00:00.000000', 'f', 'User Address', 'artist_manager', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
                ('Artist', 'Artist', 'artist@artist.com', '$2a$11$bNX8K1r3kVtrauLZbEHoaeftUeN4N825O9BpIhHm0DUg4r8SgsLqy', '1234567890', '2000-01-01 00:00:00.000000', 'm', 'User Address', 'artist', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);";
        
        using var cmdUserRoleType = new NpgsqlCommand(createUserRoleTypeScript, connection);
        cmdUserRoleType.ExecuteNonQuery();

        using var cmdGenderType = new NpgsqlCommand(createGenderTypeScript, connection);
        cmdGenderType.ExecuteNonQuery();

        using var cmdGenreType = new NpgsqlCommand(createGenreTypeScript, connection);
        cmdGenreType.ExecuteNonQuery();

        using var cmdCreateUsersTable = new NpgsqlCommand(createUsersTableScript, connection);
        cmdCreateUsersTable.ExecuteNonQuery();

        using var cmdCreateArtistTable = new NpgsqlCommand(createArtistsTableScript, connection);
        cmdCreateArtistTable.ExecuteNonQuery();

        using var cmdCreateMusicTable = new NpgsqlCommand(createMusicTableScript, connection);
        cmdCreateMusicTable.ExecuteNonQuery();

        using var cmdCheckUsers = new NpgsqlCommand(checkUsersScript, connection);
        var userCount = (long)cmdCheckUsers.ExecuteScalar()!;

        if (userCount == 0)
        {
            using var cmdSeedUsers = new NpgsqlCommand(seedUsersScript, connection);
            cmdSeedUsers.ExecuteNonQuery();
        }
    } 
}
