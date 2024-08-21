using NetP3DLib.P3D;
using NetP3DLib.P3D.Chunks;
using System.Diagnostics;
using System.Reflection;
using System.Text;

if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
{
    PrintHelp();
    Console.WriteLine("Press any key to exit . . .");
    Console.ReadKey(true);
    return;
}

HashSet<string> valueArguments = [
    "-i",
    "--input",
    "-o",
    "--output",
];
HashSet<string> options = [];
List<(string arg, string value)> valueOptions = [];
for (int i = 0; i < args.Length; i++)
{
    string arg = args[i];

    if (valueArguments.Contains(arg))
    {
        var valueIndex = ++i;
        if (valueIndex >= args.Length)
        {
            Console.WriteLine($"Not enough arguments for value option \"{arg}\".");
            return;
        }
        valueOptions.Add((arg, args[valueIndex]));
    }
    else if (arg.StartsWith('-'))
    {
        options.Add(arg);
    }
    else
    {
        Console.WriteLine($"Invalid argument: {arg}");
    }
}

List<string> inputPaths = [];
string? outputPath = null;

foreach (var (arg, value) in valueOptions)
{
    switch (arg)
    {
        case "-i":
        case "--input":
            inputPaths.Add(value);
            break;
        case "-o":
        case "--output":
            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                Console.WriteLine("You can only specify one output file.");
                Console.WriteLine("Press any key to exit . . .");
                Console.ReadKey(true);
                return;
            }
            outputPath = value;
            break;
    }
}

if (inputPaths.Count == 0)
{
    Console.WriteLine("No input files specified.");
    Console.WriteLine("Press any key to exit . . .");
    Console.ReadKey(true);
    return;
}

if (string.IsNullOrWhiteSpace(outputPath))
{
    Console.WriteLine("No output file specified.");
    Console.WriteLine("Press any key to exit . . .");
    Console.ReadKey(true);
    return;
}

bool force = false;
bool noHistory = false;
bool sort = false;
bool sortAlphabetical = false;
bool sortIncludeSections = false;
bool dedupe = false;
bool compress = false;
bool pause = false;
foreach (var option in options)
{
    switch (option)
    {
        case "-f":
        case "--force":
            force = true;
            break;
        case "-nh":
        case "--no_history":
            noHistory = true;
            break;
        case "-s":
        case "--sort":
            sort = true;
            break;
        case "-sa":
        case "--sort_alphabetical":
            sortAlphabetical = true;
            break;
        case "-sis":
        case "--sort_include_sections":
            sortIncludeSections = true;
            break;
        case "-d":
        case "--dedupe":
            dedupe = true;
            break;
        case "-c":
        case "--compress":
            compress = true;
            break;
        case "-p":
        case "--pause":
            pause = true;
            break;
        default:
            Console.WriteLine($"Unknown/unused option: {option}");
            break;
    }
}

if ((sortAlphabetical || sortIncludeSections) && !sort)
{
    Console.WriteLine("Sort Alphabetical and/or Sort Include Sections were specified without enabling Sorting. These will be ignored.");
    Console.WriteLine("Press any key to continue . . .");
    Console.ReadKey(true);
}

for (int i = 0; i < inputPaths.Count; i++)
{
    var inputFileInfo = new FileInfo(inputPaths[i]);
    if (!inputFileInfo.Exists)
    {
        Console.WriteLine($"Could not find input path: {inputPaths[i]}");
        Console.WriteLine("Press any key to exit . . .");
        Console.ReadKey(true);
        return;
    }
    inputPaths[i] = inputFileInfo.FullName;
}

var outputFileInfo = new FileInfo(outputPath);
if (!IsValidOutputPath(outputFileInfo.FullName))
{
    Console.WriteLine("Press any key to exit . . .");
    Console.ReadKey(true);
    return;
}
if (outputFileInfo.Exists && outputFileInfo.IsReadOnly)
{
    Console.WriteLine($"Output path \"{outputFileInfo.FullName}\" is read only.");
    Console.WriteLine("Press any key to exit . . .");
    Console.ReadKey(true);
    return;
}
if (outputFileInfo.Exists && !force)
{
    string? response;
    do
    {
        Console.WriteLine($"Output file \"{outputFileInfo.FullName}\" already exists.");
        Console.WriteLine("Do you want to overwrite? [Yes/No]");
        response = Console.ReadLine();
        if (response != null && response.Equals("no", StringComparison.OrdinalIgnoreCase))
            return;
    } while (response?.ToLower() != "yes");
}
outputPath = outputFileInfo.FullName;

if (inputPaths.Any(x => !Path.GetExtension(x).Equals(".p3d", StringComparison.OrdinalIgnoreCase)))
{
    Console.WriteLine("Input must be a P3D file.");
    Console.WriteLine("Press any key to exit . . .");
    Console.ReadKey(true);
    return;
}

if (!Path.GetExtension(outputPath).Equals(".p3d", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("Output must be a P3D file.");
    Console.WriteLine("Press any key to exit . . .");
    Console.ReadKey(true);
    return;
}

Console.WriteLine($"Input Paths: {string.Join("; ", inputPaths)}.");
Console.WriteLine($"Output Path: {outputPath}.");
Console.WriteLine($"Force: {force}.");
Console.WriteLine($"No History: {noHistory}.");
Console.WriteLine($"Sort: {sort}.");
if (sort)
{
    Console.WriteLine($"\tAlphabetical: {sortAlphabetical}.");
    Console.WriteLine($"\tInclude Section Headers: {sortIncludeSections}.");
}
Console.WriteLine($"Dedupe: {dedupe}.");
Console.WriteLine($"Compress: {compress}.");
Console.WriteLine($"Pause: {pause}.");

try
{
    var sw = Stopwatch.StartNew();
    P3DFile outputFile = new();

    foreach (string path in inputPaths)
    {
        Console.WriteLine($"Reading \"{path}\" {(dedupe ? "with" : "without")} deduplication..");
        P3DFile inputFile = new(path);

        if (dedupe)
        {
            foreach (var chunk in inputFile.Chunks)
                if (!outputFile.Chunks.Contains(chunk))
                    outputFile.Chunks.Add(chunk);
        }
        else
        {
            outputFile.Chunks.AddRange(inputFile.Chunks);
        }
    }

    if (sort)
    {
        Console.WriteLine("Sorting chunks...");
        outputFile.SortChunks(sortIncludeSections, sortAlphabetical, false);
    }

    if (!noHistory)
    {
        Console.WriteLine("Adding history chunk...");
        List<string> lines = [
            $"Generated with P3DTool v{Assembly.GetExecutingAssembly().GetName().Version}."
        ];
        StringBuilder commandLine = new(Path.GetFileName(Environment.ProcessPath));
        foreach (var arg in args)
        {
            var argStr = arg.Contains(' ') ? $" \"{arg}\"" : $" {arg}";
            if (commandLine.Length + argStr.Length > 255)
            {
                lines.Add(commandLine.ToString());
                commandLine.Clear();
            }
            commandLine.Append(argStr);
        }
        lines.Add(commandLine.ToString());
        lines.Add($"Run at {DateTime.Now:R}.");
        outputFile.Chunks.Insert(0, new HistoryChunk(lines));
    }

    if (compress)
    {
        Console.WriteLine($"Compressing file to \"{outputPath}\"...");
        outputFile.Compress(outputPath, false);
    }
    else
    {
        Console.WriteLine($"Writing file to \"{outputPath}\"...");
        outputFile.Write(outputPath);
    }

    sw.Stop();
    Console.WriteLine($"Process finished in {sw.Elapsed:hh\\:mm\\:ss\\.fff}.");

    if (pause)
    {
        Console.WriteLine("Press any key to exit . . .");
        Console.ReadKey(true);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"There was an error generating mipmaps: {ex}");
    Console.WriteLine("Press any key to exit . . .");
    Console.ReadKey(true);
}

static void PrintHelp()
{
    Console.WriteLine("Usage: P3DTool [options]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  -f   | --force                    Force overwrite the output file.");
    Console.WriteLine("  -nh  | --no_history               Don't add history chunk.");
    Console.WriteLine("  -s   | --sort                     Sort chunks.");
    Console.WriteLine("  -sa  | --sort_alphabetical        When sorting, the chunks will be sorted alphabetically.");
    Console.WriteLine("  -sis | --sort_include_sections    When sorting, a history chunk will be add to the start if each section.");
    Console.WriteLine("  -d   | --dedupe                   When adding a file, duplicate chunks will be omitted.");
    Console.WriteLine("  -c   | --compress                 Compresses the output Pure3D file with LZR compression.");
    Console.WriteLine("  -p   | --pause                    Pauses at the end of execution.");
    Console.WriteLine();
    Console.WriteLine("Example:");
    Console.WriteLine("  P3DTool -s -i C:\\input\\file1.p3d -i C:\\input\\file2.p3d -o C:\\output\\file.p3d");
    Console.WriteLine("  P3DTool -c --no_history -i C:\\input\\file.p3d -o C:\\output\\file.p3d");
    Console.WriteLine();
}

static bool IsValidOutputPath(string outputPath)
{
    if (outputPath.IndexOfAny(Path.GetInvalidPathChars()) != -1)
    {
        Console.WriteLine($"Output path \"{outputPath}\" contains invalid characters.");
        return false;
    }

    var directory = Path.GetDirectoryName(outputPath);
    if (!Directory.Exists(directory))
    {
        Console.WriteLine($"Output directory \"{(string.IsNullOrWhiteSpace(directory), Environment.CurrentDirectory, directory)}\" doesn't exist.");
        return false;
    }

    try
    {
        var path = Path.GetRandomFileName();
        if (!string.IsNullOrWhiteSpace(directory))
            path = Path.Combine(directory, path);
        using FileStream fs = File.Create(path, 1, FileOptions.DeleteOnClose);
    }
    catch
    {
        return false;
    }

    return true;
}