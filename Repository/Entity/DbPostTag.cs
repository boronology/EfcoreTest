using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace EfcoreTest.Repository.Entity;

[Table("post_to_tag")]
class DbPostTag
{
    [Column("post_id")]
    [ForeignKey(nameof(DbPost))]
    public Guid PostId { get; set; }
    [Column("tag_id")]
    [ForeignKey(nameof(DbTag))]
    public Guid TagId { get; set; }

    public DbPost Post { get; }
    public DbTag Tag { get; }
}