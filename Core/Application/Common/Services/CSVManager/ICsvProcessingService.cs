namespace Application.Common.Services.CSVManager;

public interface ICsvProcessingService
{
    public Task allStepsAsync(List<string> filePaths, string createdById, List<string> original, string separator = ",");
}