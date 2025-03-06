using System.Text;
using System.Text.RegularExpressions;

namespace Penumbra;
internal static class GenerateREADME // need to tidy up some and redo the config part since that was pretty... specific >_> to Bloodcraft setup but command generation works without many changes generically
{
    static string CommandsPath { get; set; }
    static string ReadMePath { get; set; }

    // Regex patterns for parsing commands
    static readonly Regex _commandGroupRegex = new(@"\[CommandGroup\(name:\s*""(?<group>[^""]+)"",\s*""(?<short>[^""]+)""\)\]"); // the first and second one here should really just be one but this works and tired so leaving >_>
    static readonly Regex _commandGroupAndShortRegex = new(@"\[CommandGroup\(name:\s*""(?<group>[^""]+)""(?:\s*,\s*short:\s*""(?<short>[^""]+)"")?\)\]");
    static readonly Regex _commandAttributeRegex = new(@"\[Command\(name:\s*""(?<name>[^""]+)""(?:,\s*shortHand:\s*""(?<shortHand>[^""]+)"")?(?:,\s*adminOnly:\s*(?<adminOnly>\w+))?(?:,\s*usage:\s*""(?<usage>[^""]+)"")?(?:,\s*description:\s*""(?<description>[^""]+)"")?\)\]");

    // Constants for README sections
    const string COMMANDS_HEADER = "## Commands";
    const string CONFIG_HEADER = "## Configuration";

    // We'll store all commands in this structure before outputting them
    static readonly Dictionary<(string groupName, string groupShort), List<(string name, string shortHand, bool adminOnly, string usage, string description)>> _commandsByGroup
        = [];
    public static void Main(string[] args)
    {
        // Check if we're running in a GitHub Actions environment and skip
        if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true")
        {
            Console.WriteLine("GenerateREADME skipped during GitHub Actions build.");
            return;
        }

        if (args.Length < 2)
        {
            Console.WriteLine("Usage: GenerateREADME <CommandsPath> <ReadMePath>");
            return;
        }

        CommandsPath = args[0];
        ReadMePath = args[1];

        try
        {
            Generate();
            Console.WriteLine("README generated successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating README: {ex.Message}");
        }
    }
    static void Generate()
    {
        CollectCommands();
        var commandsSection = BuildCommandsSection();
        UpdateReadme(commandsSection);
    }
    static void CollectCommands()
    {
        var files = Directory.GetFiles(CommandsPath, "*.cs")
                             .Where(file => !Path.GetFileName(file).Equals("DevCommands.cs", StringComparison.OrdinalIgnoreCase));

        foreach (var file in files)
        {
            var fileContent = File.ReadAllText(file);
            var commandGroupMatch = _commandGroupRegex.Match(fileContent);

            if (!commandGroupMatch.Success) commandGroupMatch = _commandGroupAndShortRegex.Match(fileContent);

            string groupName, groupShort;

            if (commandGroupMatch.Success)
            {
                groupName = commandGroupMatch.Groups["group"].Value;
                groupShort = commandGroupMatch.Groups["short"].Value;
            }
            else
            {
                groupName = "misc";
                groupShort = string.Empty;
            }

            if (!_commandsByGroup.TryGetValue((groupName, groupShort), out var cmdList))
            {
                cmdList = [];
                _commandsByGroup[(groupName, groupShort)] = cmdList;
            }

            foreach (Match commandMatch in _commandAttributeRegex.Matches(fileContent))
            {
                string name = commandMatch.Groups["name"].Value;
                string shortHand = commandMatch.Groups["shortHand"].Success ? commandMatch.Groups["shortHand"].Value : string.Empty;
                bool adminOnly = false;

                if (commandMatch.Groups["adminOnly"].Success)
                {
                    _ = bool.TryParse(commandMatch.Groups["adminOnly"].Value, out adminOnly);
                }

                string usage = commandMatch.Groups["usage"].Success ? commandMatch.Groups["usage"].Value : string.Empty;
                string description = commandMatch.Groups["description"].Success ? commandMatch.Groups["description"].Value : string.Empty;

                cmdList.Add((name, shortHand, adminOnly, usage, description));
            }
        }
    }
    static string BuildCommandsSection()
    {
        StringBuilder sb = new();
        sb.AppendLine("## Commands");

        var orderedGroups = _commandsByGroup.Keys.OrderBy(g => g.groupName).ToList();

        foreach (var group in orderedGroups)
        {
            var (groupName, groupShort) = group;
            //sb.AppendLine($"### {Capitalize(groupName)} Commands");

            var cmdList = _commandsByGroup[group];
            foreach (var (name, shortHand, adminOnly, usage, description) in cmdList)
            {
                bool hasShorthand = !string.IsNullOrEmpty(shortHand);
                bool hasGroupShort = !string.IsNullOrEmpty(groupShort);

                // If has parameters and no shorthand replace 
                string commandUsage = string.IsNullOrEmpty(usage) ? name : usage;
                string nameReplacement = commandUsage.EndsWith(name) || !hasShorthand ? name : string.Empty;

                // Prebuild command line strings
                string adminLock = adminOnly ? " 🔒" : string.Empty;
                string commandParameters = string.Empty;

                if (hasGroupShort)
                {
                    commandParameters = hasShorthand ? commandUsage.Replace($".{groupShort} {shortHand}", "") : commandUsage.Replace($".{groupShort} {nameReplacement}", "");
                }
                else
                {
                    commandParameters = hasShorthand ? commandUsage.Replace($".{groupName} {shortHand}", "") : commandUsage.Replace($".{groupName} {nameReplacement}", "");
                }

                // Build main command line string
                var commandLine = $"- `.{groupName} {name}{commandParameters}`{adminLock}";
                sb.AppendLine(commandLine);

                // Description line if available
                if (!string.IsNullOrEmpty(description))
                {
                    sb.AppendLine($"  - {description}");
                }

                sb.AppendLine($"  - Shortcut: *{commandUsage}*");
            }

            // Add spacing after each group, except the last one
            if (orderedGroups.IndexOf(group) < orderedGroups.Count - 1)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
    static void UpdateReadme(string commandsSection)
    {
        bool inCommandsSection = false;
        bool commandsReplaced = false;

        List<string> newContent = [];

        try
        {
            foreach (string line in File.ReadLines(ReadMePath))
            {
                if (line.Trim().Equals(COMMANDS_HEADER, StringComparison.OrdinalIgnoreCase))
                {
                    // Start of "## Commands"
                    inCommandsSection = true;
                    commandsReplaced = true;

                    newContent.Add(commandsSection); // Add new commands

                    continue;
                }

                if (inCommandsSection && line.Trim().StartsWith("## ", StringComparison.OrdinalIgnoreCase) &&
                    !line.Trim().Equals(COMMANDS_HEADER, StringComparison.OrdinalIgnoreCase))
                {
                    // Reached the next section or a new header
                    inCommandsSection = false;
                }

                if (!inCommandsSection)
                {
                    newContent.Add(line);
                }
            }

            if (!commandsReplaced)
            {
                // Append new section if "## Commands" not found
                newContent.Add(COMMANDS_HEADER);
                newContent.Add(commandsSection);
            }

            File.WriteAllLines(ReadMePath, newContent);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error updating the readme: {ex.Message}");
            throw;
        }
    }

    // Helper method to capitalize strings
    static string Capitalize(string input) =>
        string.IsNullOrEmpty(input) ? input : char.ToUpper(input[0]) + input[1..];
}