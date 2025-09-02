using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using CsvHelper;
using CsvHelper.Configuration;
using System.Text;
using System.Linq;

namespace Migration_Devops_Data_JIRA
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string inputCsv = @"C:\Users\Usman.Eijaz\Downloads\File\CMS-UMBRACO\CSV FORMAT\BATCH 1.csv";
            string outputCsv = @"\\10.99.8.77\DGIT shares\Temporary\Rohant\test\BATCH_1_Updated.csv";
            string centralFolder = @"\\10.99.8.77\DGIT shares\Temporary\Rohant\ATTACHMENTS";

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                MissingFieldFound = null,
                HeaderValidated = null,
                DetectColumnCountChanges = true
            };


            var updatedRows = new List<Dictionary<string, string>>();
            List<dynamic> records;

            using (var reader = new StreamReader(inputCsv))
            using (var csv = new CsvReader(reader, config))
            {
                records = csv.GetRecords<dynamic>().ToList();
            }

            foreach (var record in records)
            {
                var dict = (IDictionary<string, object>)record;
                var updated = new Dictionary<string, string>();

                foreach (var kvp in dict)
                {
                    string key = kvp.Key;
                    string value = kvp.Value?.ToString().Trim();

                    if (key.StartsWith("Attachment") && !string.IsNullOrEmpty(value))
                    {
                        try
                        {
                            // Extract filename and WorkItemID
                            string[] pathParts = value.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                            string fileName = Path.GetFileName(value);
                            string workItemId = pathParts.Reverse().Skip(1).FirstOrDefault();

                            if (string.IsNullOrEmpty(workItemId))
                            {
                                Console.WriteLine($"⚠️ Could not extract WorkItemID from path: {value}");
                                updated[key] = "Missing WorkItemID";
                                continue;
                            }

                            // Generate new filename
                            string newFileName = $"{workItemId}-{fileName}";
                            string newFilePath = Path.Combine(centralFolder, newFileName);

                            // Copy the file
                            if (File.Exists(value))
                            {
                                File.Copy(value, newFilePath, true);
                                updated[key] = newFilePath;
                                Console.WriteLine($"✅ Copied: {newFilePath}");
                            }
                            else
                            {
                                Console.WriteLine($"❌ File not found: {value}");
                                updated[key] = "File Not Found";
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Error processing {key}: {ex.Message}");
                            updated[key] = "Error";
                        }
                    }
                    else
                    {
                        updated[key] = value ?? "";
                    }
                }

                updatedRows.Add(updated);
            }

            using (var writer = new StreamWriter(outputCsv))
            using (var csvWriter = new CsvWriter(writer, config))
            {
                var headers = ((IDictionary<string, object>)records.FirstOrDefault())?.Keys?.ToList();

                if (headers != null)
                {
                    foreach (var header in headers)
                        csvWriter.WriteField(header);
                    csvWriter.NextRecord();

                    foreach (var row in updatedRows)
                    {
                        foreach (var header in headers)
                            csvWriter.WriteField(row.ContainsKey(header) ? row[header] : "");
                        csvWriter.NextRecord();
                    }
                }
            }

            Console.WriteLine("✅ All done. Updated CSV saved.");
        }
    }
}
