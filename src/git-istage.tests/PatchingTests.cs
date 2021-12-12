using Moq;
using Xunit;
using System.Linq;
using System.IO;

namespace GitIStage.Tests
{
    public class PatchingTests
    {
        [Fact]
        public void Patching_PatchContentShouldNotChange()
        {
            var processRunner = new Mock<IProcessRunner>();
            string actualPatchContent = null;
            processRunner.Setup(p => p.Run(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                         .Callback<string, string, string, bool>((g, a, w, r) =>
                         {
                             var patchFilePath = a.Split(' ').LastOrDefault()?.Trim('"');
                             if (File.Exists(patchFilePath))
                             {
                                 actualPatchContent = File.ReadAllText(patchFilePath);
                             }
                         })
                         .Returns(Enumerable.Empty<string>());

            var patching = new Patching(processRunner.Object);

            var expectedPatchContent = $@"line1{'\n'}line2";

            patching.ApplyPatch("gitpath", "workingDirectory", expectedPatchContent, PatchDirection.Stage);

            Assert.Equal(expectedPatchContent, actualPatchContent);
        }
    }
}
