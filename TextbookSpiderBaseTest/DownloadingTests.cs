using TextbookSpider;

namespace TextbookSpiderBaseTest;

public class DownloadingTests
{
    // A helper to generate the specific JSON structure Parsing.cs expects for book IDs
    private static string CreateBookIdJson(params string[] ids)
    {
        var items = ids.Select(id => $"{{\"id\": \"{id}\"}}");
        return $"[{string.Join(",", items)}]";
    }

    // A helper to generate the specific JSON structure Parsing.cs expects for Book Details
    private static string CreateBookDetailsJson(string title, string url, params string[] tags)
    {
        var tagsJson = string.Join(",", tags.Select(t => $"\"{t}\""));
        // Parsing logic looks for: global_title -> zh-CN, tag_list, and ti_items -> pdf -> ti_storages
        return $$"""

                         {
                             "global_title": { "zh-CN": "{{title}}" },
                             "tag_list": [{{tagsJson}}],
                             "ti_items": [
                                 {
                                     "ti_format": "pdf",
                                     "ti_storages": [ "{{url}}" ]
                                 },
                                 {
                                     "ti_format": "docx",
                                     "ti_storages": [ "http://ignore.me" ]
                                 }
                             ]
                         }
                 """;
    }

    // A helper to generate Tag Tree JSON
    // Note: Parsing.SimplifyTagTree will collapse nodes with single children.
    // To keep a root node in tests, ensure it has multiple children or zero children.
    private static string CreateTagTreeJson(string rootName, params (string name, string[] children)[] branches)
    {
        var childrenJson = (from branch in branches
            let subChildren = branch.children.Length > 0
                ? string.Join(",", branch.children.Select(c => $@"
                    {{
                        ""tag_name"": ""{c}"", 
                        ""hierarchies"": {{ ""children"": [] }}
                    }}"))
                : ""
            select $$"""

                                 {
                                     "tag_name": "{{branch.name}}",
                                     "hierarchies": { 
                                         "children": [{{subChildren}}] 
                                     }
                                 }
                     """).ToList();

        return $$"""

                         {
                             "tag_name": "{{rootName}}",
                             "hierarchies": {
                                 "children": [{{string.Join(",", childrenJson)}}]
                             }
                         }
                 """;
    }

    [Fact]
    public async Task TryDownloadFolderAsync_ReturnsMappedFolder_WhenAllDataIsValid()
    {
        // Arrange
        // 1. Setup Data
        const string expectedBookId = "book_123";
        const string expectedTitle = "Math Grade 1";
        const string expectedUrl = "http://example.com/math.pdf";
        const string expectedTag = "Mathematics";

        // 2. Define the behavior of the fake downloader
        ValueTask<string> FakeDownloader(string url, CancellationToken ct)
        {
            if (url.Contains("part_101.json"))
            {
                return new ValueTask<string>(CreateBookIdJson(expectedBookId));
            }

            if (url.Contains("part_10")) // 102, 103, etc.
            {
                return new ValueTask<string>("[]");
            }

            if (url.Contains($"details/{expectedBookId}.json"))
            {
                return new ValueTask<string>(CreateBookDetailsJson(expectedTitle, expectedUrl, expectedTag));
            }

            if (url.Contains("tch_material_tag.json"))
            {
                // Create a tree: Root -> [Mathematics, English]
                // We use 2 children to prevent SimplifyTagTree from collapsing "Root" into "Mathematics"
                return new ValueTask<string>(CreateTagTreeJson("Root", ("Mathematics", []), ("English", [])));
            }

            return new ValueTask<string>("{}");
        }

        // Act
        var result = await Downloading.TryDownloadFolderAsync(FakeDownloader, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Root", result.Name);

        // Find the "Mathematics" subfolder
        var mathFolder = result.Subfolders.FirstOrDefault(f => f.Name == "Mathematics");
        Assert.NotNull(mathFolder);

        // Check if the book was mapped to that folder
        var book = mathFolder.Books.Single();
        Assert.Equal(expectedTitle, book.Title);
        Assert.Equal(expectedUrl, book.Url);
    }

    [Fact]
    public async Task TryDownloadFolderAsync_ReturnsNull_WhenTagTreeParsingFails()
    {
        // Arrange
        ValueTask<string> FakeDownloader(string url, CancellationToken ct)
        {
            // Even if books exist...
            if (url.Contains("part_101.json")) return new ValueTask<string>(CreateBookIdJson("1"));
            if (url.Contains("details/1.json")) return new ValueTask<string>(CreateBookDetailsJson("T", "U", "Tag"));

            // ... if the tag tree is malformed or returns null from ParseTagTree
            if (url.Contains("tch_material_tag.json"))
            {
                return new ValueTask<string>("INVALID JSON OR MISSING FIELDS");
            }

            return new ValueTask<string>("[]");
        }

        // Act
        var result = await Downloading.TryDownloadFolderAsync(FakeDownloader, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task TryDownloadFolderAsync_HandlesMultipleBookPartsCorrectly()
    {
        // Arrange
        // Simulate books spread across part_101, 102, and 103
        ValueTask<string> FakeDownloader(string url, CancellationToken ct)
        {
            if (url.Contains("part_101.json")) return new ValueTask<string>(CreateBookIdJson("id_1"));
            if (url.Contains("part_102.json")) return new ValueTask<string>(CreateBookIdJson("id_2"));
            if (url.Contains("part_103.json")) return new ValueTask<string>(CreateBookIdJson("id_3"));

            // Respond to details
            if (url.Contains("details/id_1")) return new ValueTask<string>(CreateBookDetailsJson("Book 1", "url1", "TagA"));
            if (url.Contains("details/id_2")) return new ValueTask<string>(CreateBookDetailsJson("Book 2", "url2", "TagA"));
            if (url.Contains("details/id_3")) return new ValueTask<string>(CreateBookDetailsJson("Book 3", "url3", "TagB"));

            if (url.Contains("tch_material_tag.json"))
            {
                // Tree: Root -> [TagA, TagB]
                return new ValueTask<string>(CreateTagTreeJson("Root", ("TagA", []), ("TagB", [])));
            }

            return new ValueTask<string>("{}");
        }

        // Act
        var result = await Downloading.TryDownloadFolderAsync(FakeDownloader, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        var folderA = result.Subfolders.First(f => f.Name == "TagA");
        var folderB = result.Subfolders.First(f => f.Name == "TagB");

        Assert.Equal(2, folderA.Books.Count()); // Book 1 and Book 2
        Assert.Single(folderB.Books); // Book 3
    }
}