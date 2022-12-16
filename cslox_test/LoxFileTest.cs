using Xunit;
using System.Runtime.CompilerServices;

namespace cslox_test;

public class LoxFileTest
{
    private cslox.CsLox _csLox = new();

    private static string GetThisFilePath([CallerFilePath] string path = "") => path;

    private static List<string> GetTestFiles(string testDir)
    {
        var currentPath = Path.GetDirectoryName(GetThisFilePath()) ?? "";
        Assert.True(currentPath != "");
        currentPath = Path.Combine(currentPath, testDir);
        var directoryInfo = new DirectoryInfo(currentPath);
        return directoryInfo.GetFiles().Select(fileInfo => fileInfo.FullName).ToList();
    }

    [Fact]
    public void TestSuccess()
    {
        var files = GetTestFiles("testdata");
        Assert.True(files.Count > 0);
        foreach (var file in files)
        {
            var returnValue = _csLox.RunFile(file);
            if (returnValue != 0)
            {
                Assert.Fail($"Return {returnValue} from \"{file}\".");
            }
        }
    }

    [Fact]
    public void TestFail()
    {
        var files = GetTestFiles("testdata/error");
        Assert.True(files.Count > 0);
        foreach (var file in files)
        {
            _csLox = new cslox.CsLox();
            var returnValue = _csLox.RunFile(file);
            if (returnValue == 0)
            {
                Assert.Fail($"Return {returnValue} from \"{file}\".");
            }
        }
    }
}