namespace AMS.Models
{
    public enum PaymentStatus
    {
        MissingData,
        ReadyToSend,
        SentFirst,
        PartiallyPaid,
        UnPaid,
        Paid,
        Late,
        Closed
    }
}