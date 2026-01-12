namespace MinaGroup.Backend.Enums
{
    public enum UploadJobState
    {
        None = 0,
        Queued = 1,
        Processing = 2,
        Retrying = 3,
        Succeeded = 4,
        Skipped = 5,     // ✅ ny
        Failed = 6,
        Cancelled = 7
    }
}
