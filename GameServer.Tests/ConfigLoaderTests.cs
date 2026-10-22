using SharedLib.Config;

namespace GameServer.Tests;

public class ConfigLoaderTests
{
    [Fact]
    public void Load_RelativePath_UsesAppBaseDirectory_NotCurrentDirectory()
    {
        var fileName = $"config_test_{Guid.NewGuid():N}.json";
        var basePath = Path.Combine(AppContext.BaseDirectory, fileName);
        var tempDir = Path.Combine(Path.GetTempPath(), $"cfg_{Guid.NewGuid():N}");
        var originalDirectory = Directory.GetCurrentDirectory();

        Directory.CreateDirectory(tempDir);
        File.WriteAllText(basePath, "{\"Value\":42}");

        try
        {
            Directory.SetCurrentDirectory(tempDir);

            var config = ConfigLoader.Load<TestConfig>(fileName);

            Assert.Equal(42, config.Value);
            Assert.False(File.Exists(Path.Combine(tempDir, fileName)));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            File.Delete(basePath);
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    public sealed class TestConfig
    {
        public int Value { get; set; }
    }
}
