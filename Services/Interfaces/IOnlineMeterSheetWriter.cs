namespace AMS.Services.Interfaces
{
    public interface IOnlineMeterSheetWriter
    {
        // Update a single room's current meter readings (electric/water).
        Task UpdateRowAsync(MeterRow row, CancellationToken ct = default);

        // Roll forward: copy current to previous and clear current (performed server-side).
        Task RollForwardAsync(CancellationToken ct = default);
    }
}