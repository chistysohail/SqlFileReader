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

        // Updated file search pattern to only find files starting with 'SP_' and ending with '.sql'
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
        var regex = new Regex(@"CREATE\s+PROCEDURE\s+[\[\w]+\.\[\w]+\.\[(\w+)\]|CREATE\s+PROCEDURE\s+(\w+)", RegexOptions.IgnoreCase);
        var tableRegex = new Regex(@"FROM\s+[\[\w]+\.\[\w]+\.\[(\w+)\]|FROM\s+(\w+)|JOIN\s+[\[\w]+\.\[\w]+\.\[(\w+)\]|JOIN\s+(\w+)", RegexOptions.IgnoreCase);
        var operationRegex = new Regex(@"\b(SELECT|INSERT|UPDATE|DELETE)\b", RegexOptions.IgnoreCase);

        var procedures = new Dictionary<string, (HashSet<string> Tables, HashSet<string> Operations, int LineNumber)>();

        var lines = sql.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var matches = regex.Matches(lines[i]);
            foreach (Match match in matches)
            {
                string procName = match.Groups[1].Value;
                if (string.IsNullOrEmpty(procName))
                {
                    procName = match.Groups[2].Value;
                }

                if (!procedures.ContainsKey(procName))
                {
                    procedures[procName] = (new HashSet<string>(), new HashSet<string>(), i + 1);
                }

                var tableMatches = tableRegex.Matches(sql);
                foreach (Match tableMatch in tableMatches)
                {
                    for (int j = 1; j < tableMatch.Groups.Count; j++)
                    {
                        if (!string.IsNullOrEmpty(tableMatch.Groups[j].Value))
                        {
                            procedures[procName].Tables.Add(tableMatch.Groups[j].Value);
                            break;
                        }
                    }
                }

                var operationMatches = operationRegex.Matches(sql);
                foreach (Match opMatch in operationMatches)
                {
                    procedures[procName].Operations.Add(opMatch.Value.ToUpperInvariant());
                }
            }
        }

        return procedures;
    }
}
