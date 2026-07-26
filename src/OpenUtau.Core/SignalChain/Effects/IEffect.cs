namespace OpenUtau.Core.SignalChain.Effects {
	public interface IEffect {
		void Process(float[] buffer, int offset, int count);

		void Reset();

		bool IsBypassed { get; }
	}
}
