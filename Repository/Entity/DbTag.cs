using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace EfcoreTest.Repository.Entity;

[Table("tags")]
[PrimaryKey(nameof(TagId))]
class DbTag
{
    [Column("tag_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid TagId { get; set; }
    [Column("name")]
    public string Name { get; set; }

    public List<DbPostTag> PostTags { get; } = [];
    public List<DbPost> Posts { get; } = [];
}