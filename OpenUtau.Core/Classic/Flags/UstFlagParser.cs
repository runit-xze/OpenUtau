using System.Collections.Generic;
using System.Text;

namespace OpenUtau.Classic.Flags {
    public class UstFlagParser {
        public IList<UstFlag> Parse(string text) {
            List<UstFlag> flags = new List<UstFlag>();
            var keyBuilder = new StringBuilder();
            var valueBuilder = new StringBuilder();
            bool wasDigit = false;
            bool inSelector = false;
            for (int i = 0; i <= text.Length; ++i) {
                if (i == text.Length || (char.IsLetter(text[i]) || text[i] == '/') && wasDigit) {
                    string key = keyBuilder.ToString();
                    if (!int.TryParse(valueBuilder.ToString(), out int value)) {
                        value = 0;
                    }
                    if (!string.IsNullOrEmpty(key)) {
                        flags.Add(new UstFlag(key, value));
                    }
                    keyBuilder.Clear();
                    valueBuilder.Clear();
                }
                if (i == text.Length) {
                    break;
                }
                char c = text[i];
                if (c == '/') {
                    // Note selector prefix (e.g. "/1"), not a flag. Skip it entirely.
                    inSelector = true;
                    wasDigit = false;
                    continue;
                }
                if (c == '-' || c == '+' || char.IsDigit(c)) {
                    if (!inSelector) {
                        valueBuilder.Append(c);
                        wasDigit = true;
                    }
                } else if (char.IsLetter(c)) {
                    inSelector = false;
                    if (keyBuilder.Length == 0 && IsSingleCharacterFlag(c)) {
                        flags.Add(new UstFlag(c.ToString(), 0));
                        continue;
                    }
                    keyBuilder.Append(c);
                    wasDigit = false;
                }
            }
            return flags;
        }

        private bool IsSingleCharacterFlag(char input) {
            var patterns = new HashSet<char> { 'N', 'e', 'u' };
            return patterns.Contains(input);
        }
    }
}
