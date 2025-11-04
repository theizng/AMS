namespace AMS.Services.Interfaces
{
    public interface IMaintenanceSheetWriter
    {
        Task UpdateAsync(string requestId, IDictionary<string, object> values, CancellationToken ct = default);
    }
}