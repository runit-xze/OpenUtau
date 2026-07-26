namespace OpenUtau.Core.SignalChain {
	public interface ISignalSource {
		bool IsReady(int position, int count);
		int Mix(int position, float[] buffer, int index, int count);
	}
}
