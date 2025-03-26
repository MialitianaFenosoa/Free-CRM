using Infrastructure.CSVManager.dto;
using System.Reflection;
using System.Text.RegularExpressions;
using Application.Common.Repositories;
using Application.Common.Services.CSVManager;
using Application.Features.NumberSequenceManager;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Infrastructure.SeedManager.Demos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.CSVManager
{
    public class CsvProcessingService : ICsvProcessingService
    {
        public List<CampaignDto> CampaignDtoList { get; private set; } = new();
        public List<DetailDto> DetailDtoList { get; private set; } = new();
        public List<Campaign> Campaigns { get; private set; } = new();
        public List<Expense> Expenses { get; private set; } = new();
        public List<Budget> Budgets { get; private set; } = new();
        private ICommandRepository<SalesTeam> salesTeamRepository;
        private readonly NumberSequenceService _numberSequenceService;
        private SalesTeamSeeder seeder;
        private readonly ICommandRepository<Campaign> _campaignrepository;
        // private readonly ICommandRepository<Budget> _budgetrepository;
        // private readonly ICommandRepository<Expense> _expenserepository;
        protected readonly CommandContext Context;
        
        
    
        public CsvProcessingService( ICommandRepository<Campaign> campaignRepository, CommandContext context, ICommandRepository<SalesTeam> salesTeamrepository, SalesTeamSeeder seeeder, NumberSequenceService nnumberSequenceService)
        {
            Context = context;
            salesTeamRepository = salesTeamrepository;
            seeder = seeeder;
            _numberSequenceService = nnumberSequenceService;
            _campaignrepository = campaignRepository;
            // _budgetrepository = repository2;
            // _expenserepository = repository3;
            // _unitOfWork = unitOfWork;
            CampaignDtoList = new List<CampaignDto>();
            DetailDtoList = new List<DetailDto>();
            Campaigns = new List<Campaign>();
            Expenses = new List<Expense>();
        }
        
                
        public async Task GenererDetail(DetailDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "The DetailDto is null.");
            
            bool isExpense = dto.Type.Equals("expense", StringComparison.OrdinalIgnoreCase);
            bool isBudget = dto.Type.Equals("budget", StringComparison.OrdinalIgnoreCase);
            
            if (!isExpense && !isBudget)
                throw new ArgumentException($"Unknown type '{dto.Type}'");
            
            object newEntity = isExpense ? new Expense() : new Budget();
            Type entityType = newEntity.GetType();
            
            Dictionary<string, PropertyInfo> entityProperties = entityType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToDictionary(p => p.Name.ToLower(), p => p);
            
            Dictionary<string, string> dtoData = new()
            {
                { "campaignnumber", dto.CampaignNumber },
                { "title", dto.Title },
                { "amount", dto.Amount },
                { "date", dto.Date }
            };
            
            foreach (var entry in dtoData)
            {
                string propName = entry.Key;
                string value = entry.Value;
            
                if (propName == "date")
                {
                    propName = isExpense ? "expensedate" : "budgetdate";
                }
                if (propName == "campaignnumber")
                {
                    propName = "campaignid";
                    var valueTemp = await GetCampaignIdByNumber(Campaigns, value);
                    if (string.IsNullOrWhiteSpace(valueTemp))
                    {
                        throw new ArgumentException($"Unknown Campaign value for '{propName}' : {value}");
                    }
                    value = valueTemp;
                }
                if (entityProperties.ContainsKey(propName))
                {
                    PropertyInfo prop = entityProperties[propName];
                    Type propType = prop.PropertyType;
            
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        throw new ArgumentException($"Missing value for '{propName}'");
                    }
            
                    try
                    {
                        object convertedValue = CsvHelperExtensions.ConvertToType(value, propType);
            
                        if (propName == "amount" && convertedValue is double amount && amount <= 0)
                            throw new ArgumentException($"'{propName}' must be greater than 0");
            
                        prop.SetValue(newEntity, convertedValue);
                    }
                    catch (Exception ex)
                    {
                        throw new FormatException($"Error on '{propName}': {ex.Message}");
                    }
                }
            }
            
            if (isExpense)
            {
                Expense expense = (Expense)newEntity;
            
                if (expense.Description == null)
                    expense.Description = CsvHelperExtensions.GenerateDescription("Expense", dto.Title);
                
                expense.Number = _numberSequenceService.GenerateNumber(nameof(Expense), "", "EXP");
            
                if (expense.Status == null)
                    expense.Status = (ExpenseStatus)CsvHelperExtensions.GenerateStatus("Expense");
            
                Expenses.Add(expense);
            }
            else
            {
                Budget budget = (Budget)newEntity;
                
                if (budget.Description == null)
                    budget.Description = CsvHelperExtensions.GenerateDescription("Budget", dto.Title);
            
                budget.Number = _numberSequenceService.GenerateNumber(nameof(Budget), "", "BUD");
                if (budget.Status == null)
                    budget.Status = (BudgetStatus)CsvHelperExtensions.GenerateStatus("Budget");
            
                Budgets.Add(budget);
            }
        }

        
        
        public async Task GenererCampaign(CampaignDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "CampaignDto is null.");
            
            Campaign newCampaign = new Campaign();
            
            Dictionary<string, PropertyInfo> campaignProperties = typeof(Campaign)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToDictionary(p => p.Name.ToLower(), p => p);
            
            Dictionary<string, string> dtoData = new()
            {
                { "number", dto.Number },
                { "title", dto.Title }
            };
            
            foreach (var entry in dtoData)
            {
                string propName = entry.Key;
                string value = entry.Value;
            
                if (campaignProperties.ContainsKey(propName))
                {
                    PropertyInfo prop = campaignProperties[propName];
                    Type propType = prop.PropertyType;
            
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        throw new ArgumentException($"Missing value '{propName}'");
                    }
                    try
                    {
                        object convertedValue = CsvHelperExtensions.ConvertToType(value, propType);
                        prop.SetValue(newCampaign, convertedValue);
                    }
                    catch (Exception ex)
                    {
                        throw new FormatException($"Error on '{propName}': {ex.Message}");
                    }
                }
            }
            
            if (newCampaign.Description == null)
                newCampaign.Description = CsvHelperExtensions.GenerateDescription("Campaign", dto.Title);
            
            if (newCampaign.TargetRevenueAmount == null)
                newCampaign.TargetRevenueAmount = CsvHelperExtensions.GenerateAmount();
            
            if (newCampaign.SalesTeamId == null)
                 newCampaign.SalesTeamId = await CsvHelperExtensions.GenerateSalesTeamIdAsync(salesTeamRepository, seeder);
            
            if (newCampaign.CampaignDateStart == null)
                newCampaign.CampaignDateStart = CsvHelperExtensions.GenerateRandomDate(DateTime.UtcNow.AddMonths(-6), DateTime.UtcNow);
            
            if (newCampaign.CampaignDateFinish == null)
                newCampaign.CampaignDateFinish = newCampaign.CampaignDateStart.Value.AddMonths(2);

            if (newCampaign.Status == null)
                newCampaign.Status = (CampaignStatus)CsvHelperExtensions.GenerateStatus("Campaign");
            
            Campaigns.Add(newCampaign);
        }
        
        
        
        public async Task ChangeDtoToEntity()
        {
            if (CampaignDtoList == null || DetailDtoList == null)
                throw new ArgumentNullException("Both CampaignDtoList and DetailDtoList must not be null.");
            
            List<Campaign> campaigns = new List<Campaign>();
            List<object> details = new List<object>();
            
            Dictionary<string, Campaign> campaignByNumber = new Dictionary<string, Campaign>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var campaignDto in CampaignDtoList)
            {
                try
                {
                    await GenererCampaign(campaignDto);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error processing in file {campaignDto.NomFichier}, line {campaignDto.Ligne}: {ex.Message}");
                }
            }
            
            foreach (var detailDto in DetailDtoList)
            {
                try
                {
                    await GenererDetail(detailDto);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error processing in file {detailDto.NomFichier}, line {detailDto.Ligne}: {ex.Message}");
                }
            }
            
        }
        
        
        
        public async Task insertAllAsync(string createdById)
        {
                foreach (var campaign in Campaigns)
                {
                    Console.WriteLine($"[INFOOO] IDCAMPAGNEEE: '{campaign.Number}'");
                    campaign.CreatedById = createdById;
                    try
                    {
                        await  Context.AddAsync(campaign);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                    
                }
                
                foreach (var budget in Budgets)
                {
                    budget.CreatedById = createdById;
                    try
                    {
                        await Context.AddAsync(budget);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                    
                }
                Budgets.Clear();
                DetailDtoList.Clear();
                
                foreach (var expense in Expenses)
                {
                    expense.CreatedById = createdById;
                    try
                    {
                        await Context.AddAsync(expense);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                    
                }
                Expenses.Clear();
                DetailDtoList.Clear();
        }



        public async Task allStepsAsync(List<string> filePaths, string createdById, List<string> originalFileNames, string separator = ",")
        {
            await Context.Database.BeginTransactionAsync();
            var fileMapping = filePaths
                .Select((filePath, index) => new { FilePath = filePath, OriginalName = originalFileNames[index] })
                .ToList();
            filePaths = CsvHelperExtensions.OrderCsvFiles(filePaths);
            originalFileNames = filePaths
                .Select(orderedPath => fileMapping.First(mapping => mapping.FilePath == orderedPath).OriginalName)
                .ToList();
            try
            {
                if (filePaths.Count == 0)
                {
                    throw new ArgumentException("At least one file path must be specified.");
                }

                int i = 0;
                foreach (var filePath in filePaths)
                {
                    await ChangeCsvToDtoAsync(filePath, originalFileNames[i], separator);
                    i++;
                }

                await ChangeDtoToEntity();
                 await insertAllAsync(createdById);
                Campaigns.Clear();
                CampaignDtoList.Clear();
                DetailDtoList.Clear();
                Budgets.Clear();
                Expenses.Clear();
                
                await Context.SaveChangesAsync();
                await Context.Database.CommitTransactionAsync();
            }
            catch (Exception e)
            {
                await Context.Database.RollbackTransactionAsync();
                Console.WriteLine(e);
                throw;
            }
            
            
        }

        public async Task ChangeCsvToDtoAsync(string filePath, string originalFileName, string separator)
        {
            Console.WriteLine($"[DEBUG] Separator: '{separator}'");
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[ERROR] File not found: {originalFileName}");
                throw new FileNotFoundException($"File not found: {originalFileName}");
            }

            Console.WriteLine($"[INFO] Processing file: {originalFileName}");

            var lines = await File.ReadAllLinesAsync(filePath);

            if (lines.Length < 2)
            {
                Console.WriteLine($"[WARNING] File {originalFileName} is empty or contains only headers.");
                throw new Exception($"File {originalFileName} is empty or contains only headers.");
            }

            var headers = lines[0].Split(separator).Select(h => h.Trim().ToLower()).ToArray();
            Console.WriteLine($"[INFO] CSV Headers: {string.Join(", ", headers)}");

            bool containsTypeColumn = headers.Contains("type");

            for (int i = 1; i < lines.Length; i++)
            {

                string pattern = @",(?=(?:[^""]*""[^""]*"")*[^""]*$)";

                string[] values = Regex.Split(lines[i], pattern).Select(v => v.Trim()).ToArray();
                foreach (var value in values)
                {
                    Console.WriteLine($"[Debug] Value OOOO {value} .");
                }
                
                if (values.Length != headers.Length)
                {
                    Console.WriteLine($"[WARNING] Skipping line {i + 1} due to column count mismatch.");
                    throw new Exception($"Column count mismatch on file {originalFileName} line {i + 1}");
                }

                Console.WriteLine($"[INFO] Processing line {i + 1}: {string.Join(", ", values)}");

                var dataDict = headers.Zip(values, (key, value) => new { key, value })
                                      .ToDictionary(x => x.key, x => x.value);

                var transformedData = new Dictionary<string, string>();

                foreach (var entry in dataDict)
                {
                    string key = entry.Key;
                    string value = entry.Value;

                    if (key.Contains("_"))
                    {
                        var split = key.Split('_');
                        if (split.Length == 2)
                        {
                            string tableName = split[0];
                            string columnName = split[1];

                            Console.WriteLine($"[DEBUG] Transforming key: {key} -> Table: {tableName}, Column: {columnName}");

                            if (columnName.Equals("code", StringComparison.OrdinalIgnoreCase) || columnName.Equals("number", StringComparison.OrdinalIgnoreCase))
                            {
                                if (containsTypeColumn)
                                {
                                    transformedData["campaignnumber"] = value;
                                    Console.WriteLine($"[DEBUG] Mapped '{key}' to 'campaignnumber': {value}");
                                }
                                else
                                {
                                    transformedData["number"] = value;
                                    Console.WriteLine($"[DEBUG] Mapped '{key}' to 'number': {value}");
                                }
                            }
                            else
                            {
                                transformedData[columnName] = value;
                                Console.WriteLine($"[DEBUG] Mapped '{key}' to '{columnName}': {value}");
                            }
                        }
                    }
                    else
                    {
                        transformedData[key] = value;
                        Console.WriteLine($"[DEBUG] Mapped '{key}' to '{key}': {value}");
                    }
                }

                if (!containsTypeColumn)
                {
                    var campaignDto = new CampaignDto
                    {
                        Number = transformedData.GetValueOrDefault("number", ""),
                        Title = transformedData.GetValueOrDefault("title", ""),
                        NomFichier = Path.GetFileName(filePath),
                        Ligne = (i + 1).ToString()
                    };

                    Console.WriteLine($"[INFO] Adding CampaignDto: {campaignDto.Number}, {campaignDto.Title}, {campaignDto.NomFichier}, Line: {campaignDto.Ligne}");
                    CampaignDtoList.Add(campaignDto);
                }
                else
                {
                    string type = transformedData.GetValueOrDefault("type", "");

                    var detailDto = new DetailDto
                    {
                        CampaignNumber = transformedData.GetValueOrDefault("campaignnumber", ""),
                        Title = transformedData.GetValueOrDefault("title", ""),
                        Type = type,
                        Date = transformedData.GetValueOrDefault("date", ""),
                        Amount = transformedData.GetValueOrDefault("amount", ""),
                        NomFichier = originalFileName,
                        Ligne = (i + 1).ToString()
                    };

                    Console.WriteLine($"[INFO] Adding DetailDto: {detailDto.CampaignNumber}, {detailDto.Title}, {detailDto.Type}, {detailDto.Amount}, {detailDto.NomFichier}, Line: {detailDto.Ligne}");
                    DetailDtoList.Add(detailDto);
                }
            }

            Console.WriteLine($"[INFO] Processing completed for {originalFileName}");
        }

        
        public async Task<string> GetCampaignIdByNumber(List<Campaign> campaigns, string campaignNumber)
        {
            var confirmedCampaigns = await _campaignrepository.GetQuery()
                .Where(c => c.Number == campaignNumber)
                .Select(c => c.Id)
                .ToListAsync();
            if (confirmedCampaigns.Count==0 && (campaigns == null || campaigns.Count == 0))
                throw new ArgumentException("The campaign list is empty or null.");

            if (string.IsNullOrWhiteSpace(campaignNumber))
                throw new ArgumentException("The campaign number is null or empty.");

            if (confirmedCampaigns.Count == 0)
            {
                var campaign = campaigns.FirstOrDefault(c => c.Number.Equals(campaignNumber, StringComparison.OrdinalIgnoreCase));
                return campaign?.Id;
            }
            else
            {
                var campaign = confirmedCampaigns.FirstOrDefault();
                return campaign;
            }
        }
        
    }
}
