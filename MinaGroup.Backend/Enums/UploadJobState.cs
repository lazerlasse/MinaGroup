namespace MinaGroup.Backend.Enums
{
    public enum UploadJobState
    {
        Queued = 0,
        Processing = 1,
        Retrying = 2,
        Succeeded = 3,
        Failed = 4,
        Cancelled = 5
    }
}
