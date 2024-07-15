using System.Diagnostics.Tracing;
using System.Text.Json;

using EfcoreTest.Repository;
using EfcoreTest.Repository.Entity;

using Microsoft.EntityFrameworkCore;

namespace EfcoreTest;

public class Program
{
    static DataBaseContext GetDataBaseContext()
    {
        //return new SqliteContext();
        return new PgSqlContext();
    }
    static void ResetDatabase()
    {
        using var context = GetDataBaseContext();
        context.PostTags.ExecuteDelete();
        context.Posts.ExecuteDelete();
        context.Tags.ExecuteDelete();

        context.Posts.AddRange([
            new DbPost { PostId = Constants.PostId1, Title = "Article A" },
            new DbPost { PostId = Constants.PostId2, Title = "Post B" },
            new DbPost { PostId = Constants.PostId3, Title = "News C" },
            new DbPost { PostId = Constants.PostId4, Title = "Paper D" },
        ]);
        context.Tags.AddRange([
            new DbTag { TagId = Constants.AdaTagId, Name = "Ada" },
            new DbTag { TagId = Constants.ErlangTagId, Name = "Erlang" },
            new DbTag { TagId = Constants.JavaTagId, Name = "Java" },
            new DbTag { TagId = Constants.PythonTagId, Name = "Python" },
            new DbTag { TagId = Constants.RubyTagId, Name = "Ruby" },
            new DbTag { TagId = Constants.PerlTagId, Name = "Perl" },
            new DbTag { TagId = Constants.RustTagId, Name = "Rust" },
        ]);
        context.PostTags.AddRange([
            new DbPostTag { PostId = Constants.PostId1, TagId = Constants.AdaTagId },
            new DbPostTag { PostId = Constants.PostId1, TagId = Constants.PythonTagId },
            new DbPostTag { PostId = Constants.PostId1, TagId = Constants.RustTagId },
            new DbPostTag { PostId = Constants.PostId4, TagId = Constants.AdaTagId },
            new DbPostTag { PostId = Constants.PostId4, TagId = Constants.RubyTagId },
            new DbPostTag { PostId = Constants.PostId4, TagId = Constants.PerlTagId },
        ]);
        context.SaveChanges();

    }

    /// <summary>
    /// 一度TagsごとPostを取得し、Tagを追加して保存する。
    /// 明示的でわかりやすい方法
    /// </summary>
    static void AddNewPostTags(Guid postId, IEnumerable<Guid> tagIds)
    {
        using var context = GetDataBaseContext();
        var newTags = tagIds.Select(id => new DbTag { TagId = id, }).ToArray();
        context.AttachRange(newTags);
        var post = context.Posts.Include(e => e.Tags).FirstOrDefault(e => e.PostId == postId);

        post.Tags.AddRange(newTags);
        context.SaveChanges();
    }

    /// <summary>
    /// 重複のないTagを追加するだけであれば事前にPostを取得する必要はない。
    /// 直接中間テーブルにAddすればよい。
    /// </summary>
    static void AddToPostTags(Guid postId, IEnumerable<Guid> addTagIds)
    {
        using var context = GetDataBaseContext();

        var postTags = addTagIds.Select(id => new DbPostTag { PostId = postId, TagId = id });
        context.PostTags.AddRange(postTags);

        context.SaveChanges();
    }

    /// <summary>
    /// Tagを追加する場合は直接to-manyのプロパティに追加してもよい
    /// </summary>
    /// <param name="postId"></param>
    /// <param name="addTagIds"></param>
    static void AddToTags(Guid postId, IEnumerable<Guid> addTagIds)
    {
        using var context = GetDataBaseContext();
        var tags = addTagIds.Select(id => new DbTag { TagId = id }).ToArray();
        context.AttachRange(tags);

        var post = context.Posts.FirstOrDefault(e => e.PostId == postId);
        post.Tags.AddRange(tags);
        context.SaveChanges();
    }

    /// <summary>
    /// 追加するTagに既存のTag重複するものがある場合は一度取得して自分でチェックが必要
    /// </summary>
    /// <param name="postId"></param>
    /// <param name="addTagIds"></param>
    static void AddWithDuplicate(Guid postId, IEnumerable<Guid> addTagIds)
    {
        using var context = GetDataBaseContext();
        var post = context.Posts.Include(e => e.PostTags).FirstOrDefault(e => e.PostId == postId);

        var newTagIds = new HashSet<Guid>(addTagIds);
        newTagIds.UnionWith(post.PostTags.Select(e => e.TagId));
        var newPostTags = newTagIds.Select(id => new DbPostTag { TagId = id });
        post.PostTags = newPostTags.ToList();

        context.SaveChanges();
    }


    /// <summary>
    /// すべてのTagを削除するなら中間テーブルにExecuteDeleteするだけでよい
    /// </summary>
    static void DeleteAllTags(Guid postId)
    {
        using var context = GetDataBaseContext();
        context.PostTags
            .Where(e => e.PostId == postId)
            .ExecuteDelete();
    }

    /// <summary>
    /// すべてのTagを付け替える簡単な方法はない。一度取得してから再度追加する
    /// </summary>
    static void ReplaceTags(Guid postId, IEnumerable<Guid> newTags)
    {
        using var context = GetDataBaseContext();

        //一度取得
        var post = context.Posts.Include(e => e.PostTags).FirstOrDefault(e => e.PostId == postId);

        var postTags = newTags.Select(id => new DbPostTag { TagId = id }).ToList();

        //1. すべて削除してあらためて追加
        post.PostTags.Clear();
        post.PostTags.AddRange(postTags);

        //2. リストごと入れ替え
        //post.PostTags = postTags;

        context.SaveChanges();
    }

    /// <summary>
    /// Tagを選択して削除するときも中間テーブルを操作する
    /// </summary>
    static void DeleteSomeTags(Guid postId, IEnumerable<Guid> deleteTags)
    {
        using var context = GetDataBaseContext();
        context.PostTags
            .Where(e => e.PostId == postId && deleteTags.Contains(e.TagId))
            .ExecuteDelete();
    }

    public static void Main()
    {
        ResetDatabase();

        //Read
        Console.WriteLine("初期状態（Article Aのタグは[Ada, Python, Rust]）");
        DumpPosts();

        //Delete (1)
        Console.WriteLine("すべてのタグを削除する ->[]");
        ResetDatabase();
        DeleteAllTags(Constants.PostId1);
        DumpPosts();

        //Delete (2)
        Console.WriteLine("PythonとRustだけ削除する ->[Ada]");
        ResetDatabase();
        DeleteSomeTags(Constants.PostId1, [Constants.PythonTagId, Constants.RustTagId]);
        DumpPosts();

        //Create (1)
        Console.WriteLine("Perl, Javaを追加する ->[Ada, Python, Rust, Perl, Java]");
        AddNewPostTags(Constants.PostId1, [Constants.PerlTagId, Constants.JavaTagId]);
        DumpPosts();

        //Create (2)
        Console.WriteLine("RubyとErlangをTags経由で追加する ->[Ada, Python, Rust, Ruby, Erlang]");
        ResetDatabase();
        AddToTags(Constants.PostId1, [Constants.RubyTagId, Constants.ErlangTagId]);
        DumpPosts();

        //Create (3)
        Console.WriteLine("RubyとErlangをPostTagsに追加する ->[Ada, Python, Rust, Ruby, Erlang]");
        ResetDatabase();
        AddToPostTags(Constants.PostId1, [Constants.RubyTagId, Constants.ErlangTagId]);
        DumpPosts();

        //Update (1-2)
        Console.WriteLine("RubyとErlangをTags経由で追加する ->[Ada, Python, Rust, Ruby, Erlang]");
        ResetDatabase();
        AddToTags(Constants.PostId1, [Constants.RubyTagId, Constants.ErlangTagId]);
        DumpPosts();

        //Update (1)
        Console.WriteLine("ErlangとJavaとPythonに置き換える ->[Erlang, Java, Python]");
        ResetDatabase();
        ReplaceTags(Constants.PostId1, [Constants.ErlangTagId, Constants.JavaTagId, Constants.PythonTagId]);
        DumpPosts();

        //Update (2)
        Console.WriteLine("JavaとすでにTagsにあるAdaを追加する ->[Ada, Python, Rust, Java]");
        ResetDatabase();
        AddWithDuplicate(Constants.PostId1, [Constants.AdaTagId, Constants.JavaTagId]);
        DumpPosts();
    }

    #region  出力用
    private static void DumpByJson(object o)
    {
        Console.WriteLine("********************************************************");
        var text = JsonSerializer.Serialize(o, new JsonSerializerOptions
        {
            WriteIndented = true,
        });
        Console.WriteLine(text);
    }

    private static void DumpPosts()
    {
        using var context = GetDataBaseContext();
        var allPosts = context.Posts.Include(e => e.Tags);
        var displayPosts = allPosts.Select(e => new
        {
            Title = e.Title,
            PostId = e.PostId,
            Tags = e.Tags.Select(s => new
            {
                Name = s.Name,
                TagId = s.TagId,
            }).ToArray(),
        }).ToArray();
        DumpByJson(displayPosts);
    }
    #endregion
}