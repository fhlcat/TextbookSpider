using System.Text.Json.Nodes;

namespace TextbookSpider;

public record BookInfo
{
    public required string Title { get; init; }
    public required string Url { get; init; }
    public required IEnumerable<string> Tags { get; init; }
}

public record TagTreeNode
{
    public required string? Name { get; init; }
    public required IEnumerable<TagTreeNode> Children { get; init; }
}

public static class Parsing
{
    public static IEnumerable<string> ParseBookIds(string json)
    {
        return JsonNode.Parse(json)?.AsArray()
                   .Select(bookInfoNode => bookInfoNode?["id"]?.GetValue<string>())
                   .OfType<string>() // Filter out nulls
               ?? [];
    }

    public static BookInfo? TryParseBookInfo(string json)
    {
        try
        {
            var jsonNode = JsonNode.Parse(json);

            var title = jsonNode?["global_title"]?["zh-CN"]?.GetValue<string>();
            if (title is null) return null;

            var tags = jsonNode?["tag_list"]?.AsArray()
                .Select(tagNode => tagNode?.GetValue<string>())
                .OfType<string>();
            if (tags is null) return null;

            var firstPdfTiItemNode = jsonNode?["ti_items"]?.AsArray()
                .First(tiItemNode => tiItemNode?["ti_format"]?.GetValue<string>() == "pdf");
            var url = firstPdfTiItemNode?["ti_storages"]?.AsArray().First()?.GetValue<string>();
            if (url is null) return null;

            return new BookInfo
            {
                Title = title,
                Url = url,
                Tags = tags
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static TagTreeNode ParseJsonNodeTagTree(JsonNode node)
    {
        return new TagTreeNode
        {
            Name = node["tag_name"]?.GetValue<string>(),
            Children = node["hierarchies"]?["children"]?.AsArray().OfType<JsonNode>().Select(ParseJsonNodeTagTree) ?? []
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

    public static TagTreeNode? TryParseTagTree(string json)
    {
        try
        {
            var jsonNode = JsonNode.Parse(json);
            return jsonNode is null ? null : SimplifyTagTree(ParseJsonNodeTagTree(jsonNode));
        }
        catch (Exception)
        {
            return null;
        }
    }
}