using System.Collections.Generic;
using Xunit;

namespace OpenUtau.Classic {
    public class SharpWavtoolTest {
        private static SharpWavtool.Segment Segment(
            float[] samples, int posSamples, int correction = 0, int skipSamples = 0) {
            return new SharpWavtool.Segment {
                samples = samples,
                posSamples = posSamples,
                correction = correction,
                skipSamples = skipSamples,
            };
        }

        [Fact]
        public void MixesSequentialSegments() {
            var result = SharpWavtool.MixSegments(new List<SharpWavtool.Segment> {
                Segment(new float[] { 1, 1 }, posSamples: 0),
                Segment(new float[] { 2, 2 }, posSamples: 2),
            });

            Assert.Equal(new float[] { 1, 1, 2, 2 }, result);
        }

        [Fact]
        public void OverlappingSegmentsAreSummed() {
            var result = SharpWavtool.MixSegments(new List<SharpWavtool.Segment> {
                Segment(new float[] { 1, 1, 1 }, posSamples: 0),
                Segment(new float[] { 2, 2 }, posSamples: 1),
            });

            Assert.Equal(new float[] { 1, 3, 3 }, result);
        }

        // A negative phase correction can place a segment before the buffer start.
        // This used to index phraseSamples with a negative offset and throw
        // IndexOutOfRangeException, crashing rendering (openutau/OpenUtau#2154).
        [Fact]
        public void NegativeCorrectionDoesNotThrow() {
            var result = SharpWavtool.MixSegments(new List<SharpWavtool.Segment> {
                Segment(new float[] { 1, 1, 1, 1 }, posSamples: 1, correction: -3),
            });

            // The two samples pushed before zero are dropped; the rest land at 0.
            Assert.Equal(new float[] { 1, 1 }, result);
        }

        // A later segment ending earlier than an earlier one used to shrink the
        // buffer via Array.Resize, so the following writes ran past the new end.
        [Fact]
        public void ShorterLaterSegmentDoesNotShrinkBuffer() {
            var result = SharpWavtool.MixSegments(new List<SharpWavtool.Segment> {
                Segment(new float[] { 1, 1, 1, 1, 1 }, posSamples: 0),
                Segment(new float[] { 2 }, posSamples: 0),
            });

            Assert.Equal(5, result.Length);
            Assert.Equal(new float[] { 3, 1, 1, 1, 1 }, result);
        }

        [Fact]
        public void SkipSamplesOffsetsTheRead() {
            var result = SharpWavtool.MixSegments(new List<SharpWavtool.Segment> {
                Segment(new float[] { 9, 9, 1, 1 }, posSamples: 0, skipSamples: 2),
            });

            Assert.Equal(new float[] { 1, 1 }, result);
        }
    }
}
