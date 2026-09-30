namespace VehicleContractSystem_Final.Services
{
    public class LocalPrivateFileStorage : IFileStorage
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public LocalPrivateFileStorage(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;
        }

        public string ProviderName => "LocalPrivate";

        public async Task<StoredFileResult> SaveContractDocumentAsync(
            int contractId,
            Stream content,
            string extension,
            CancellationToken cancellationToken = default)
        {
            var safeExtension = NormalizeExtension(extension);
            var storedFileName = $"{Guid.NewGuid():N}{safeExtension}";
            var uploadRoot = GetPrivateContractRoot(contractId);
            Directory.CreateDirectory(uploadRoot);

            var fullPath = BuildSafePath(uploadRoot, storedFileName)
                ?? throw new InvalidOperationException("Invalid document storage path.");

            await using (var output = File.Create(fullPath))
            {
                await content.CopyToAsync(output, cancellationToken);
            }

            return new StoredFileResult
            {
                StoredFileName = storedFileName,
                FilePath = BuildLogicalPath(contractId, storedFileName)
            };
        }

        public Task<StoredFileReadResult?> OpenContractDocumentAsync(
            int contractId,
            string storedFileName,
            string? filePath,
            CancellationToken cancellationToken = default)
        {
            var safeFileName = Path.GetFileName(storedFileName);
            if (contractId <= 0 || string.IsNullOrWhiteSpace(safeFileName))
                return Task.FromResult<StoredFileReadResult?>(null);

            var privateRoot = GetPrivateContractRoot(contractId);
            var privatePath = BuildSafePath(privateRoot, safeFileName);
            if (privatePath != null && File.Exists(privatePath))
            {
                return Task.FromResult<StoredFileReadResult?>(new StoredFileReadResult
                {
                    Content = File.OpenRead(privatePath)
                });
            }

            var legacyPath = BuildLegacyWebRootPath(contractId, safeFileName);
            if (legacyPath != null && File.Exists(legacyPath))
            {
                return Task.FromResult<StoredFileReadResult?>(new StoredFileReadResult
                {
                    Content = File.OpenRead(legacyPath)
                });
            }

            return Task.FromResult<StoredFileReadResult?>(null);
        }

        private string GetPrivateContractRoot(int contractId)
        {
            var configuredRoot = _configuration["FileStorage:LocalPrivateRoot"];
            var baseRoot = string.IsNullOrWhiteSpace(configuredRoot)
                ? Path.Combine(_environment.ContentRootPath, "App_Data", "uploads")
                : configuredRoot;

            return Path.GetFullPath(Path.Combine(baseRoot, "contracts", contractId.ToString()));
        }

        private string? BuildLegacyWebRootPath(int contractId, string storedFileName)
        {
            var legacyRoot = Path.GetFullPath(Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "contracts",
                contractId.ToString()));

            return BuildSafePath(legacyRoot, storedFileName);
        }

        private static string? BuildSafePath(string root, string storedFileName)
        {
            var fullRoot = Path.GetFullPath(root);
            var fileName = Path.GetFileName(storedFileName);
            var fullPath = Path.GetFullPath(Path.Combine(fullRoot, fileName));

            return fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase)
                ? fullPath
                : null;
        }

        private static string BuildLogicalPath(int contractId, string storedFileName)
        {
            return $"contracts/{contractId}/{storedFileName}";
        }

        private static string NormalizeExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
                throw new InvalidOperationException("Document extension is required.");

            return extension.StartsWith('.')
                ? extension.ToLowerInvariant()
                : $".{extension.ToLowerInvariant()}";
        }
    }
}
