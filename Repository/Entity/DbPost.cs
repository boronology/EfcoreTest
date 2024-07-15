using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace EfcoreTest.Repository.Entity;

[Table("posts")]
[PrimaryKey(nameof(PostId))]
class DbPost
{
    [Column("post_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid PostId { get; set; }
    [Column("title")]
    public string Title { get; set; }

    public List<DbTag> Tags { get; set; } = [];
    public List<DbPostTag> PostTags { get; set; } = [];
}