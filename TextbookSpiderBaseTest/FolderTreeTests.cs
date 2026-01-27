using TextbookSpider;

namespace TextbookSpiderBaseTest;

public class FolderTreeTests
{
    [Fact]
    public void MapFolderTree_WithSingleNodeNoBooks_CreatesEmptyFolder()
    {
        var tagTreeNode = new TagTreeNode
        {
            Name = "Root",
            Children = []
        };

        var result = FolderTree.MapFolderTree(tagTreeNode, []);

        Assert.Equal("Root", result.Name);
        Assert.Empty(result.Subfolders);
        Assert.Empty(result.Books);
    }

    [Fact]
    public void MapFolderTree_WithMatchingBook_IncludesBookInFolder()
    {
        var tagTreeNode = new TagTreeNode
        {
            Name = "Science",
            Children = []
        };
        var bookInfos = new List<BookInfo>
        {
            new()
            {
                Title = "Physics 101",
                Url = "http://example.com/physics",
                Tags = ["Science", "Physics"]
            }
        };

        var result = FolderTree.MapFolderTree(tagTreeNode, bookInfos);

        Assert.Single(result.Books);
        Assert.Equal("Physics 101", result.Books.First().Title);
    }

    [Fact]
    public void MapFolderTree_WithNonMatchingBook_ExcludesBookFromFolder()
    {
        var tagTreeNode = new TagTreeNode
        {
            Name = "Science",
            Children = []
        };
        var bookInfos = new List<BookInfo>
        {
            new()
            {
                Title = "History 101",
                Url = "http://example.com/history",
                Tags = ["History", "Ancient"]
            }
        };

        var result = FolderTree.MapFolderTree(tagTreeNode, bookInfos);

        Assert.Empty(result.Books);
    }

    [Fact]
    public void MapFolderTree_WithMultipleBooks_FiltersCorrectly()
    {
        var tagTreeNode = new TagTreeNode
        {
            Name = "Math",
            Children = []
        };
        var bookInfos = new List<BookInfo>
        {
            new()
            {
                Title = "Calculus",
                Url = "http://example.com/calculus",
                Tags = ["Math", "Advanced"]
            },
            new()
            {
                Title = "Algebra",
                Url = "http://example.com/algebra",
                Tags = ["Math", "Beginner"]
            },
            new()
            {
                Title = "Physics",
                Url = "http://example.com/physics",
                Tags = ["Science"]
            }
        };

        var result = FolderTree.MapFolderTree(tagTreeNode, bookInfos);

        Assert.Equal(2, result.Books.Count());
        Assert.Contains(result.Books, b => b.Title == "Calculus");
        Assert.Contains(result.Books, b => b.Title == "Algebra");
        Assert.DoesNotContain(result.Books, b => b.Title == "Physics");
    }

    [Fact]
    public void MapFolderTree_WithNestedStructure_CreatesSubfolders()
    {
        var childNode = new TagTreeNode
        {
            Name = "Advanced",
            Children = []
        };
        var tagTreeNode = new TagTreeNode
        {
            Name = "Math",
            Children = [childNode]
        };

        var result = FolderTree.MapFolderTree(tagTreeNode, []);

        Assert.Single(result.Subfolders);
        Assert.Equal("Advanced", result.Subfolders.First().Name);
    }

    [Fact]
    public void MapFolderTree_WithMultipleSubfolders_CreatesAllSubfolders()
    {
        var tagTreeNode = new TagTreeNode
        {
            Name = "Science",
            Children =
            [
                new TagTreeNode { Name = "Physics", Children = [] },
                new TagTreeNode { Name = "Chemistry", Children = [] },
                new TagTreeNode { Name = "Biology", Children = [] }
            ]
        };

        var result = FolderTree.MapFolderTree(tagTreeNode, []);

        Assert.Equal(3, result.Subfolders.Count());
        Assert.Contains(result.Subfolders, f => f.Name == "Physics");
        Assert.Contains(result.Subfolders, f => f.Name == "Chemistry");
        Assert.Contains(result.Subfolders, f => f.Name == "Biology");
    }

    [Fact]
    public void MapFolderTree_WithDeepNesting_CreatesFullHierarchy()
    {
        var tagTreeNode = new TagTreeNode
        {
            Name = "Root",
            Children =
            [
                new TagTreeNode
                {
                    Name = "Level1",
                    Children =
                    [
                        new TagTreeNode
                        {
                            Name = "Level2",
                            Children = []
                        }
                    ]
                }
            ]
        };

        var result = FolderTree.MapFolderTree(tagTreeNode, []);

        Assert.Single(result.Subfolders);
        var level1 = result.Subfolders.First();
        Assert.Equal("Level1", level1.Name);
        Assert.Single(level1.Subfolders);
        var level2 = level1.Subfolders.First();
        Assert.Equal("Level2", level2.Name);
    }

    [Fact]
    public void MapFolderTree_WithBooksAtMultipleLevels_DistributesBooksCorrectly()
    {
        var tagTreeNode = new TagTreeNode
        {
            Name = "Science",
            Children =
            [
                new TagTreeNode { Name = "Physics", Children = [] }
            ]
        };
        var bookInfos = new List<BookInfo>
        {
            new()
            {
                Title = "General Science",
                Url = "http://example.com/science",
                Tags = ["Science"]
            },
            new()
            {
                Title = "Physics 101",
                Url = "http://example.com/physics",
                Tags = ["Physics", "Science"]
            }
        };

        var result = FolderTree.MapFolderTree(tagTreeNode, bookInfos);

        Assert.Equal(2, result.Books.Count());
        var physicsFolder = result.Subfolders.First();
        Assert.Single(physicsFolder.Books);
        Assert.Equal("Physics 101", physicsFolder.Books.First().Title);
    }

    [Fact]
    public void MapFolderTree_WithNullName_PreservesNullName()
    {
        var tagTreeNode = new TagTreeNode
        {
            Name = null,
            Children = []
        };

        var result = FolderTree.MapFolderTree(tagTreeNode, []);

        Assert.Null(result.Name);
    }

    [Fact]
    public void MapFolderTree_WithBookContainingMultipleTags_MatchesCorrectly()
    {
        var tagTreeNode = new TagTreeNode
        {
            Name = "Fiction",
            Children = []
        };
        var bookInfos = new List<BookInfo>
        {
            new()
            {
                Title = "Fantasy Novel",
                Url = "http://example.com/fantasy",
                Tags = ["Fiction", "Fantasy", "Adventure", "Young Adult"]
            }
        };

        var result = FolderTree.MapFolderTree(tagTreeNode, bookInfos);

        Assert.Single(result.Books);
        Assert.Equal("Fantasy Novel", result.Books.First().Title);
    }

    [Fact]
    public void MapFolderTree_WithComplexScenario_WorksCorrectly()
    {
        var tagTreeNode = new TagTreeNode
        {
            Name = "Library",
            Children =
            [
                new TagTreeNode
                {
                    Name = "Fiction",
                    Children =
                    [
                        new TagTreeNode { Name = "SciFi", Children = [] },
                        new TagTreeNode { Name = "Fantasy", Children = [] }
                    ]
                },
                new TagTreeNode { Name = "NonFiction", Children = [] }
            ]
        };
        var bookInfos = new List<BookInfo>
        {
            new()
            {
                Title = "Dune",
                Url = "http://example.com/dune",
                Tags = ["SciFi", "Fiction", "Library"]
            },
            new()
            {
                Title = "LOTR",
                Url = "http://example.com/lotr",
                Tags = ["Fantasy", "Fiction"]
            },
            new()
            {
                Title = "History Book",
                Url = "http://example.com/history",
                Tags = ["NonFiction", "History"]
            }
        };

        var result = FolderTree.MapFolderTree(tagTreeNode, bookInfos);

        Assert.Equal("Library", result.Name);
        Assert.Single(result.Books);
        var fictionFolder = result.Subfolders.First(f => f.Name == "Fiction");
        Assert.Equal(2, fictionFolder.Books.Count());
        var sciFiFolder = fictionFolder.Subfolders.First(f => f.Name == "SciFi");
        Assert.Single(sciFiFolder.Books);
        Assert.Equal("Dune", sciFiFolder.Books.First().Title);

        var fantasyFolder = fictionFolder.Subfolders.First(f => f.Name == "Fantasy");
        Assert.Single(fantasyFolder.Books);
        Assert.Equal("LOTR", fantasyFolder.Books.First().Title);

        var nonFictionFolder = result.Subfolders.First(f => f.Name == "NonFiction");
        Assert.Single(nonFictionFolder.Books);
        Assert.Equal("History Book", nonFictionFolder.Books.First().Title);
    }

    [Fact]
    public void MapFolderTree_WithEmptyBookList_CreatesEmptyFolders()
    {
        var tagTreeNode = new TagTreeNode
        {
            Name = "Category",
            Children =
            [
                new TagTreeNode { Name = "Subcategory", Children = [] }
            ]
        };

        var result = FolderTree.MapFolderTree(tagTreeNode, []);

        Assert.Empty(result.Books);
        Assert.Single(result.Subfolders);
        Assert.Empty(result.Subfolders.First().Books);
    }
}