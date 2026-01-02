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
        var assemblyLocation = Path.GetDirectoryName(
            typeof(BrunoCollectionFixture).Assembly.Location
        );

        if (assemblyLocation == null)
        {
            throw new InvalidOperationException(
                "Unable to determine assembly location; Assembly.Location is null. "
                    + "This may occur when running in certain test environments or when the assembly is loaded from memory."
            );
        }

        var currentDir = assemblyLocation;

        // Walk up until we find the samples directory or reach the root
        while (
            currentDir != null && !Directory.Exists(Path.Combine(currentDir, "bruno-collection"))
        )
        {
            var parent = Directory.GetParent(currentDir);
            if (parent == null)
            {
                break;
            }

            currentDir = parent.FullName;
        }

        if (currentDir == null)
        {
            throw new InvalidOperationException(
                "Unable to locate samples directory. Make sure the test is running from the expected location."
            );
        }

        CollectionPath = Path.Combine(currentDir, "bruno-collection");

        if (!Directory.Exists(CollectionPath))
        {
            throw new InvalidOperationException(
                $"Bruno collection not found at: {CollectionPath}. "
                    + "The directory 'bruno-collection' could not be found in any parent directory of the assembly location."
            );
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
