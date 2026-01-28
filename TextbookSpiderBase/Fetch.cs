using CSharpFunctionalExtensions;

namespace TextbookSpider;

public static class Fetch
{
    public delegate ValueTask<Result<string>> GetStringAsyncDelegate(string url, CancellationToken token);

    public static async ValueTask<Result<string>> FetchUrlAsync(
        string id,
        GetStringAsyncDelegate getStringAsyncDelegate,
        CancellationToken token
    )
    {
        var jsonResult = await getStringAsyncDelegate(
            $"https://s-file-1.ykt.cbern.com.cn/zxx/ndrv2/resources/tch_material/details/{id}.json",
            token
        );
        return jsonResult.Bind(Parse.ParseUrl);
    }

    private static IAsyncEnumerable<T> Compat<T>(this IAsyncEnumerable<Result<T>> input)
    {
        return input.Where(result => result.IsSuccess).Select(result => result.Value);
    }

    public static IAsyncEnumerable<BookInfo> FetchBookInfosAsync(GetStringAsyncDelegate getStringAsyncDelegate)
    {
        var bookInfoJsons = AsyncEnumerable
            .Range(1, 3)
            .Select((i, token) =>
                getStringAsyncDelegate(
                    $"https://s-file-2.ykt.cbern.com.cn/zxx/ndrs/resources/tch_material/part_10{i}.json",
                    token
                )
            )
            .Compat();
        return bookInfoJsons
            .Select(Parse.ParseBookInfos)
            .Compat()
            .SelectMany(bookInfos1 => bookInfos1);
    }

    public static async ValueTask<Result<TagTreeNode>> FetchTagTreeAsync(GetStringAsyncDelegate getStringAsyncDelegate,
        CancellationToken token)
    {
        var tagTreeJson =
            await getStringAsyncDelegate(
                "https://s-file-1.ykt.cbern.com.cn/zxx/ndrs/tags/tch_material_tag.json",
                token
            );
        return tagTreeJson.Bind(Parse.ParseTagTree);
    }

    public static async ValueTask<Result<Folder>> FetchFolderAsync(GetStringAsyncDelegate getStringAsyncDelegate,
        CancellationToken token)
    {
        var tagTreeResult = await FetchTagTreeAsync(getStringAsyncDelegate, token);
        if (tagTreeResult.IsFailure)
        {
            return Result.Failure<Folder>(tagTreeResult.Error);
        }

        var tagTree = tagTreeResult.Value;
        var bookInfos = await FetchBookInfosAsync(getStringAsyncDelegate).ToArrayAsync(token);
        return Folder.MapTree(tagTree, bookInfos, []);
    }
}