namespace TextbookSpider;

public record Folder
{
    public required string? Name { get; init; }
    public required IEnumerable<Folder> Subfolders { get; init; }
    public required IEnumerable<BookInfo> Books { get; init; }
}

public static class FolderTree
{
    public static Folder MapFolderTree(TagTreeNode tagTreeNode, IEnumerable<BookInfo> bookInfos)
    {
        var name = tagTreeNode.Name;
        var subfolders = tagTreeNode.Children.Select(child => MapFolderTree(child, bookInfos));
        var books = bookInfos.Where(book => book.Tags.Contains(tagTreeNode.Name));
        return new Folder
        {
            Name = name,
            Subfolders = subfolders,
            Books = books
        };
    }

    public static Folder SortTags(Folder folder, IEnumerable<string> rootTags)
    {
        var name = folder.Name;
        var currentTags = name is not null
            ? rootTags.Append(name)
            : rootTags;

        return folder with
        {
            Subfolders =
            from child in folder.Subfolders
            select SortTags(child, currentTags),
            
            Books =
            from book in folder.Books
            select book with { Tags = currentTags }
        };
    }
}