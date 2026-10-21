using CSharpFunctionalExtensions;

namespace TextbookSpider;

public static class Fetch
{
    public delegate ValueTask<Result<string>> GetStringAsyncDelegate(string url, CancellationToken token);

    public static async ValueTask<Result<string>> FetchUrlAsync(
        string id, GetStringAsyncDelegate getStringAsyncDelegate, CancellationToken token)
    {
        var jsonResult = await getStringAsyncDelegate(
            $"https://s-file-1.ykt.cbern.com.cn/zxx/ndrv2/resources/tch_material/details/{id}.json", token);
        token.ThrowIfCancellationRequested();
        return jsonResult.Bind(Parse.ParseUrl);
    }

    // Retained for callers that enumerate catalog records directly.
    public static async IAsyncEnumerable<BookInfo> FetchBookInfosAsync(GetStringAsyncDelegate getStringAsyncDelegate)
    {
        var result = await FetchBookInfosResultAsync(getStringAsyncDelegate, CancellationToken.None);
        if (result.IsSuccess)
            foreach (var book in result.Value) yield return book;
    }

    private static async Task<Result<BookInfo[]>> FetchBookInfosResultAsync(
        GetStringAsyncDelegate getStringAsyncDelegate, CancellationToken token)
    {
        var books = new List<BookInfo>();
        for (var i = 1; i <= 3; i++)
        {
            token.ThrowIfCancellationRequested();
            var json = await getStringAsyncDelegate(
                $"https://s-file-2.ykt.cbern.com.cn/zxx/ndrs/resources/tch_material/part_10{i}.json", token);
            token.ThrowIfCancellationRequested();
            if (json.IsFailure) return Result.Failure<BookInfo[]>($"part_10{i}: {json.Error}");
            // Force lazy parsing inside Result so malformed records do not escape as exceptions.
            var parsed = json.Bind(Parse.ParseBookInfos)
                .Bind(records => Result.Try(() => records.ToArray()));
            if (parsed.IsFailure) return Result.Failure<BookInfo[]>($"part_10{i}: {parsed.Error}");
            books.AddRange(parsed.Value);
        }
        return Result.Success(books.ToArray());
    }

    public static async ValueTask<Result<TagTreeNode>> FetchTagTreeAsync(
        GetStringAsyncDelegate getStringAsyncDelegate, CancellationToken token)
    {
        var json = await getStringAsyncDelegate(
            "https://s-file-1.ykt.cbern.com.cn/zxx/ndrs/tags/tch_material_tag.json", token);
        token.ThrowIfCancellationRequested();
        return json.Bind(Parse.ParseTagTree);
    }

    public static async ValueTask<Result<Folder>> FetchFolderAsync(
        GetStringAsyncDelegate getStringAsyncDelegate, CancellationToken token)
    {
        var tree = await FetchTagTreeAsync(getStringAsyncDelegate, token);
        if (tree.IsFailure) return Result.Failure<Folder>(tree.Error);
        var books = await FetchBookInfosResultAsync(getStringAsyncDelegate, token);
        if (books.IsFailure) return Result.Failure<Folder>(books.Error);
        return Result.Try(() => Folder.MapTree(tree.Value, books.Value, []));
    }
}
