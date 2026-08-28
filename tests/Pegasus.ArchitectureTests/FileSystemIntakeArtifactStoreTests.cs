using System.Security.Cryptography;
using Pegasus.Core.Intake;
using Pegasus.Infrastructure.Intake;

namespace Pegasus.ArchitectureTests;

public sealed class FileSystemIntakeArtifactStoreTests
{
    [Fact]
    public async Task LocalFilesystemFailureIsTranslatedAtTheAdapterBoundary()
    {
        var rootFile = Path.GetTempFileName();
        try
        {
            var content = "controlled content"u8.ToArray();
            var hash = Convert.ToHexString(SHA256.HashData(content));
            using var store = new FileSystemIntakeArtifactStore(rootFile);

            var exception = await Assert.ThrowsAsync<IntakeDependencyUnavailableException>(
                () => store.StoreAsync(hash, content, CancellationToken.None));

            Assert.IsType<IOException>(exception.InnerException);
        }
        finally
        {
            File.Delete(rootFile);
        }
    }
}
