namespace OpenUtau.Api {
	public interface IG2p {
		bool IsValidSymbol(string symbol);
		bool IsVowel(string symbol);

		bool IsGlide(string symbol);

		string[] Query(string grapheme);

		string[] UnpackHint(string hint, char separator = ' ');
	}
}
