using System.Text.Json;
using TextbookSpider;

namespace TextbookSpiderBaseTest;

public class ParsingTests
{
    [Fact]
    public void ParseBookIds_ValidJson_ReturnsBookIds()
    {
        const string json = """
                            [
                                {"id": "book1"},
                                {"id": "book2"},
                                {"id": "book3"}
                            ]
                            """;
        var result = Parsing.ParseBookIds(json);
        Assert.Equal(["book1", "book2", "book3"], result);
    }

    [Fact]
    public void ParseBookIds_EmptyArray_ReturnsEmpty()
    {
        const string json = "[]";

        var result = Parsing.ParseBookIds(json);

        Assert.Empty(result);
    }

    [Fact]
    public void ParseBookIds_MixedNullAndValidIds_ReturnsOnlyValidIds()
    {
        const string json = """
                            [
                                {"id": "book1"},
                                {"id": null},
                                {"id": "book2"},
                                {"other": "field"}
                            ]
                            """;

        var result = Parsing.ParseBookIds(json);

        Assert.Equal(["book1", "book2"], result);
    }

    [Fact]
    public void ParseBookIds_InvalidJson_ReturnsEmpty()
    {
        const string json = "invalid json";

        Assert.ThrowsAny<JsonException>(() => Parsing.ParseBookIds(json).ToList());
    }

    [Fact]
    public void ParseBookIds_NullJson_ReturnsEmpty()
    {
        const string json = "null";

        var result = Parsing.ParseBookIds(json);

        Assert.Empty(result);
    }


    [Fact]
    public void TryParseBookInfo_ValidJson_ReturnsBookInfo()
    {
        const string json = """
                            {
                                "global_title": {
                                    "zh-CN": "Test Book Title"
                                },
                                "tag_list": ["tag1", "tag2", "tag3"],
                                "ti_items": [
                                    {
                                        "ti_format": "pdf",
                                        "ti_storages": ["https://example.com/book.pdf"]
                                    }
                                ]
                            }
                            """;

        var result = Parsing.TryParseBookInfo(json);

        Assert.NotNull(result);
        Assert.Equal("Test Book Title", result.Title);
        Assert.Equal("https://example.com/book.pdf", result.Url);
        Assert.Equal(["tag1", "tag2", "tag3"], result.Tags);
    }

    [Fact]
    public void TryParseBookInfo_MissingTitle_ReturnsNull()
    {
        const string json = """
                            {
                                "tag_list": ["tag1"],
                                "ti_items": [
                                    {
                                        "ti_format": "pdf",
                                        "ti_storages": ["https://example.com/book.pdf"]
                                    }
                                ]
                            }
                            """;

        var result = Parsing.TryParseBookInfo(json);

        Assert.Null(result);
    }

    [Fact]
    public void TryParseBookInfo_MissingTags_ReturnsNull()
    {
        const string json = """
                            {
                                "global_title": {
                                    "zh-CN": "Test Book Title"
                                },
                                "ti_items": [
                                    {
                                        "ti_format": "pdf",
                                        "ti_storages": ["https://example.com/book.pdf"]
                                    }
                                ]
                            }
                            """;

        var result = Parsing.TryParseBookInfo(json);

        Assert.Null(result);
    }

    [Fact]
    public void TryParseBookInfo_MissingPdfUrl_ReturnsNull()
    {
        const string json = """
                            {
                                "global_title": {
                                    "zh-CN": "Test Book Title"
                                },
                                "tag_list": ["tag1"]
                            }
                            """;

        var result = Parsing.TryParseBookInfo(json);

        Assert.Null(result);
    }

    [Fact]
    public void TryParseBookInfo_NoPdfFormat_ThrowsException()
    {
        const string json = """
                            {
                                "global_title": {
                                    "zh-CN": "Test Book Title"
                                },
                                "tag_list": ["tag1"],
                                "ti_items": [
                                    {
                                        "ti_format": "epub",
                                        "ti_storages": ["https://example.com/book.epub"]
                                    }
                                ]
                            }
                            """;

        Assert.Throws<InvalidOperationException>(() => Parsing.TryParseBookInfo(json));
    }

    [Fact]
    public void TryParseBookInfo_EmptyTiItems_ThrowsException()
    {
        const string json = """
                            {
                                "global_title": {
                                    "zh-CN": "Test Book Title"
                                },
                                "tag_list": ["tag1"],
                                "ti_items": []
                            }
                            """;

        Assert.Throws<InvalidOperationException>(() => Parsing.TryParseBookInfo(json));
    }

    [Fact]
    public void TryParseBookInfo_MultiplePdfFormats_ReturnsFirstPdf()
    {
        const string json = """
                            {
                                "global_title": {
                                    "zh-CN": "Test Book Title"
                                },
                                "tag_list": ["tag1"],
                                "ti_items": [
                                    {
                                        "ti_format": "epub",
                                        "ti_storages": ["https://example.com/book.epub"]
                                    },
                                    {
                                        "ti_format": "pdf",
                                        "ti_storages": ["https://example.com/first.pdf"]
                                    },
                                    {
                                        "ti_format": "pdf",
                                        "ti_storages": ["https://example.com/second.pdf"]
                                    }
                                ]
                            }
                            """;

        var result = Parsing.TryParseBookInfo(json);

        Assert.NotNull(result);
        Assert.Equal("https://example.com/first.pdf", result.Url);
    }

    [Fact]
    public void TryParseBookInfo_EmptyTagList_ReturnsBookInfoWithEmptyTags()
    {
        const string json = """
                            {
                                "global_title": {
                                    "zh-CN": "Test Book Title"
                                },
                                "tag_list": [],
                                "ti_items": [
                                    {
                                        "ti_format": "pdf",
                                        "ti_storages": ["https://example.com/book.pdf"]
                                    }
                                ]
                            }
                            """;

        var result = Parsing.TryParseBookInfo(json);

        Assert.NotNull(result);
        Assert.Empty(result.Tags);
    }


    [Fact]
    public void ParseTagTree_ValidSimpleTree_ReturnsTagTreeNode()
    {
        const string json = """
                            {
                                "tag_name": "root",
                                "hierarchies": {
                                    "children": []
                                }
                            }
                            """;

        var result = Parsing.TryParseTagTree(json);

        Assert.NotNull(result);
        Assert.Equal("root", result.Name);
        Assert.Empty(result.Children);
    }

    [Fact]
    public void ParseTagTree_ValidNestedTree_ReturnsTagTreeNode()
    {
        const string json = """
                            {
                                "tag_name": "root",
                                "hierarchies": {
                                    "children": [
                                        {
                                            "tag_name": "child1",
                                            "hierarchies": {
                                                "children": []
                                            }
                                        },
                                        {
                                            "tag_name": "child2",
                                            "hierarchies": {
                                                "children": []
                                            }
                                        }
                                    ]
                                }
                            }
                            """;

        var result = Parsing.TryParseTagTree(json);

        Assert.NotNull(result);
        Assert.Equal("root", result.Name);
        Assert.Equal(2, result.Children.Count());
        Assert.Contains(result.Children, c => c.Name == "child1");
        Assert.Contains(result.Children, c => c.Name == "child2");
    }

    [Fact]
    public void ParseTagTree_SimplifiesChainWithSingleChild()
    {
        const string json = """
                            {
                                "tag_name": "root",
                                "hierarchies": {
                                    "children": [
                                        {
                                            "tag_name": "intermediate",
                                            "hierarchies": {
                                                "children": [
                                                    {
                                                        "tag_name": "leaf",
                                                        "hierarchies": {
                                                            "children": []
                                                        }
                                                    }
                                                ]
                                            }
                                        }
                                    ]
                                }
                            }
                            """;

        var result = Parsing.TryParseTagTree(json);

        Assert.NotNull(result);
        Assert.Equal("leaf", result.Name);
        Assert.Empty(result.Children);
    }

    [Fact]
    public void ParseTagTree_DoesNotSimplifyWithMultipleChildren()
    {
        const string json = """
                            {
                                "tag_name": "root",
                                "hierarchies": {
                                    "children": [
                                        {
                                            "tag_name": "child1",
                                            "hierarchies": {
                                                "children": [
                                                    {
                                                        "tag_name": "grandchild1",
                                                        "hierarchies": {
                                                            "children": []
                                                        }
                                                    }
                                                ]
                                            }
                                        },
                                        {
                                            "tag_name": "child2",
                                            "hierarchies": {
                                                "children": []
                                            }
                                        }
                                    ]
                                }
                            }
                            """;

        var result = Parsing.TryParseTagTree(json);

        Assert.NotNull(result);
        Assert.Equal("root", result.Name);
        Assert.Equal(2, result.Children.Count());
    }

    [Fact]
    public void ParseTagTree_NullJson_ReturnsNull()
    {
        const string json = "null";

        var result = Parsing.TryParseTagTree(json);

        Assert.Null(result);
    }

    [Fact]
    public void ParseTagTree_MissingTagName_ReturnsNodeWithNullName()
    {
        const string json = """
                            {
                                "hierarchies": {
                                    "children": []
                                }
                            }
                            """;

        var result = Parsing.TryParseTagTree(json);

        Assert.NotNull(result);
        Assert.Null(result.Name);
        Assert.Empty(result.Children);
    }

    [Fact]
    public void ParseTagTree_MissingChildren_ReturnsNodeWithEmptyChildren()
    {
        var json = """
                   {
                       "tag_name": "root"
                   }
                   """;

        var result = Parsing.TryParseTagTree(json);

        Assert.NotNull(result);
        Assert.Equal("root", result.Name);
        Assert.Empty(result.Children);
    }

    [Fact]
    public void ParseTagTree_ComplexNestedStructure_ParsesCorrectly()
    {
        const string json = """
                            {
                                "tag_name": "root",
                                "hierarchies": {
                                    "children": [
                                        {
                                            "tag_name": "branch1",
                                            "hierarchies": {
                                                "children": [
                                                    {
                                                        "tag_name": "leaf1",
                                                        "hierarchies": {
                                                            "children": []
                                                        }
                                                    },
                                                    {
                                                        "tag_name": "leaf2",
                                                        "hierarchies": {
                                                            "children": []
                                                        }
                                                    }
                                                ]
                                            }
                                        }
                                    ]
                                }
                            }
                            """;

        var result = Parsing.TryParseTagTree(json);

        Assert.NotNull(result);
        Assert.Equal("branch1", result.Name);
        Assert.Equal(2, result.Children.Count());
        Assert.Contains(result.Children, c => c.Name == "leaf1");
        Assert.Contains(result.Children, c => c.Name == "leaf2");
    }
}