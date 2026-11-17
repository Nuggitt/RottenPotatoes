using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using RottenPotatoes.Data.Entities;

namespace RottenPotatoes.Data.Context;

public partial class RottenPotatoesDbContext : DbContext
{
    public RottenPotatoesDbContext(DbContextOptions<RottenPotatoesDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Actor> Actors { get; set; }

    public virtual DbSet<Award> Awards { get; set; }

    public virtual DbSet<Director> Directors { get; set; }

    public virtual DbSet<Genre> Genres { get; set; }

    public virtual DbSet<Movie> Movies { get; set; }

    public virtual DbSet<MovieRatingOverview> MovieRatingOverviews { get; set; }

    public virtual DbSet<Rating> Ratings { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Series> Series { get; set; }

    public virtual DbSet<SeriesRatingOverview> SeriesRatingOverviews { get; set; }

    public virtual DbSet<StreamingService> StreamingServices { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserActivity> UserActivities { get; set; }

    public virtual DbSet<UserActivityArchive> UserActivityArchives { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Actor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.Name, "IX_Actors_Name");

            entity.Property(e => e.Biography).HasColumnType("text");
            entity.Property(e => e.Name).HasMaxLength(150);
        });

        modelBuilder.Entity<Award>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.Name, "UQ_Awards_Name").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(150);
        });

        modelBuilder.Entity<Director>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.Name, "IX_Directors_Name");

            entity.Property(e => e.Biography).HasColumnType("text");
            entity.Property(e => e.Name).HasMaxLength(150);
        });

        modelBuilder.Entity<Genre>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.Name, "UQ_Genres_Name").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Movie>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.ReleaseDate, "IX_Movies_ReleaseDate");

            entity.HasIndex(e => e.Title, "IX_Movies_Title");

            entity.Property(e => e.AgeRating).HasMaxLength(20);
            entity.Property(e => e.AverageRating).HasPrecision(4, 2);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Language).HasMaxLength(50);
            entity.Property(e => e.Resume).HasColumnType("text");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.HasMany(d => d.Actors).WithMany(p => p.Movies)
                .UsingEntity<Dictionary<string, object>>(
                    "MovieActor",
                    r => r.HasOne<Actor>().WithMany()
                        .HasForeignKey("ActorId")
                        .HasConstraintName("FK_MovieActors_Actors"),
                    l => l.HasOne<Movie>().WithMany()
                        .HasForeignKey("MovieId")
                        .HasConstraintName("FK_MovieActors_Movies"),
                    j =>
                    {
                        j.HasKey("MovieId", "ActorId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("MovieActors");
                        j.HasIndex(new[] { "ActorId" }, "IX_MovieActors_ActorId");
                    });

            entity.HasMany(d => d.Awards).WithMany(p => p.Movies)
                .UsingEntity<Dictionary<string, object>>(
                    "MovieAward",
                    r => r.HasOne<Award>().WithMany()
                        .HasForeignKey("AwardId")
                        .HasConstraintName("FK_MovieAwards_Awards"),
                    l => l.HasOne<Movie>().WithMany()
                        .HasForeignKey("MovieId")
                        .HasConstraintName("FK_MovieAwards_Movies"),
                    j =>
                    {
                        j.HasKey("MovieId", "AwardId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("MovieAwards");
                        j.HasIndex(new[] { "AwardId" }, "IX_MovieAwards_AwardId");
                    });

            entity.HasMany(d => d.Directors).WithMany(p => p.Movies)
                .UsingEntity<Dictionary<string, object>>(
                    "MovieDirector",
                    r => r.HasOne<Director>().WithMany()
                        .HasForeignKey("DirectorId")
                        .HasConstraintName("FK_MovieDirectors_Directors"),
                    l => l.HasOne<Movie>().WithMany()
                        .HasForeignKey("MovieId")
                        .HasConstraintName("FK_MovieDirectors_Movies"),
                    j =>
                    {
                        j.HasKey("MovieId", "DirectorId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("MovieDirectors");
                        j.HasIndex(new[] { "DirectorId" }, "IX_MovieDirectors_DirectorId");
                    });

            entity.HasMany(d => d.Genres).WithMany(p => p.Movies)
                .UsingEntity<Dictionary<string, object>>(
                    "MovieGenre",
                    r => r.HasOne<Genre>().WithMany()
                        .HasForeignKey("GenreId")
                        .HasConstraintName("FK_MovieGenres_Genres"),
                    l => l.HasOne<Movie>().WithMany()
                        .HasForeignKey("MovieId")
                        .HasConstraintName("FK_MovieGenres_Movies"),
                    j =>
                    {
                        j.HasKey("MovieId", "GenreId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("MovieGenres");
                        j.HasIndex(new[] { "GenreId" }, "IX_MovieGenres_GenreId");
                    });

            entity.HasMany(d => d.StreamingServices).WithMany(p => p.Movies)
                .UsingEntity<Dictionary<string, object>>(
                    "MovieStreamingService",
                    r => r.HasOne<StreamingService>().WithMany()
                        .HasForeignKey("StreamingServiceId")
                        .HasConstraintName("FK_MovieStreamingServices_Services"),
                    l => l.HasOne<Movie>().WithMany()
                        .HasForeignKey("MovieId")
                        .HasConstraintName("FK_MovieStreamingServices_Movies"),
                    j =>
                    {
                        j.HasKey("MovieId", "StreamingServiceId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("MovieStreamingServices");
                        j.HasIndex(new[] { "StreamingServiceId" }, "IX_MovieStreamingServices_ServiceId");
                    });
        });

        modelBuilder.Entity<MovieRatingOverview>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("MovieRatingOverview");

            entity.Property(e => e.AverageRating).HasPrecision(4, 2);
            entity.Property(e => e.MovieId).HasDefaultValueSql("'0'");
            entity.Property(e => e.Title).HasMaxLength(255);
        });

        modelBuilder.Entity<Rating>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.MovieId, "IX_Ratings_MovieId");

            entity.HasIndex(e => e.SeriesId, "IX_Ratings_SeriesId");

            entity.HasIndex(e => new { e.UserId, e.MovieId }, "UQ_Ratings_User_Movie").IsUnique();

            entity.HasIndex(e => new { e.UserId, e.SeriesId }, "UQ_Ratings_User_Series").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Score).HasPrecision(3, 1);
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Movie).WithMany(p => p.Ratings)
                .HasForeignKey(d => d.MovieId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Ratings_Movies");

            entity.HasOne(d => d.Series).WithMany(p => p.Ratings)
                .HasForeignKey(d => d.SeriesId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Ratings_Series");

            entity.HasOne(d => d.User).WithMany(p => p.Ratings)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Ratings_Users");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.RatingId, "UQ_Reviews_RatingId").IsUnique();

            entity.Property(e => e.Content).HasColumnType("text");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Rating).WithOne(p => p.Review)
                .HasForeignKey<Review>(d => d.RatingId)
                .HasConstraintName("FK_Reviews_Ratings");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.Name, "UQ_Roles_Name").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(20);
        });

        modelBuilder.Entity<Series>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.ReleaseDate, "IX_Series_ReleaseDate");

            entity.HasIndex(e => e.Title, "IX_Series_Title");

            entity.Property(e => e.AgeRating).HasMaxLength(20);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Language).HasMaxLength(50);
            entity.Property(e => e.Resume).HasColumnType("text");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.HasMany(d => d.Actors).WithMany(p => p.Series)
                .UsingEntity<Dictionary<string, object>>(
                    "SeriesActor",
                    r => r.HasOne<Actor>().WithMany()
                        .HasForeignKey("ActorId")
                        .HasConstraintName("FK_SeriesActors_Actors"),
                    l => l.HasOne<Series>().WithMany()
                        .HasForeignKey("SeriesId")
                        .HasConstraintName("FK_SeriesActors_Series"),
                    j =>
                    {
                        j.HasKey("SeriesId", "ActorId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("SeriesActors");
                        j.HasIndex(new[] { "ActorId" }, "IX_SeriesActors_ActorId");
                    });

            entity.HasMany(d => d.Awards).WithMany(p => p.Series)
                .UsingEntity<Dictionary<string, object>>(
                    "SeriesAward",
                    r => r.HasOne<Award>().WithMany()
                        .HasForeignKey("AwardId")
                        .HasConstraintName("FK_SeriesAwards_Awards"),
                    l => l.HasOne<Series>().WithMany()
                        .HasForeignKey("SeriesId")
                        .HasConstraintName("FK_SeriesAwards_Series"),
                    j =>
                    {
                        j.HasKey("SeriesId", "AwardId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("SeriesAwards");
                        j.HasIndex(new[] { "AwardId" }, "IX_SeriesAwards_AwardId");
                    });

            entity.HasMany(d => d.Directors).WithMany(p => p.Series)
                .UsingEntity<Dictionary<string, object>>(
                    "SeriesDirector",
                    r => r.HasOne<Director>().WithMany()
                        .HasForeignKey("DirectorId")
                        .HasConstraintName("FK_SeriesDirectors_Directors"),
                    l => l.HasOne<Series>().WithMany()
                        .HasForeignKey("SeriesId")
                        .HasConstraintName("FK_SeriesDirectors_Series"),
                    j =>
                    {
                        j.HasKey("SeriesId", "DirectorId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("SeriesDirectors");
                        j.HasIndex(new[] { "DirectorId" }, "IX_SeriesDirectors_DirectorId");
                    });

            entity.HasMany(d => d.Genres).WithMany(p => p.Series)
                .UsingEntity<Dictionary<string, object>>(
                    "SeriesGenre",
                    r => r.HasOne<Genre>().WithMany()
                        .HasForeignKey("GenreId")
                        .HasConstraintName("FK_SeriesGenres_Genres"),
                    l => l.HasOne<Series>().WithMany()
                        .HasForeignKey("SeriesId")
                        .HasConstraintName("FK_SeriesGenres_Series"),
                    j =>
                    {
                        j.HasKey("SeriesId", "GenreId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("SeriesGenres");
                        j.HasIndex(new[] { "GenreId" }, "IX_SeriesGenres_GenreId");
                    });

            entity.HasMany(d => d.StreamingServices).WithMany(p => p.Series)
                .UsingEntity<Dictionary<string, object>>(
                    "SeriesStreamingService",
                    r => r.HasOne<StreamingService>().WithMany()
                        .HasForeignKey("StreamingServiceId")
                        .HasConstraintName("FK_SeriesStreamingServices_Services"),
                    l => l.HasOne<Series>().WithMany()
                        .HasForeignKey("SeriesId")
                        .HasConstraintName("FK_SeriesStreamingServices_Series"),
                    j =>
                    {
                        j.HasKey("SeriesId", "StreamingServiceId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("SeriesStreamingServices");
                        j.HasIndex(new[] { "StreamingServiceId" }, "IX_SeriesStreamingServices_ServiceId");
                    });
        });

        modelBuilder.Entity<SeriesRatingOverview>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("SeriesRatingOverview");

            entity.Property(e => e.AverageRating).HasPrecision(4, 2);
            entity.Property(e => e.SeriesId).HasDefaultValueSql("'0'");
            entity.Property(e => e.Title).HasMaxLength(255);
        });

        modelBuilder.Entity<StreamingService>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.Name, "UQ_StreamingServices_Name").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.RoleId, "IX_Users_RoleId");

            entity.HasIndex(e => e.Email, "UQ_Users_Email").IsUnique();

            entity.HasIndex(e => e.Username, "UQ_Users_Username").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.RoleId).HasDefaultValueSql("'1'");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Username).HasMaxLength(50);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");
        });

        modelBuilder.Entity<UserActivity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("UserActivity");

            entity.HasIndex(e => e.ActivityAt, "IX_UserActivity_ActivityAt");

            entity.HasIndex(e => e.UserId, "IX_UserActivity_UserId");

            entity.Property(e => e.ActivityAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.ActivityType).HasMaxLength(50);

            entity.HasOne(d => d.User).WithMany(p => p.UserActivities)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_UserActivity_Users");
        });

        modelBuilder.Entity<UserActivityArchive>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("UserActivityArchive");

            entity.HasIndex(e => e.ActivityAt, "IX_UserActivityArchive_ActivityAt");

            entity.HasIndex(e => e.UserId, "IX_UserActivityArchive_UserId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ActivityAt).HasColumnType("datetime");
            entity.Property(e => e.ActivityType).HasMaxLength(50);
            entity.Property(e => e.ArchivedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
