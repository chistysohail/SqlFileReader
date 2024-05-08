using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;

namespace SqlCommandReader
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Please enter the directory path:");
            string directoryPath = Console.ReadLine();

            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            {
                Console.WriteLine("The specified directory does not exist or the path is invalid.");
                return;
            }

            var csvLines = new List<string> { "Sl. No.,Path,Line Number,Text" };
            int serialNumber = 1;

            foreach (string file in Directory.EnumerateFiles(directoryPath, "*.cs", SearchOption.AllDirectories))
            {
                var fileLines = File.ReadAllLines(file);

                for (int i = 0; i < fileLines.Length; i++)
                {
                    if (Regex.IsMatch(fileLines[i], @"new SqlCommand\("))
                    {
                        string lineText = fileLines[i].Trim();
                        csvLines.Add($"{serialNumber++},{file},{i + 1},\"{lineText}\"");
                    }
                }
            }

            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            string csvFilePath = Path.Combine(directoryPath, $"SqlCommandList_{timestamp}.csv");
            File.WriteAllLines(csvFilePath, csvLines);

            Console.WriteLine($"CSV file created at: {csvFilePath}");
        }
    }
}
