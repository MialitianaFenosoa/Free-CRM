using System;
using System.Globalization;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.SeedManager.Demos;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.CSVManager
{
    public static class CsvHelperExtensions
    {
        public static object ConvertToType(string value, Type targetType)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                value = value.Substring(1, value.Length - 2); 
            }
            try
            {
                if (targetType.IsEnum)
                {
                    if (int.TryParse(value, out var intValue))
                        return Enum.ToObject(targetType, intValue);
                    if (Enum.TryParse(targetType, value, true, out var enumValue))
                        return enumValue;
                    
                    throw new InvalidCastException($"Invalid enum value '{value}' for {targetType.Name}");
                }

                if (targetType == typeof(int) || targetType == typeof(int?)) 
                {
                    int result = int.Parse(value, CultureInfo.GetCultureInfo("fr-FR"));
                    if (result <= 0) throw new ArgumentOutOfRangeException($"Value must be > 0: {value}");
                    return result;
                }

                if (targetType == typeof(long) || targetType == typeof(long?)) 
                {
                    long result = long.Parse(value, CultureInfo.GetCultureInfo("fr-FR"));
                    if (result <= 0) throw new ArgumentOutOfRangeException($"Value must be > 0: {value}");
                    return result;
                }

                if (targetType == typeof(double) || targetType == typeof(double?)) 
                {
                    double result = ParseFlexibleNumber(value);
                    if (result <= 0) throw new ArgumentOutOfRangeException($"Value must be > 0: {value}");
                    return result;
                }

                if (targetType == typeof(float) || targetType == typeof(float?)) 
                {
                    float result = float.Parse(value, CultureInfo.GetCultureInfo("fr-FR"));
                    if (result <= 0) throw new ArgumentOutOfRangeException($"Value must be > 0: {value}");
                    return result;
                }

                if (targetType == typeof(bool)) return bool.Parse(value);
                if (targetType == typeof(string)) return value.Trim();

                if (targetType == typeof(DateTime))
                {
                    string[] formats = {
                        "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd", "MM/dd/yyyy", 
                        "dd/MM/yyyy", "yyyy/MM/dd", "dd/MM/yyyy HH:mm:ss", 
                        "MM/dd/yyyy HH:mm:ss", "yyyy/MM/dd HH:mm:ss"
                    };

                    if (DateTime.TryParseExact(value, formats, CultureInfo.GetCultureInfo("fr-FR"), DateTimeStyles.None, out var dateValue))
                        return dateValue;

                    throw new FormatException($"Invalid DateTime format: '{value}'");
                }

                return Convert.ChangeType(value, targetType, CultureInfo.GetCultureInfo("fr-FR"));
            }
            catch (Exception ex)
            {
                throw new InvalidCastException($"Cannot convert value '{value}' to type {targetType.FullName}: {ex.Message}");
            }
        }


        public static string GenerateDescription(string entityName, string title)
        {
            return entityName + " Description for " + title;
        }
        
        public static DateTime GenerateRandomDate(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
                throw new ArgumentException("startDate must be earlier than endDate.");

            Random random = new Random();
            int range = (endDate - startDate).Days;
            return startDate.AddDays(random.Next(range + 1)); 
        }

       
        public static Enum GenerateStatus(string entityName)
        {
            Random random = new Random();

            if (entityName.Equals("Expense", StringComparison.OrdinalIgnoreCase))
            {
                return Util.GetRandomStatus(random, 
                    new[] { ExpenseStatus.Draft, ExpenseStatus.Cancelled, ExpenseStatus.Confirmed, ExpenseStatus.Archived }, 
                    new[] { 0, 0, 4, 0 });
            }
            else if (entityName.Equals("Campaign", StringComparison.OrdinalIgnoreCase))
            {
                return Util.GetRandomStatus(random, 
                    new[] { CampaignStatus.Draft, CampaignStatus.Cancelled, CampaignStatus.Confirmed, 
                        CampaignStatus.OnProgress, CampaignStatus.OnHold, CampaignStatus.Finished, CampaignStatus.Archived }, 
                    new[] { 0, 0, 4, 0, 0, 0, 0 });
            }
            else if (entityName.Equals("Budget", StringComparison.OrdinalIgnoreCase))
            {
                return Util.GetRandomStatus(random, 
                    new[] { BudgetStatus.Draft, BudgetStatus.Cancelled, BudgetStatus.Confirmed, BudgetStatus.Archived }, 
                    new[] { 0, 0, 3, 0 });
            }
            else
            {
                throw new ArgumentException($"Unknown entity name: {entityName}");
            }
        }
        
        
        public static async Task<string> GenerateSalesTeamIdAsync(ICommandRepository<SalesTeam> salesTeamRepository, SalesTeamSeeder seeder)
        {
            Random random = new Random();
            if (salesTeamRepository == null)
                throw new ArgumentNullException(nameof(salesTeamRepository));
            var salesTeamIds = await salesTeamRepository.GetQuery()
                .Select(st => st.Id)
                .ToListAsync();
            if (salesTeamIds.Count == 0)
            {
                await seeder.GenerateDataAsync();
                salesTeamIds = await salesTeamRepository.GetQuery()
                    .Select(st => st.Id)
                    .ToListAsync();
            }
            return Util.GetRandomValue(salesTeamIds, random);
        }


        public static double GenerateAmount()
        {
            Random random = new Random();
            return 10000 * Math.Ceiling((random.NextDouble() * 89) + 1);
        }
        
        
        
        public static List<string> OrderCsvFiles(List<string> filePaths, string separator = ",")
        {
            if (filePaths == null || filePaths.Count == 0)
            {
                Console.WriteLine("[ERROR] The file list is empty or null.");
                throw new ArgumentException("The file list is empty or null.");
            }

            Console.WriteLine($"[INFO] Processing {filePaths.Count} files.");

            var prioritizedFiles = new List<string>(); 
            var delayedFiles = new List<string>();     

            foreach (var filePath in filePaths)
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"[ERROR] File not found: {filePath}");
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                Console.WriteLine($"[INFO] Checking file: {filePath}");

                var firstLine = File.ReadLines(filePath).FirstOrDefault();
                if (firstLine == null)
                {
                    Console.WriteLine($"[WARNING] File {filePath} is empty. Skipping...");
                    continue; 
                }

                var headers = firstLine.Split(separator).Select(h => h.Trim()).ToArray();
                Console.WriteLine($"[INFO] Headers found: {string.Join(", ", headers)}");

                if (headers.Any(h => h.Equals("Type", StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine($"[DEBUG] File {filePath} contains 'Type' column. Adding to delayed list.");
                    delayedFiles.Add(filePath); 
                }
                else
                {
                    Console.WriteLine($"[DEBUG] File {filePath} does not contain 'Type' column. Adding to prioritized list.");
                    prioritizedFiles.Add(filePath); 
                }
            }

            var orderedList = prioritizedFiles.Concat(delayedFiles).ToList();
            Console.WriteLine($"[INFO] Final ordered list: {string.Join(" -> ", orderedList)}");

            return orderedList;
        }

        
        public static double ParseFlexibleNumber(string value)
        {
            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                value = value.Substring(1, value.Length - 2);
            }
            value = value.Replace(".", ","); 
            return double.Parse(value, CultureInfo.GetCultureInfo("fr-FR"));
        }

        
    }
}
