namespace AMS.Models
{
    public enum PaymentStatus
    {
        MissingData,
        ReadyToSend,
        SentFirst,
        PartiallyPaid,
        Paid,
        Late,
        Closed
    }
}