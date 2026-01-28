using System.Text.Json.Nodes;
using CSharpFunctionalExtensions;

namespace TextbookSpider;

public record BookInfo
{
    public required string Id { get; init; }
    public required Maybe<string> Title { get; init; }
    public required IEnumerable<string> Tags { get; init; }
}

public record TagTreeNode
{
    public required Maybe<string> Name { get; init; }
    public required IEnumerable<TagTreeNode> Children { get; init; }
}

public static class Parse
{
    public static Result<IEnumerable<BookInfo>> ParseBookInfos(string json)
    {
        return Result.Try(() =>
        {
            var jsonNode = JsonNode.Parse(json);
            if (jsonNode is null)
            {
                throw new ArgumentException("Invalid json");
            }

            return jsonNode.AsArray()
                .Select(bookInfoNode =>
                {
                    var id = bookInfoNode?["id"]?.GetValue<string>();
                    var tags = bookInfoNode?["tag_list"]?.AsArray()
                        .Select(node => node?["tag_name"]?.GetValue<string>())
                        .OfType<string>();
                    if (id is null || tags is null)
                    {
                        return null;
                    }

                    return new BookInfo
                    {
                        Id = id,
                        Tags = tags,
                        Title = bookInfoNode?["global_title"]?["zh-CN"]?.GetValue<string>()
                    };
                })
                .OfType<BookInfo>();
        });
    }

    public static Result<string> ParseUrl(string json)
    {
        return Result.Try(() =>
        {
            var jsonNode = JsonNode.Parse(json);

            var firstPdfTiItemNode = jsonNode?["ti_items"]?.AsArray()
                .First(tiItemNode => tiItemNode?["ti_format"]?.GetValue<string>() == "pdf");
            var url = firstPdfTiItemNode?["ti_storages"]?.AsArray().First()?.GetValue<string>();

            return url ?? throw new ArgumentException("No pdf url found");
        });
    }

    private static TagTreeNode ParseOriginalTagTree(JsonNode node)
    {
        var children = node["hierarchies"]?.AsArray()
            .First()?["children"]?.AsArray()
            .OfType<JsonNode>()
            .Select(ParseOriginalTagTree);

        return new TagTreeNode
        {
            Name = node["tag_name"]?.GetValue<string>(),
            Children = children ?? []
        };
    }

    private static TagTreeNode SimplifyTagTree(TagTreeNode node)
    {
        try
        {
            var singleChild = node.Children.Single();
            return SimplifyTagTree(singleChild);
        }
        catch (InvalidOperationException)
        {
            return node;
        }
    }

    public static Result<TagTreeNode> ParseTagTree(string json)
    {
        return Result.Try(() =>
        {
            var jsonNode = JsonNode.Parse(json);
            var originalTagTree = ParseOriginalTagTree(jsonNode!);
            return SimplifyTagTree(originalTagTree);
        });
    }
}