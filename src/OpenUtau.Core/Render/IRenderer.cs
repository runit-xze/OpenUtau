using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Render {
	public class NoResamplerException : Exception { }
	public class NoWavtoolException : Exception { }
	public class ResamplerFailedException : Exception {
		public ResamplerFailedException(string message) : base(message) { }
	}

	public class RenderResult {
		public float[] samples;

		public double leadingMs;

		public double positionMs;

		public double estimatedLengthMs;
	}

	public class RenderPitchResult {
		public float[] ticks;

		public float[] tones;
	}

	public class RenderRealCurveResult {
		public string abbr;

		public float[] ticks;

		public float[] values;
	}

	public interface IRenderer {
		USingerType SingerType { get; }
		bool SupportsRenderPitch { get; }
		bool SupportsRealCurve { get { return false; } }
		bool SupportsExpression(UExpressionDescriptor descriptor);
		RenderResult Layout(RenderPhrase phrase);
		Task<RenderResult> Render(RenderPhrase phrase, Progress progress, int trackNo, CancellationTokenSource cancellation, bool isPreRender = false);
		RenderPitchResult LoadRenderedPitch(RenderPhrase phrase);
		List<RenderRealCurveResult> LoadRenderedRealCurves(RenderPhrase phrase) { return new List<RenderRealCurveResult>(0); }
		UExpressionDescriptor[] GetSuggestedExpressions(USinger singer, URenderSettings renderSettings);
	}
}
