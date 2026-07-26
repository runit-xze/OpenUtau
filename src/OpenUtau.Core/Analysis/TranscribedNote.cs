namespace OpenUtau.Core.Analysis;

public class TranscribedNote {
	public float noteDuration;

	public float noteScore;

	public bool noteVoiced;

	public TranscribedNote(float noteDuration, float noteScore, bool noteVoiced) {
		this.noteDuration = noteDuration;
		this.noteScore = noteScore;
		this.noteVoiced = noteVoiced;
	}
}

