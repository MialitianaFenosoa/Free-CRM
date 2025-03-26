using Application.Common.Repositories;
using Application.Common.Services.CSVManager;
using Infrastructure.DataAccessManager.EFCore.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.CSVManager
{
    public static class DI
    {
        public static IServiceCollection RegisterCSVManager(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<CSVSettings>(configuration.GetSection("CSVSettings"));
            services.AddTransient<ICsvImportService, CsvImportService>();
            services.AddTransient<ICsvExportService, CsvExportService>();
            services.AddTransient<IEntityMetadataService, EntityMetadataService>();
            services.AddTransient<ICsvProcessingService, CsvProcessingService>();
            return services;
        }
    }
}