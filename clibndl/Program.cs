
using System.CommandLine;

var languageOption = new Option<string>(
    "--language",
    "שפת הקוד (או all לכל הקבצים)"
);
languageOption.AddAlias("-l");
languageOption.IsRequired = true;

var outputOption = new Option<string>(
    "--output",
    "שם קובץ ה-bundle שיווצר (אפשר גם עם ניתוב מלא)"
);
outputOption.AddAlias("-o");
outputOption.IsRequired = true;

var noteOption = new Option<bool>(
    "--note",
    "האם להוסיף הערה עם שם הקובץ המקורי"
);
noteOption.AddAlias("-n");

var removeEmptyLinesOption = new Option<bool>(
    "--remove-empty-lines",
    "האם למחוק שורות ריקות"
);
removeEmptyLinesOption.AddAlias("-r");

var authorOption = new Option<string>(
    "--author",
    "שם היוצר שירשם בראש הקובץ"
);
authorOption.AddAlias("-a");


// ---------- אופציה חדשה: sort ----------
var sortOption = new Option<string>(
    "--sort",
    "סדר הקבצים בקובץ bundle: name (ברירת מחדל) או type"
);
sortOption.AddAlias("-s");
sortOption.SetDefaultValue("name"); // ברירת מחדל

var bundleCommand = new Command("bundle", "מאחד קבצי קוד לקובץ אחד");

bundleCommand.AddOption(languageOption);
bundleCommand.AddOption(outputOption);
bundleCommand.AddOption(noteOption);
bundleCommand.AddOption(removeEmptyLinesOption);
bundleCommand.AddOption(authorOption);


bundleCommand.SetHandler((
    string language,
    string output,
    bool note,
    bool removeEmptyLines,
    string author,
    string sort) =>
{
    string[] excludedFolders = { "bin", "obj", "debug" };
    var currentDirectory = Directory.GetCurrentDirectory();

    var allFiles = Directory.GetFiles(currentDirectory, "*.*", SearchOption.AllDirectories)
        .Where(file =>
        {
            string relativePath = Path.GetRelativePath(currentDirectory, file);
            return !excludedFolders.Any(folder =>
                relativePath.Split(Path.DirectorySeparatorChar).Contains(folder));
        });

    IEnumerable<string> matchingFiles;

    if (language.ToLower() == "all")
    {
        string[] codeExtensions = { ".cs", ".js", ".ts", ".py", ".java", ".cpp", ".c", ".html", ".css" };
        matchingFiles = allFiles.Where(file => codeExtensions.Contains(Path.GetExtension(file)));
    }
    else
    {
        string extension = "." + language.ToLower();
        matchingFiles = allFiles.Where(file => Path.GetExtension(file).ToLower() == extension);
    }

    if (sort.ToLower() == "type")
    {
        matchingFiles = matchingFiles
            .OrderBy(f => Path.GetExtension(f))
            .ThenBy(f => Path.GetFileName(f));
    }
    else
    {
        matchingFiles = matchingFiles
            .OrderBy(f => Path.GetFileName(f));
    }
    try
    {
        using var writer = new StreamWriter(output);

        if (!string.IsNullOrWhiteSpace(author))
        {
            writer.WriteLine($"// Author: {author}");
            writer.WriteLine();
        }

        foreach (var file in matchingFiles)
        {
            if (note)
            {
                var relative = Path.GetRelativePath(currentDirectory, file);
                writer.WriteLine($"// File: {relative}");
            }

            var lines = File.ReadAllLines(file);

            foreach (var line in lines)
            {
                if (removeEmptyLines && string.IsNullOrWhiteSpace(line))
                    continue;

                writer.WriteLine(line);
            }

            writer.WriteLine(); // רווח בין קבצים
        }

        Console.WriteLine($"✅ נוצר קובץ bundle: {output}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ שגיאה ביצירת הקובץ: {ex.Message}");
    }

}, languageOption, outputOption, noteOption, removeEmptyLinesOption, authorOption, sortOption);
// הגדרת RootCommand והפעלה
var rootCommand = new RootCommand("CLI לאריזת קוד");
rootCommand.AddCommand(bundleCommand);

var createRspCommand = new Command("create-rsp", "יוצר קובץ תגובה עם פקודת bundle מוכנה");

createRspCommand.SetHandler(async () =>
{

    Console.Write("📌 language (cs, py, js... או all): ");
    string language = Console.ReadLine() ?? "";

    Console.Write("📁 output (שם קובץ): ");
    string output = Console.ReadLine() ?? "";

    Console.Write("📌 note (y/n): ");
    bool note = Console.ReadLine()?.Trim().ToLower() == "y";

    Console.Write("🧹 remove-empty-lines (y/n): ");
    bool removeEmpty = Console.ReadLine()?.Trim().ToLower() == "y";

    Console.Write("✍️ author (או ריק): ");
    string author = Console.ReadLine() ?? "";

    // בניית שורת פקודה
    List<string> parts = new()
    {
        "bundle",
        $"-l {language}",
        $"-o \"{output}\""
    };

    if (note) parts.Add("-n");
    if (removeEmpty) parts.Add("-r");
    if (!string.IsNullOrWhiteSpace(author)) parts.Add($"-a \"{author}\"");

    string fullCommand = string.Join(" ", parts);

    Console.Write("📄 שם קובץ ה-rsp לשמור אליו (למשל mycmd.rsp): ");
    string rspFile = Console.ReadLine() ?? "command.rsp";

    try
    {
        await File.WriteAllTextAsync(rspFile, fullCommand);
        Console.WriteLine($"✅ נשמר בהצלחה לקובץ: {rspFile}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ שגיאה בשמירה: {ex.Message}");
    }
});
rootCommand.AddCommand(createRspCommand);

await rootCommand.InvokeAsync(args);



