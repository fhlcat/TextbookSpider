using CSharpFunctionalExtensions;

namespace TextbookSpider;

public record Book
{
    public required string Title { get; init; }
    public required string Id { get; init; }
}

public record Folder
{
    public required Maybe<string> Name { get; init; }
    public required IEnumerable<Folder> Subfolders { get; init; }
    public required IEnumerable<Book> Books { get; init; }

    public static Folder MapTree(TagTreeNode node, BookInfo[] bookInfos, IEnumerable<string> parentNodes)
    {
        var currentNodes = node.Name.HasValue ? parentNodes.Append(node.Name.Value) : parentNodes;
        var currentNodesArray = currentNodes.ToArray();
        var subfolders =
            from child in node.Children
            select MapTree(child, bookInfos, currentNodesArray);

        if (node.Name.HasNoValue)
        {
            return new Folder
            {
                Name = Maybe<string>.None,
                Subfolders = subfolders,
                Books = []
            };
        }

        var books =
            from bookInfo in bookInfos
            where currentNodesArray.All(bookInfo.Tags.Contains)
            where bookInfo.Title.HasValue
            select new Book
            {
                Title = bookInfo.Title.Value,
                Id = bookInfo.Id
            };

        return new Folder
        {
            Name = node.Name,
            Subfolders = subfolders,
            Books = books
        };
    }
}