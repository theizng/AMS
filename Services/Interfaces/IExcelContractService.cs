namespace AMS.Services.Interfaces
{
    public interface IExcelContractService
    {
        // Generate one or more contracts into an .xlsx at the given path
        Task GenerateContractsAsync(string outputPath /*, add your data model params */);
    }
}