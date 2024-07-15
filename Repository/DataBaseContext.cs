using EfcoreTest.Repository.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using Npgsql;

namespace EfcoreTest.Repository;

abstract class DataBaseContext : DbContext
{
    public DbSet<DbPost> Posts { get; set; }
    public DbSet<DbTag> Tags { get; set; }
    public DbSet<DbPostTag> PostTags { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<Guid>().HaveConversion<GuidToStringConverter>();
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //多対多のリレーションを明示的に指定
        modelBuilder.Entity<DbPost>()
            .HasMany(e => e.Tags)
            .WithMany(e => e.Posts)
            .UsingEntity<DbPostTag>
            (
                l => l.HasOne(e => e.Tag).WithMany(e => e.PostTags).HasForeignKey(e => e.TagId),
                r => r.HasOne(e => e.Post).WithMany(e => e.PostTags).HasForeignKey(e => e.PostId),
                j => j.HasKey(e => new { e.PostId, e.TagId })
            );
    }
}

class SqliteContext : DataBaseContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseSqlite("Data Source=./efcore_sqlite.db");
        optionsBuilder.LogTo(Console.Error.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information).EnableSensitiveDataLogging();
    }
}

class PgSqlContext : DataBaseContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Database = "ef_blog",
            Host = "localhost",
            Port = 5432,
            Username = "efcoretestuser",
            Password = "42a4a890-c27c-4894-a2e2-159223f8d976"
        };
        optionsBuilder.UseNpgsql(builder.ToString());
        optionsBuilder.LogTo(Console.Error.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information).EnableSensitiveDataLogging();
    }

}