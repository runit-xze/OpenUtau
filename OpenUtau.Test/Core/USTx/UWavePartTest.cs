using System.IO;
using OpenUtau.Core.Ustx;
using Xunit;

namespace OpenUtau.Core.USTx {
    public class UWavePartTest {
        // Autosave writes a backup without ever setting project.FilePath, so a project
        // that has never been saved reaches BeforeSave with an empty FilePath. That used
        // to pass null into Path.GetRelativePath and throw ArgumentNullException, which
        // broke autosave permanently for new projects holding audio
        // (openutau/OpenUtau#2229).
        [Fact]
        public void BeforeSaveOnUnsavedProjectDoesNotThrow() {
            var project = new UProject();
            var track = new UTrack(project);
            var audioPath = Path.Combine(Path.GetTempPath(), "openutau-test-audio.wav");
            var part = new UWavePart { FilePath = audioPath };

            part.BeforeSave(project, track);

            // Nothing to be relative to, so the absolute path is kept verbatim.
            Assert.Equal(audioPath, part.relativePath);
        }

        [Fact]
        public void BeforeSaveOnSavedProjectStoresRelativePath() {
            var dir = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
            var project = new UProject { FilePath = Path.Combine(dir, "song.ustx") };
            var track = new UTrack(project);
            var part = new UWavePart { FilePath = Path.Combine(dir, "audio.wav") };

            part.BeforeSave(project, track);

            Assert.Equal("audio.wav", part.relativePath);
        }
    }
}
