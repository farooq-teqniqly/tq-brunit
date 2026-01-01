namespace XUnitSample;

/// <summary>
/// XUnit fixture that provides the path to the Bruno collection.
/// This ensures the collection path is resolved once and shared across all tests.
/// </summary>
public sealed class BrunoCollectionFixture : IDisposable
{
    public string CollectionPath { get; }

    public BrunoCollectionFixture()
    {
        // Resolve the collection path relative to the sample project
        // Go up from bin/Debug/net10.0 to the source directory, then to samples root
        var assemblyLocation = typeof(BrunoCollectionFixture).Assembly.Location;
        var binDirectory = Path.GetDirectoryName(assemblyLocation)!;
        var debugDirectory = Path.GetDirectoryName(binDirectory)!;
        var netDirectory = Path.GetDirectoryName(debugDirectory)!;
        var binFolder = Path.GetDirectoryName(netDirectory)!;
        var samplesDirectory = Path.GetDirectoryName(binFolder)!;
        CollectionPath = Path.Combine(samplesDirectory, "bruno-collection");

        if (!Directory.Exists(CollectionPath))
        {
            throw new InvalidOperationException(
                $"Bruno collection not found at: {CollectionPath}. "
                    + "Make sure the samples/bruno-collection exists."
            );
        }
    }

    public void Dispose()
    {
        // No cleanup needed - sealed class with no unmanaged resources
        GC.SuppressFinalize(this);
    }
}
