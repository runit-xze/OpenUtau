using System.IO;
using System.Text;
using Xunit;

namespace OpenUtau.Classic {
    public class ResamplerManifestTest {
        private static string WriteTempManifest(string yaml) {
            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".yaml");
            File.WriteAllText(path, yaml, Encoding.UTF8);
            return path;
        }

        [Fact]
        public void DefaultsWhenFieldsOmitted() {
            var path = WriteTempManifest("expression_filter: false\n");
            try {
                var manifest = ResamplerManifest.Load(path);

                Assert.False(manifest.expressionFilter);
                Assert.Empty(manifest.files);
                Assert.Empty(manifest.expressions);
            } finally {
                File.Delete(path);
            }
        }

        // 'files' lets a resampler manifest declare extra meta files to copy alongside
        // the sample (openutau/OpenUtau#1768). It defaults to empty rather than null so
        // callers can enumerate it unconditionally.
        [Fact]
        public void ReadsDeclaredMetaFiles() {
            var path = WriteTempManifest("files:\n  - .frq\n  - .llsm\n");
            try {
                var manifest = ResamplerManifest.Load(path);

                Assert.Equal(new[] { ".frq", ".llsm" }, manifest.files);
            } finally {
                File.Delete(path);
            }
        }

        [Fact]
        public void ExpressionKeysAreLowercased() {
            var path = WriteTempManifest(
                "expressions:\n" +
                "  MyExp:\n" +
                "    name: My Expression\n" +
                "    abbr: MYE\n" +
                "    type: Numerical\n" +
                "    min: 0\n" +
                "    max: 100\n" +
                "    default_value: 50\n");
            try {
                var manifest = ResamplerManifest.Load(path);

                Assert.True(manifest.expressions.ContainsKey("myexp"));
                Assert.False(manifest.expressions.ContainsKey("MyExp"));
            } finally {
                File.Delete(path);
            }
        }
    }
}
