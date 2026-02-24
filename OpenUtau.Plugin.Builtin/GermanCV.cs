using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("German CV Phonemizer", "German CV", "LilianMakesStuff", language: "DE")]
    public class GermanCVPhonemizer : SyllableBasedPhonemizer {

        // All vowel phonemes in the reclist
        protected override string[] GetVowels() => new string[] {
            "a", "e", "i", "o", "u",
            "ae", "oe", "ue",
            "ai", "au", "eu"
        };

        // All consonant phonemes in the reclist
        protected override string[] GetConsonants() => new string[] {
            "b", "d", "f", "g", "h", "j", "k", "l", "m", "n", "ng",
            "p", "r", "s", "sh", "t", "ts", "v", "ch", "ach",
            "sp", "st", "pf", "kv"
        };

        // G2P: convert German spelling to reclist phonemes
        protected override string[] GetSymbols(Note note) {
            string lyric = note.lyric.ToLower().Trim();
            var result = new List<string>();
            int i = 0;

            while (i < lyric.Length) {
                // Try multi-character clusters first (order matters!)
                if (TryMatch(lyric, i, "tsch", out _)) { result.Add("sh"); i += 4; }
                else if (TryMatch(lyric, i, "sch", out _)) { result.Add("sh"); i += 3; }
                else if (TryMatch(lyric, i, "ach", out _)) { result.Add("ach"); i += 3; }
                else if (TryMatch(lyric, i, "äu", out _)) { result.Add("eu"); i += 2; }
                else if (TryMatch(lyric, i, "ie", out _)) { result.Add("i"); i += 2; }
                else if (TryMatch(lyric, i, "ei", out _)) { result.Add("ai"); i += 2; }
                else if (TryMatch(lyric, i, "ae", out _)) { result.Add("ae"); i += 2; }
                else if (TryMatch(lyric, i, "oe", out _)) { result.Add("oe"); i += 2; }
                else if (TryMatch(lyric, i, "ue", out _)) { result.Add("ue"); i += 2; }
                else if (TryMatch(lyric, i, "ai", out _)) { result.Add("ai"); i += 2; }
                else if (TryMatch(lyric, i, "au", out _)) { result.Add("au"); i += 2; }
                else if (TryMatch(lyric, i, "eu", out _)) { result.Add("eu"); i += 2; }
                else if (TryMatch(lyric, i, "ng", out _)) { result.Add("ng"); i += 2; }
                else if (TryMatch(lyric, i, "ck", out _)) { result.Add("k"); i += 2; }
                else if (TryMatch(lyric, i, "pf", out _)) { result.Add("pf"); i += 2; }
                else if (TryMatch(lyric, i, "qu", out _)) { result.Add("kv"); i += 2; }
                else if (TryMatch(lyric, i, "sp", out _)) { result.Add("sp"); i += 2; }
                else if (TryMatch(lyric, i, "st", out _)) { result.Add("st"); i += 2; }
                else if (TryMatch(lyric, i, "tz", out _)) { result.Add("ts"); i += 2; }
                else if (TryMatch(lyric, i, "ss", out _)) { result.Add("s"); i += 2; }
                else if (TryMatch(lyric, i, "ch", out _)) { result.Add("ch"); i += 2; }
                else {
                    // Single characters
                    char c = lyric[i];
                    switch (c) {
                        case 'a': result.Add("a"); break;
                        case 'e': result.Add("e"); break;
                        case 'i': result.Add("i"); break;
                        case 'o': result.Add("o"); break;
                        case 'u': result.Add("u"); break;
                        case 'ä': result.Add("ae"); break;
                        case 'ö': result.Add("oe"); break;
                        case 'ü': result.Add("ue"); break;
                        case 'b': result.Add("b"); break;
                        case 'd': result.Add("d"); break;
                        case 'f': result.Add("f"); break;
                        case 'g': result.Add("g"); break;
                        case 'h': result.Add("h"); break;
                        case 'j': result.Add("j"); break;
                        case 'k': result.Add("k"); break;
                        case 'l': result.Add("l"); break;
                        case 'm': result.Add("m"); break;
                        case 'n': result.Add("n"); break;
                        case 'p': result.Add("p"); break;
                        case 'r': result.Add("r"); break;
                        case 's': result.Add("s"); break;
                        case 't': result.Add("t"); break;
                        case 'v': result.Add("v"); break;
                        case 'w': result.Add("v"); break;
                        case 'x': result.Add("s"); break;
                        case 'y': result.Add("i"); break;
                        case 'z': result.Add("ts"); break;
                        case 'ß': result.Add("s"); break;
                        default: break;
                    }
                    i++;
                }
            }
            return result.ToArray();
        }

        protected override List<string> ProcessSyllable(Syllable syllable) {
            string prevV = syllable.prevV;
            string[] cc = syllable.cc;
            string v = syllable.v;
            var result = new List<string>();

            if (syllable.IsStartingV) {
                result.Add(v);
            } else if (syllable.IsVV) {
                result.Add(v);
            } else if (syllable.IsStartingCV) {
                result.Add(cc[0] + v);
            } else if (syllable.IsVCV) {
                result.Add(cc[0] + v);
            } else {
                // Fallback
                result.Add(v);
            }
            return result;
        }

        protected override List<string> ProcessEnding(Ending ending) {
            var result = new List<string>();
            if (ending.IsEndingV) return result;
            foreach (var c in ending.cc) {
                result.Add(c);
            }
            return result;
        }

        bool TryMatch(string s, int i, string pattern, out string matched) {
            matched = pattern;
            return i + pattern.Length <= s.Length && s.Substring(i, pattern.Length) == pattern;
        }
    }
}

