using System.Text.RegularExpressions;
using TextbookSpider;

namespace TextbookSpiderCli;

public static partial class Tools
{
    public static string AskForString(string prompt, Func<string, bool> validator)
    {
        Console.Write(prompt);
        var input = Console.ReadLine();
        if (input is null || !validator(input))
        {
            return AskForString(prompt, validator);
        }

        return input;
    }

    [GeneratedRegex("^([0-9]+)$")]
    private static partial Regex SelectFolderRegex();

    [GeneratedRegex("^([0-9]+)~$")]
    private static partial Regex EnterFolderRegex();

    public static Folder ChooseFolder(Folder folder)
    {
        var children = folder.Subfolders.ToArray();
        var i = 0;
        Console.WriteLine(new string('-', 10));
        foreach (var child in children)
        {
            Console.WriteLine($"[{i}]{child.Name}" + (child.Subfolders.Any() ? "~" : string.Empty));
            i++;
        }

        Console.Write("Choose a folder: ");
        var input = Console.ReadLine()!;
        var selectFolderMatch = SelectFolderRegex().Match(input);
        if (selectFolderMatch.Success)
        {
            return children[int.Parse(selectFolderMatch.Groups[1].Value)];
        }

        var enterFolderMatch = EnterFolderRegex().Match(input);
        if (enterFolderMatch.Success)
        {
            var child = children[int.Parse(enterFolderMatch.Groups[1].Value)];
            if (child.Subfolders.Any())
            {
                return ChooseFolder(child);
            }

            Console.WriteLine("No more children");
            return ChooseFolder(folder);
        }

        Console.WriteLine("Invalid input");
        return ChooseFolder(folder);
    }
}