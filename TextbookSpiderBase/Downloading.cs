namespace TextbookSpider;

using FuncGetStringAsync = Func<string, CancellationToken, ValueTask<string>>;

public static class Downloading
{
    private static IAsyncEnumerable<BookInfo> DownloadBookInfos(FuncGetStringAsync getStringAsync)
    {
        var bookIdsJsonUrls =
            from i in Enumerable.Range(1, 3)
            select $"https://s-file-2.ykt.cbern.com.cn/zxx/ndrs/resources/tch_material/part_10{i}.json";
        var bookIdsJsons = bookIdsJsonUrls.ToAsyncEnumerable().Select(getStringAsync);
        var bookIds = bookIdsJsons.SelectMany(Parsing.ParseBookIds);
        var bookInfoJsonUrls =
            from bookId in bookIds
            select $"https://s-file-1.ykt.cbern.com.cn/zxx/ndrv2/resources/tch_material/details/{bookId}.json";
        var bookInfoJsons = bookInfoJsonUrls.Select(getStringAsync);
        var bookInfos = bookInfoJsons
            .Select(Parsing.TryParseBookInfo)
            .OfType<BookInfo>();
        return bookInfos;
    }

    public static async ValueTask<Folder?> TryDownloadFolderAsync(
        FuncGetStringAsync getStringAsync,
        CancellationToken ct
    )
    {
        var bookInfos = await DownloadBookInfos(getStringAsync).ToArrayAsync(ct);

        var tagTreeJson =
            await getStringAsync("https://s-file-1.ykt.cbern.com.cn/zxx/ndrs/tags/tch_material_tag.json", ct);
        var tagTree = Parsing.TryParseTagTree(tagTreeJson);
        if (tagTree is null) return null;

        var folderTree = FolderTree.MapFolderTree(tagTree, bookInfos);
        return FolderTree.SortTags(folderTree,[]);
    }
}