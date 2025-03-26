using MediatR;
using Application.Common.Services.CSVManager;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Repositories;

namespace Application.Features.CSVManager.Commands
{
    public record ProcessCsvResult
    {
        public string Message { get; init; }
    }
    
    public class ProcessCsvRequest : IRequest<ProcessCsvResult>
    {
        public List<string> FilePath { get; set; }
        public string Separator { get; set; } = ",";
        public string? CreatedById { get; init; }
        public List<string> OriginalFileNames;

    }

    public class ProcessCsvHandler : IRequestHandler<ProcessCsvRequest, ProcessCsvResult>
    {
        private readonly ICsvProcessingService _csvProcessService;
        private readonly IUnitOfWork _unitOfWork;

        public ProcessCsvHandler(ICsvProcessingService csvProcessService, IUnitOfWork unitOfWork)
        {
            _csvProcessService = csvProcessService;
            _unitOfWork = unitOfWork;
        }

        public async Task<ProcessCsvResult> Handle(ProcessCsvRequest request, CancellationToken cancellationToken)
        {
            try
            {
                await _csvProcessService.allStepsAsync(request.FilePath, request.CreatedById, request.OriginalFileNames, request.Separator);
                
                return new ProcessCsvResult
                {
                    Message = "CSV import process completed successfully."
                };
            }
            catch (Exception ex)
            {
                return new ProcessCsvResult
                {   
                    Message = $"Error: {ex.Message}"
                };
            }
        }
    }
}