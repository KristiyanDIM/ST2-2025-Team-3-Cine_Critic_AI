using Cine_Critic_AI.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace Cine_Critic_AI.Services
{
    public sealed class DatabaseService
    {
        private static readonly Lazy<DatabaseService> lazy =
            new Lazy<DatabaseService>(() => new DatabaseService());

        public static DatabaseService Instance => lazy.Value;

        private readonly string _connectionString;

        private DatabaseService()
        {
            _connectionString = "Data Source=CineCriticDB.sqlite";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
    CREATE TABLE IF NOT EXISTS Users(
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Username TEXT NOT NULL UNIQUE,
        Email TEXT NOT NULL UNIQUE,
        Password TEXT NOT NULL,
        RegisteredOn TEXT
    );

            CREATE TABLE IF NOT EXISTS Movies(
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Year INTEGER NOT NULL,
                Genre TEXT NOT NULL,
                Director TEXT NOT NULL,
                Description TEXT,
                ImageUrl TEXT
            );

    CREATE TABLE IF NOT EXISTS Reviews(
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Rate INTEGER NOT NULL,
        Comment TEXT,
        EmotionTone TEXT,
        Date TEXT NOT NULL,
        MovieId INTEGER
    );

    CREATE TABLE IF NOT EXISTS ChatMessages(
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        UserId INTEGER NOT NULL,
        Sender TEXT NOT NULL,
        Message TEXT NOT NULL,
        Timestamp TEXT NOT NULL DEFAULT (datetime('now'))
    );


    -- За онлайн интеграция с имейл услуга, използвайте SendGrid, SMTP или друг доставчик.
    CREATE TABLE IF NOT EXISTS Users(
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Username TEXT NOT NULL UNIQUE,
        Email TEXT NOT NULL UNIQUE,
        Password TEXT NOT NULL,
        RegisteredOn TEXT,
        ResetToken TEXT,
        ResetTokenExpiry TEXT
);
    ";
            cmd.ExecuteNonQuery();

            // ✅ Проверка и ако колоната Date е NOT NULL, я правим NULLABLE
            cmd.CommandText = "PRAGMA table_info(Reviews);";
            using (var reviewReader = cmd.ExecuteReader())
            {
                bool dateNotNull = false;
                while (reviewReader.Read())
                {
                    if (reviewReader["name"].ToString() == "Date" &&
                        reviewReader["notnull"].ToString() == "1")
                    {
                        dateNotNull = true;
                        break;
                    }
                }
                reviewReader.Close();

                if (dateNotNull)
                {
                    // Преименуваме старата таблица и създаваме нова, където Date е NULLABLE
                    cmd.CommandText = @"
                ALTER TABLE Reviews RENAME TO Reviews_old;

                CREATE TABLE Reviews(
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Rate INTEGER NOT NULL,
                    Comment TEXT,
                    EmotionTone TEXT,
                    Date TEXT NULL,
                    MovieId INTEGER
                );

                INSERT INTO Reviews (Id, Rate, Comment, EmotionTone, Date, MovieId)
                SELECT Id, Rate, Comment, EmotionTone, Date, MovieId FROM Reviews_old;

                DROP TABLE Reviews_old;";
                    cmd.ExecuteNonQuery();
                }
            }

            // ✅ Проверка и добавяне на MovieId, ако липсва
            cmd.CommandText = "PRAGMA table_info(Reviews);";
            using var reader = cmd.ExecuteReader();
            bool movieIdExists = false;
            while (reader.Read())
            {
                if (reader["name"].ToString() == "MovieId")
                {
                    movieIdExists = true;
                    break;
                }
            }
            reader.Close();

            if (!movieIdExists)
            {
                cmd.CommandText = "ALTER TABLE Reviews ADD COLUMN MovieId INTEGER DEFAULT 1;";
                cmd.ExecuteNonQuery();
            }

            // ✅ Проверка и добавяне на ImageUrl в Movies, ако липсва
            cmd.CommandText = "PRAGMA table_info(Movies);";
            using var reader2 = cmd.ExecuteReader();
            bool imageUrlExists = false;
            while (reader2.Read())
            {
                if (reader2["name"].ToString() == "ImageUrl")
                {
                    imageUrlExists = true;
                    break;
                }
            }
            reader2.Close();

            if (!imageUrlExists)
            {
                cmd.CommandText = "ALTER TABLE Movies ADD COLUMN ImageUrl TEXT;";
                cmd.ExecuteNonQuery();
            }

            // Проверка и добавяне на AddedOn в Movies, ако липсва
            cmd.CommandText = "PRAGMA table_info(Movies);";
            using var reader3 = cmd.ExecuteReader();
            bool addedOnExists = false;
            while (reader3.Read())
            {
                if (reader3["name"].ToString() == "AddedOn")
                {
                    addedOnExists = true;
                    break;
                }
            }
            reader3.Close();
        }

        // ================== USERS ==================
        public void InsertUser(User user)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Users (Username, Email, Password, RegisteredOn)
                VALUES (@Username, @Email, @Password, @RegisteredOn)";
            cmd.Parameters.AddWithValue("@Username", user.Username);
            cmd.Parameters.AddWithValue("@Email", user.Email);
            cmd.Parameters.AddWithValue("@Password", user.Password);
            cmd.Parameters.AddWithValue("@RegisteredOn", user.RegisteredOn.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
        }

        public void UpdateUser(User user)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE Users
                SET Username = @Username,
                    Email = @Email,
                    Password = @Password
                WHERE Id = @Id";
            cmd.Parameters.AddWithValue("@Username", user.Username);
            cmd.Parameters.AddWithValue("@Email", user.Email);
            cmd.Parameters.AddWithValue("@Password", user.Password);
            cmd.Parameters.AddWithValue("@Id", user.Id);
            cmd.ExecuteNonQuery();
        }

        public User GetUserByUsername(string username)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users WHERE Username = @Username";
            cmd.Parameters.AddWithValue("@Username", username);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new User
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Username = reader["Username"].ToString(),
                    Email = reader["Email"].ToString(),
                    Password = reader["Password"].ToString(),
                    RegisteredOn = DateTime.Parse(reader["RegisteredOn"].ToString())
                };
            }
            return null;
        }

        public List<User> GetAllUsers()
        {
            var users = new List<User>();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                users.Add(new User
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Username = reader["Username"].ToString(),
                    Email = reader["Email"].ToString(),
                    Password = reader["Password"].ToString(),
                    RegisteredOn = DateTime.Parse(reader["RegisteredOn"].ToString())
                });
            }
            return users;
        }

        // ================== FORGOT PASSWORD ==================
        // За онлайн интеграция с имейл услуга, използвайте SendGrid, SMTP или друг доставчик.

        // Записва токен за нулиране на парола
        public void SetPasswordResetToken(string email, string token, DateTime expiry)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        UPDATE Users 
        SET ResetToken = @Token, ResetTokenExpiry = @Expiry 
        WHERE Email = @Email";
            cmd.Parameters.AddWithValue("@Token", token);
            cmd.Parameters.AddWithValue("@Expiry", expiry.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.ExecuteNonQuery();
        }

        // Проверява токена и връща потребител
        public User? GetUserByResetToken(string token)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users WHERE ResetToken = @Token";
            cmd.Parameters.AddWithValue("@Token", token);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var expiry = DateTime.Parse(reader["ResetTokenExpiry"].ToString() ?? DateTime.MinValue.ToString());
                if (expiry > DateTime.Now)
                {
                    return new User
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Username = reader["Username"].ToString(),
                        Email = reader["Email"].ToString()
                    };
                }
            }
            return null;
        }

        // Обновява паролата и изчиства токена
        public void ResetPassword(int userId, string newHashedPassword)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        UPDATE Users 
        SET Password = @Password, ResetToken = NULL, ResetTokenExpiry = NULL
        WHERE Id = @Id";
            cmd.Parameters.AddWithValue("@Password", newHashedPassword);
            cmd.Parameters.AddWithValue("@Id", userId);
            cmd.ExecuteNonQuery();
        }

        // ================== DELETE USER ==================
        public void DeleteUser(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Users WHERE Id = @Id";
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();
        }


        // ================== MOVIES ==================
        public void InsertMovie(Movie movie)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        INSERT INTO Movies (Title, Year, Genre, Director, Description, ImageUrl, AddedOn)
        VALUES (@Title, @Year, @Genre, @Director, @Description, @ImageUrl, @AddedOn)";
            cmd.Parameters.AddWithValue("@Title", movie.Title);
            cmd.Parameters.AddWithValue("@Year", movie.Year);
            cmd.Parameters.AddWithValue("@Genre", movie.Genre);
            cmd.Parameters.AddWithValue("@Director", movie.Director);
            cmd.Parameters.AddWithValue("@Description", movie.Description ?? "");
            cmd.Parameters.AddWithValue("@ImageUrl", movie.ImageUrl ?? "");
            cmd.Parameters.AddWithValue("@AddedOn", movie.AddedOn.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
        }

        public void UpdateMovie(Movie movie)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        UPDATE Movies
        SET Title = @Title,
            Year = @Year,
            Genre = @Genre,
            Director = @Director,
            Description = @Description,
            ImageUrl = @ImageUrl
        WHERE Id = @Id";
            cmd.Parameters.AddWithValue("@Title", movie.Title);
            cmd.Parameters.AddWithValue("@Year", movie.Year);
            cmd.Parameters.AddWithValue("@Genre", movie.Genre);
            cmd.Parameters.AddWithValue("@Director", movie.Director);
            cmd.Parameters.AddWithValue("@Description", movie.Description ?? "");
            cmd.Parameters.AddWithValue("@ImageUrl", movie.ImageUrl ?? "");
            cmd.Parameters.AddWithValue("@Id", movie.Id);
            cmd.ExecuteNonQuery();
        }

        public void DeleteMovie(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Movies WHERE Id = @Id";
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();
        }

        public List<Movie> GetAllMovies()
        {
            var movies = new List<Movie>();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Movies";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                movies.Add(new Movie
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Title = reader["Title"].ToString(),
                    Year = Convert.ToInt32(reader["Year"]),
                    Genre = reader["Genre"].ToString(),
                    Director = reader["Director"].ToString(),
                    Description = reader["Description"].ToString(),
                    ImageUrl = reader["ImageUrl"] != DBNull.Value ? reader["ImageUrl"].ToString() : "",
                    AddedOn = reader["AddedOn"] != DBNull.Value
                        ? DateTime.Parse(reader["AddedOn"].ToString())
                        : DateTime.Now // fallback ако е стара база
                });
            }
            return movies;
        }

        public Movie GetMovieById(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Movies WHERE Id = @Id";
            cmd.Parameters.AddWithValue("@Id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Movie
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Title = reader["Title"].ToString(),
                    Year = Convert.ToInt32(reader["Year"]),
                    Genre = reader["Genre"].ToString(),
                    Director = reader["Director"].ToString(),
                    Description = reader["Description"].ToString(),
                    ImageUrl = reader["ImageUrl"] != DBNull.Value ? reader["ImageUrl"].ToString() : "",
                    AddedOn = reader["AddedOn"] != DBNull.Value
                        ? DateTime.Parse(reader["AddedOn"].ToString())
                        : DateTime.Now
                };
            }
            return null;
        }


        // ================== REVIEWS ==================
        public void InsertReview(Review review)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Reviews (Rate, Comment, EmotionTone, Date, MovieId)
                VALUES (@Rate, @Comment, @EmotionTone, @Date, @MovieId)";
            cmd.Parameters.AddWithValue("@Rate", review.Rate);
            cmd.Parameters.AddWithValue("@Comment", review.Comment ?? "");
            cmd.Parameters.AddWithValue("@EmotionTone", review.EmotionTone ?? "");
            cmd.Parameters.AddWithValue("@Date",
                review.Date.HasValue
                    ? review.Date.Value.ToString("yyyy-MM-dd HH:mm:ss")
                    : (object)DBNull.Value); cmd.Parameters.AddWithValue("@MovieId", review.MovieId);
            cmd.ExecuteNonQuery();
        }

        public void UpdateReview(Review review)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE Reviews
                SET Rate = @Rate,
                    Comment = @Comment,
                    EmotionTone = @EmotionTone,
                    Date = @Date
                WHERE Id = @Id";
            cmd.Parameters.AddWithValue("@Rate", review.Rate);
            cmd.Parameters.AddWithValue("@Comment", review.Comment ?? "");
            cmd.Parameters.AddWithValue("@EmotionTone", review.EmotionTone ?? "");
            cmd.Parameters.AddWithValue("@Date",
                review.Date.HasValue
                    ? review.Date.Value.ToString("yyyy-MM-dd HH:mm:ss")
                    : (object)DBNull.Value); cmd.Parameters.AddWithValue("@Id", review.Id);
            cmd.ExecuteNonQuery();
        }

        public void DeleteReview(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Reviews WHERE Id = @Id";
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();
        }

        public List<Review> GetAllReviews()
        {
            var reviews = new List<Review>();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT r.*, m.Title, m.Year, m.Genre, m.Director, m.Description, m.ImageUrl
                FROM Reviews r
                JOIN Movies m ON r.MovieId = m.Id";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                reviews.Add(new Review
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Rate = Convert.ToInt32(reader["Rate"]),
                    Comment = reader["Comment"].ToString(),
                    EmotionTone = reader["EmotionTone"].ToString(),
                    Date = reader["Date"] != DBNull.Value && !string.IsNullOrWhiteSpace(reader["Date"].ToString())
    ? DateTime.Parse(reader["Date"].ToString())
    : (DateTime?)null,
                    MovieId = Convert.ToInt32(reader["MovieId"]),
                    Movie = new Movie
                    {
                        Id = Convert.ToInt32(reader["MovieId"]),
                        Title = reader["Title"].ToString(),
                        Year = Convert.ToInt32(reader["Year"]),
                        Genre = reader["Genre"].ToString(),
                        Director = reader["Director"].ToString(),
                        Description = reader["Description"].ToString(),
                        ImageUrl = reader["ImageUrl"] != DBNull.Value ? reader["ImageUrl"].ToString() : ""
                    }
                });
            }
            return reviews;
        }

        public Review GetReviewById(int id)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Reviews WHERE Id = @Id";
            cmd.Parameters.AddWithValue("@Id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Review
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Rate = Convert.ToInt32(reader["Rate"]),
                    Comment = reader["Comment"].ToString(),
                    EmotionTone = reader["EmotionTone"].ToString(),
                    Date = reader["Date"] != DBNull.Value && !string.IsNullOrWhiteSpace(reader["Date"].ToString())
    ? DateTime.Parse(reader["Date"].ToString())
    : (DateTime?)null,
                    MovieId = reader["MovieId"] != DBNull.Value ? Convert.ToInt32(reader["MovieId"]) : 0
                };
            }
            return null;
        }

        // ================== CHAT MESSAGES INSERT ==================
        public void InsertChatMessage(ChatMessage msg)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        INSERT INTO ChatMessages (UserId, Sender, Message, Timestamp)
        VALUES (@UserId, @Sender, @Message, @Timestamp)";
            cmd.Parameters.AddWithValue("@UserId", msg.UserId);
            cmd.Parameters.AddWithValue("@Sender", msg.Sender);
            cmd.Parameters.AddWithValue("@Message", msg.Message);
            cmd.Parameters.AddWithValue("@Timestamp", msg.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
        }


        // ================== CHAT MESSAGES GET ==================
        public List<ChatMessage> GetChatMessagesByUser(int userId)
        {
            var messages = new List<ChatMessage>();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM ChatMessages WHERE UserId = @UserId ORDER BY Timestamp";
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                messages.Add(new ChatMessage
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    UserId = Convert.ToInt32(reader["UserId"]),
                    Sender = reader["Sender"].ToString(),
                    Message = reader["Message"].ToString(),
                    Timestamp = DateTime.Parse(reader["Timestamp"].ToString())
                });
            }
            return messages;
        }

        // ================== CHAT MESSAGES DELETE ==================
        public void ClearChatByUser(int userId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM ChatMessages WHERE UserId = @UserId";
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.ExecuteNonQuery();
        }



    }
}
