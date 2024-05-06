using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Please enter the directory path:");
        string folderPath = Console.ReadLine();

        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
        {
            Console.WriteLine("The specified directory does not exist or no path was entered.");
            return;
        }

        var sqlFiles = Directory.GetFiles(folderPath, "SP_*.sql", SearchOption.AllDirectories);
        List<string> csvLines = new List<string>
        {
            "Sl. No.,File Path,File Name,Stored Procedure Name,Used Tables,Operations,Line Number"
        };

        int serialNumber = 1;

        foreach (var file in sqlFiles)
        {
            string sqlContent = File.ReadAllText(file);
            var storedProcedures = ExtractStoredProcedures(sqlContent, file);

            foreach (var sp in storedProcedures)
            {
                string tables = string.Join("; ", sp.Value.Tables);
                string operations = string.Join("; ", sp.Value.Operations);
                csvLines.Add($"{serialNumber++},{file},{Path.GetFileName(file)},{sp.Key},{tables},{operations},{sp.Value.LineNumber}");
            }
        }

        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        string outputPath = Path.Combine(folderPath, $"ProceduresTables_{timestamp}.csv");
        File.WriteAllLines(outputPath, csvLines);
        Console.WriteLine($"Output saved to {outputPath}");
    }

    private static Dictionary<string, (HashSet<string> Tables, HashSet<string> Operations, int LineNumber)> ExtractStoredProcedures(string sql, string filePath)
    {
        var procedureRegex = new Regex(@"CREATE\s+PROCEDURE\s+[\[\w]+\.\[\w]+\.\[(\w+)\]|CREATE\s+PROCEDURE\s+(\w+).*?AS(.*?)END", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var tableRegex = new Regex(@"\bFROM\s+[\[\w]+\.\[\w]+\.\[(\w+)\]\b|\bFROM\s+(\w+)\b|\bJOIN\s+[\[\w]+\.\[\w]+\.\[(\w+)\]\b|\bJOIN\s+(\w+)\b", RegexOptions.IgnoreCase);
        var operationRegex = new Regex(@"\b(SELECT|INSERT|UPDATE|DELETE)\b", RegexOptions.IgnoreCase);

        var procedures = new Dictionary<string, (HashSet<string> Tables, HashSet<string> Operations, int LineNumber)>();

        var matches = procedureRegex.Matches(sql);
        foreach (Match match in matches)
        {
            string procName = match.Groups[1].Value;
            if (string.IsNullOrEmpty(procName))
            {
                procName = match.Groups[2].Value;
            }
            string procBody = match.Groups[3].Value;
            int lineNumber = CountLines(sql.Substring(0, match.Index)) + 1; // Calculates the line number of the procedure's start

            var tables = new HashSet<string>();
            var operations = new HashSet<string>();

            var tableMatches = tableRegex.Matches(procBody);
            foreach (Match tableMatch in tableMatches)
            {
                for (int i = 1; i < tableMatch.Groups.Count; i++)
                {
                    if (!string.IsNullOrEmpty(tableMatch.Groups[i].Value))
                    {
                        tables.Add(tableMatch.Groups[i].Value);
                        break;
                    }
                }
            }

            var operationMatches = operationRegex.Matches(procBody);
            foreach (Match opMatch in operationMatches)
            {
                operations.Add(opMatch.Value.ToUpperInvariant());
            }

            if (!procedures.ContainsKey(procName))
            {
                procedures[procName] = (tables, operations, lineNumber);
            }
        }

        return procedures;
    }

    // Helper method to count lines in a given text up to a certain position
    private static int CountLines(string text)
    {
        return text.Count(c => c == '\n') + 1;
    }
}
