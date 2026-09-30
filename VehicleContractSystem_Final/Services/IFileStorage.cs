namespace VehicleContractSystem_Final.Services
{
    public interface IFileStorage
    {
        string ProviderName { get; }
        Task<StoredFileResult> SaveContractDocumentAsync(int contractId, Stream content, string extension, CancellationToken cancellationToken = default);
        Task<StoredFileReadResult?> OpenContractDocumentAsync(int contractId, string storedFileName, string? filePath, CancellationToken cancellationToken = default);
    }

    public sealed class StoredFileResult
    {
        public string StoredFileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
    }

    public sealed class StoredFileReadResult : IAsyncDisposable
    {
        public Stream Content { get; set; } = Stream.Null;

        public ValueTask DisposeAsync()
        {
            return Content.DisposeAsync();
        }
    }
}
