using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using OpenUtau.Core.Ustx;
using OpenUtau.Classic;
using Serilog;
using static OpenUtau.Api.Phonemizer;
using OpenUtau.Api;

namespace OpenUtau.Core {
	public static class KoreanPhonemizerUtil {
		const string FIRST_CONSONANTS = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";
		const string MIDDLE_VOWELS = "ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ";

		const string LAST_CONSONANTS = " ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ"; // The first blank(" ") is needed because Hangeul may not have lastConsonant.

		const ushort HANGEUL_UNICODE_START = 0xAC00;

		const ushort HANGEUL_UNICODE_END = 0xD79F;

		public static readonly Hashtable basicSounds = new Hashtable() {
			["ㄱ"] = 0,
			["ㄷ"] = 1,
			["ㅂ"] = 2,
			["ㅈ"] = 3,
			["ㅅ"] = 4
		};

		public static readonly Hashtable aspirateSounds = new Hashtable() {
			[0] = "ㅋ",
			[1] = "ㅌ",
			[2] = "ㅍ",
			[3] = "ㅊ",
			[4] = "ㅌ"
		};

		public static readonly Hashtable fortisSounds = new Hashtable() {
			[0] = "ㄲ",
			[1] = "ㄸ",
			[2] = "ㅃ",
			[3] = "ㅉ",
			[4] = "ㅆ"
		};

		public static readonly Hashtable nasalSounds = new Hashtable() {
			["ㄴ"] = 0,
			["ㅇ"] = 1,
			["ㅁ"] = 2
		};

		public static readonly Dictionary<String, String> ROMAJI_KOREAN_FIRST_CONSONANTS_DICT = new Dictionary<String, String>() {
			{"g", "ㄱ"},
			{"n", "ㄴ"},
			{"d", "ㄷ"},
			{"r", "ㄹ"},
			{"l", "ㄹ"},
			{"m", "ㅁ"},
			{"b", "ㅂ"},
			{"s", "ㅅ"},
			{"j", "ㅈ"},
			{"ch", "ㅊ"},
			{"k", "ㅋ"},
			{"t", "ㅌ"},
			{"p", "ㅍ"},
			{"h", "ㅎ"},
			{"gg", "ㄲ"},
			{"kk", "ㄲ"},
			{"dd", "ㄸ"},
			{"tt", "ㄸ"},
			{"bb", "ㅃ"},
			{"pp", "ㅃ"},
			{"ss", "ㅆ"},
			{"jj", "ㅉ"},
			{"", "ㅇ" }
		};

		public static readonly Dictionary<String, String> ROMAJI_KOREAN_MIDDLE_VOWELS_DICT = new Dictionary<String, String>() {
			{"yae", "ㅒ"},
			{"yeo", "ㅕ"},
			{"wae", "ㅙ"},
			{"weo", "ㅝ"},
			{"eui", "ㅢ"},
			{"ui", "ㅢ"},
			{"wa", "ㅘ"},
			{"oe", "ㅚ"},
			{"wo", "ㅝ"},
			{"wi", "ㅟ"},
			{"we", "ㅞ"},
			{"ya", "ㅑ"},
			{"yu", "ㅠ"},
			{"ye", "ㅖ"},
			{"yo", "ㅛ"},
			{"ae", "ㅐ"},
			{"eu", "ㅡ"},
			{"eo", "ㅓ"},
			{"a", "ㅏ"},
			{"i", "ㅣ"},
			{"u", "ㅜ"},
			{"e", "ㅔ"},
			{"o", "ㅗ"},
		};

		// <summary>
		public static readonly Dictionary<String, String> ROMAJI_KOREAN_LAST_CONSONANTS_DICT = new Dictionary<String, String>() {
			{"k", "ㄱ"},
			{"n", "ㄴ"},
			{"t", "ㄷ"},
			{"l", "ㄹ"},
			{"m", "ㅁ"},
			{"p", "ㅂ"},
			{"ng", "ㅇ"},
			{"", " " }
		};

		public static bool IsHangeul(string? character) {

			ushort unicodeIndex;
			bool isHangeul;
			if ((character != null) && character.StartsWith('!')) {
				// Automatically deletes ! from start.
				// Prevents error when user uses ! as a phonetic symbol.  
				unicodeIndex = Convert.ToUInt16(character.TrimStart('!')[0]);
				isHangeul = !(unicodeIndex < HANGEUL_UNICODE_START || unicodeIndex > HANGEUL_UNICODE_END);
			} else if (character != null) {
				try {
					unicodeIndex = Convert.ToUInt16(character[0]);
					isHangeul = !(unicodeIndex < HANGEUL_UNICODE_START || unicodeIndex > HANGEUL_UNICODE_END);
				} catch {
					isHangeul = false;
				}

			} else {
				isHangeul = false;
			}

			return isHangeul;
		}

		public static bool IsKoreanRomaji(string lyric) {
			if (!KoreanPhonemizerUtil.IsHangeul(lyric) && KoreanPhonemizerUtil.TryParseKoreanRomaji(lyric) != null) {
				return true;
			}
			return false;
		}

		public static string? TryParseKoreanRomaji(string? romaji) {

			if (string.IsNullOrEmpty(romaji)) {
				return null;
			}
			List<string> allRomajiHangeul = new List<string>();
			List<string> allRomajiRomaji = new List<string>();
			StringBuilder sb = new StringBuilder();
			foreach (var first in ROMAJI_KOREAN_FIRST_CONSONANTS_DICT.Keys) {
				foreach (var middle in ROMAJI_KOREAN_MIDDLE_VOWELS_DICT.Keys) {
					foreach (var last in ROMAJI_KOREAN_LAST_CONSONANTS_DICT.Keys) {
						sb.Clear();
						sb.Append(first);
						sb.Append(middle);
						sb.Append(last);
						allRomajiRomaji.Add(sb.ToString());
						sb.Clear();
						sb.Append(ROMAJI_KOREAN_FIRST_CONSONANTS_DICT[first]);
						sb.Append("\t");
						sb.Append(ROMAJI_KOREAN_MIDDLE_VOWELS_DICT[middle]);
						sb.Append("\t");
						sb.Append(ROMAJI_KOREAN_LAST_CONSONANTS_DICT[last]);
						allRomajiHangeul.Add(sb.ToString());

					}
				}
			}

			if (allRomajiRomaji.Contains(romaji)) {
				string hangeul = allRomajiHangeul[allRomajiRomaji.IndexOf(romaji)];
				Hashtable separated = new Hashtable() {
					[0] = hangeul.Split("\t")[0].ToString(),
					[1] = hangeul.Split("\t")[1].ToString(),
					[2] = hangeul.Split("\t")[2].ToString()
				};
				string result = Merge(separated);
				Log.Debug("Korean Romaji Parsed: " + romaji + " -> " + result);
				return result;
			} else {
				return null;
			}

		}

		public static Hashtable Separate(string character) {

			int hangeulIndex; // unicode index of hangeul - unicode index of '가' (ex) '냥'

			int firstConsonantIndex; // (ex) 2
			int middleVowelIndex; // (ex) 2
			int lastConsonantIndex; // (ex) 21

			string firstConsonant; // (ex) "ㄴ"
			string middleVowel; // (ex) "ㅑ"
			string lastConsonant; // (ex) "ㅇ"

			Hashtable separatedHangeul; // (ex) {[0]: "ㄴ", [1]: "ㅑ", [2]: "ㅇ"}


			hangeulIndex = Convert.ToUInt16(character[0]) - HANGEUL_UNICODE_START;

			// seperates lastConsonant
			lastConsonantIndex = hangeulIndex % 28;
			hangeulIndex = (hangeulIndex - lastConsonantIndex) / 28;

			// seperates middleVowel
			middleVowelIndex = hangeulIndex % 21;
			hangeulIndex = (hangeulIndex - middleVowelIndex) / 21;

			// there's only firstConsonant now
			firstConsonantIndex = hangeulIndex;

			// separates character
			firstConsonant = FIRST_CONSONANTS[firstConsonantIndex].ToString();
			middleVowel = MIDDLE_VOWELS[middleVowelIndex].ToString();
			lastConsonant = LAST_CONSONANTS[lastConsonantIndex].ToString();

			separatedHangeul = new Hashtable() {
				[0] = firstConsonant,
				[1] = middleVowel,
				[2] = lastConsonant
			};


			return separatedHangeul;
		}

		public static string[] SeparateRomaji(string character) {
			try {
				string[] separatedCharacter = new string[0];
				foreach (var vowel in ROMAJI_KOREAN_MIDDLE_VOWELS_DICT.Keys) {
					if (character.Contains(vowel)) {
						// 예시를 기준으로 변수 part는 {"n", "ng"}
						var part = character.Split(vowel);
						if (!(part[1] == "")) { // 글자에 초성, 중성, 종성이 전부 있는 경우
							separatedCharacter = new string[] { part[0], vowel, part[1] };
						} else if (part == new string[] { "", "" }) { // 글자에 중성만 존재하는 경우
							separatedCharacter = new string[] { "", vowel, "" };
						} else if (part.Length == 2) { // 글자에 초성, 중성만 존재 하는 경우
							separatedCharacter = new string[] { part[0], vowel, "" };
						}
						break;
					}

					if (separatedCharacter.Length == 0) { // 무엇도 해당하지 않을경우 빈 문자열 3개만 담음
						separatedCharacter = new string[] { "", "", "" };
					}

					return separatedCharacter;
				}
			} catch (Exception e) {
				Log.Error(e, "SeparateRomaji Method Error!");
				return new string[] { "", "", "" };
			}

			return new string[] { "", "", "" };
		}

		public static string Merge(Hashtable separatedHangeul, int offset = 0) {

			int firstConsonantIndex; // (ex) 2
			int middleVowelIndex; // (ex) 2
			int lastConsonantIndex; // (ex) 21

			char firstConsonant = ((string)separatedHangeul[offset + 0])[0]; // (ex) "ㄴ"
			char middleVowel = ((string)separatedHangeul[offset + 1])[0]; // (ex) "ㅑ"
			char lastConsonant = ((string)separatedHangeul[offset + 2])[0]; // (ex) "ㅇ"

			if (firstConsonant == ' ') { firstConsonant = 'ㅇ'; }

			firstConsonantIndex = FIRST_CONSONANTS.IndexOf(firstConsonant); // 초성 인덱스
			middleVowelIndex = MIDDLE_VOWELS.IndexOf(middleVowel); // 중성 인덱스
			lastConsonantIndex = LAST_CONSONANTS.IndexOf(lastConsonant); // 종성 인덱스

			int mergedCode = HANGEUL_UNICODE_START + (firstConsonantIndex * 21 + middleVowelIndex) * 28 + lastConsonantIndex;

			string result = Convert.ToChar(mergedCode).ToString();
			//Debug.Print("Hangeul merged: " + $"{firstConsonant} + {middleVowel} + {lastConsonant} = " + result);
			return result;
		}

		private static Hashtable Variate(Hashtable firstCharSeparated, Hashtable nextCharSeparated, int returnCharIndex = -1) {

			string firstLastConsonant = (string)firstCharSeparated[2]; // 문래 에서 ㄴ, 맑다 에서 ㄺ
			string nextFirstConsonant = (string)nextCharSeparated[0]; // 문래 에서 ㄹ, 맑다 에서 ㄷ

			// 1. 연음 적용 + ㅎ탈락
			if ((!firstLastConsonant.Equals(" ")) && nextFirstConsonant.Equals("ㅎ")) {
				if (basicSounds.Contains(firstLastConsonant)) {
					// 착하다 = 차카다
					nextFirstConsonant = (string)aspirateSounds[basicSounds[firstLastConsonant]];
					firstLastConsonant = " ";
				} else {
					// 뻔한 = 뻔안 (아래에서 연음 적용되서 뻐난 됨)
					nextFirstConsonant = "ㅇ";
				}
			}

			if (nextFirstConsonant.Equals("ㅇ") && (!firstLastConsonant.Equals(" "))) {
				// ㄳ ㄵ ㄶ ㄺ ㄻ ㄼ ㄽ ㄾ ㄿ ㅀ ㅄ 일 경우에도 분기해서 연음 적용
				if (firstLastConsonant.Equals("ㄳ")) {
					firstLastConsonant = "ㄱ";
					nextFirstConsonant = "ㅅ";
				} else if (firstLastConsonant.Equals("ㄵ")) {
					firstLastConsonant = "ㄴ";
					nextFirstConsonant = "ㅈ";
				} else if (firstLastConsonant.Equals("ㄶ")) {
					firstLastConsonant = "ㄴ";
					nextFirstConsonant = "ㅎ";
				} else if (firstLastConsonant.Equals("ㄺ")) {
					firstLastConsonant = "ㄹ";
					nextFirstConsonant = "ㄱ";
				} else if (firstLastConsonant.Equals("ㄼ")) {
					firstLastConsonant = "ㄹ";
					nextFirstConsonant = "ㅂ";
				} else if (firstLastConsonant.Equals("ㄽ")) {
					firstLastConsonant = "ㄹ";
					nextFirstConsonant = "ㅅ";
				} else if (firstLastConsonant.Equals("ㄾ")) {
					firstLastConsonant = "ㄹ";
					nextFirstConsonant = "ㅌ";
				} else if (firstLastConsonant.Equals("ㄿ")) {
					firstLastConsonant = "ㄹ";
					nextFirstConsonant = "ㅍ";
				} else if (firstLastConsonant.Equals("ㅀ")) {
					firstLastConsonant = "ㄹ";
					nextFirstConsonant = "ㅎ";
				} else if (firstLastConsonant.Equals("ㅄ")) {
					firstLastConsonant = "ㅂ";
					nextFirstConsonant = "ㅅ";
				} else if (firstLastConsonant.Equals("ㄻ")) {
					firstLastConsonant = "ㄹ";
					nextFirstConsonant = "ㅁ";
				} else if (firstLastConsonant.Equals("ㅇ") && nextFirstConsonant.Equals("ㅇ")) {
					// Do nothing
				} else {
					// 겹받침 아닐 때 연음
					nextFirstConsonant = firstLastConsonant;
					firstLastConsonant = " ";
				}
			}


			// 1. 유기음화 및 ㅎ탈락 1
			if (firstLastConsonant.Equals("ㅎ") && (!nextFirstConsonant.Equals("ㅅ")) && basicSounds.Contains(nextFirstConsonant)) {
				// ㅎ으로 끝나고 다음 소리가 ㄱㄷㅂㅈ이면 / ex) 낳다 = 나타
				firstLastConsonant = " ";
				nextFirstConsonant = (string)aspirateSounds[basicSounds[nextFirstConsonant]];
			} else if (firstLastConsonant.Equals("ㅎ") && (!nextFirstConsonant.Equals("ㅅ")) && nextFirstConsonant.Equals("ㅇ")) {
				// ㅎ으로 끝나고 다음 소리가 없으면 / ex) 낳아 = 나아
				firstLastConsonant = " ";
			} else if (firstLastConsonant.Equals("ㄶ") && (!nextFirstConsonant.Equals("ㅅ")) && basicSounds.Contains(nextFirstConsonant)) {
				// ㄶ으로 끝나고 다음 소리가 ㄱㄷㅂㅈ이면 / ex) 많다 = 만타
				firstLastConsonant = "ㄴ";
				nextFirstConsonant = (string)aspirateSounds[basicSounds[nextFirstConsonant]];
			} else if (firstLastConsonant.Equals("ㅀ") && (!nextFirstConsonant.Equals("ㅅ")) && basicSounds.Contains(nextFirstConsonant)) {
				// ㅀ으로 끝나고 다음 소리가 ㄱㄷㅂㅈ이면 / ex) 끓다 = 끌타
				firstLastConsonant = "ㄹ";
				nextFirstConsonant = (string)aspirateSounds[basicSounds[nextFirstConsonant]];
			}




			// 2-1. 된소리되기 1
			if ((firstLastConsonant.Equals("ㄳ") || firstLastConsonant.Equals("ㄵ") || firstLastConsonant.Equals("ㄽ") || firstLastConsonant.Equals("ㄾ") || firstLastConsonant.Equals("ㅄ") || firstLastConsonant.Equals("ㄼ") || firstLastConsonant.Equals("ㄺ") || firstLastConsonant.Equals("ㄿ")) && basicSounds.Contains(nextFirstConsonant)) {
				// [ㄻ, (ㄶ, ㅀ)<= 유기음화에 따라 예외] 제외한 겹받침으로 끝나고 다음 소리가 예사소리이면
				nextFirstConsonant = (string)fortisSounds[basicSounds[nextFirstConsonant]];
			}

			// 3. 첫 번째 글자의 자음군단순화 및 평파열음화(음절의 끝소리 규칙)
			if (firstLastConsonant.Equals("ㄽ") || firstLastConsonant.Equals("ㄾ") || firstLastConsonant.Equals("ㄼ")) {
				firstLastConsonant = "ㄹ";
			} else if (firstLastConsonant.Equals("ㄵ") || firstLastConsonant.Equals("ㅅ") || firstLastConsonant.Equals("ㅆ") || firstLastConsonant.Equals("ㅈ") || firstLastConsonant.Equals("ㅉ") || firstLastConsonant.Equals("ㅊ") || firstLastConsonant.Equals("ㅌ")) {
				firstLastConsonant = "ㄷ";
			} else if (firstLastConsonant.Equals("ㅃ") || firstLastConsonant.Equals("ㅍ") || firstLastConsonant.Equals("ㄿ") || firstLastConsonant.Equals("ㅄ")) {
				firstLastConsonant = "ㅂ";
			} else if (firstLastConsonant.Equals("ㄲ") || firstLastConsonant.Equals("ㅋ") || firstLastConsonant.Equals("ㄺ") || firstLastConsonant.Equals("ㄳ")) {
				firstLastConsonant = "ㄱ";
			} else if (firstLastConsonant.Equals("ㄻ")) {
				firstLastConsonant = "ㅁ";
			}



			// 2-1. 된소리되기 2
			if (basicSounds.Contains(firstLastConsonant) && basicSounds.Contains(nextFirstConsonant)) {
				// 예사소리로 끝나고 다음 소리가 예사소리이면 / ex) 닭장 = 닥짱
				nextFirstConsonant = (string)fortisSounds[basicSounds[nextFirstConsonant]];
			}
			// else if ((firstLastConsonant.Equals("ㄹ")) && (basicSounds.Contains(nextFirstConsonant))){
			//     // ㄹ로 끝나고 다음 소리가 예사소리이면 / ex) 솔직 = 솔찍
			//     // 본래 관형형 어미 (으)ㄹ과 일부 한자어에서만 일어나는 변동이나, 워낙 사용되는 빈도가 많아서 기본으로 적용되게 해 두
			//     // 려 했으나 좀 아닌 것 같아서 보류하기로 함
			//     nextFirstConsonant = (string)fortisSounds[basicSounds[nextFirstConsonant]];
			// }

			// 1. 유기음화 2
			if (basicSounds.Contains(firstLastConsonant) && nextFirstConsonant.Equals("ㅎ")) {
				// ㄱㄷㅂㅈ(+ㅅ)로 끝나고 다음 소리가 ㅎ이면 / ex) 축하 = 추카, 옷하고 = 오타고
				// ㅅ은 미리 평파열음화가 진행된 것으로 보고 ㄷ으로 간주한다
				nextFirstConsonant = (string)aspirateSounds[basicSounds[firstLastConsonant]];
				firstLastConsonant = " ";
			} else if (nextFirstConsonant.Equals("ㅎ")) {
				nextFirstConsonant = "ㅇ";
			}

			if ((!firstLastConsonant.Equals(" ")) && nextFirstConsonant.Equals("ㅇ") && (!firstLastConsonant.Equals("ㅇ"))) {
				// 연음 2
				nextFirstConsonant = firstLastConsonant;
				firstLastConsonant = " ";
			}


			// 4. 비음화
			if (firstLastConsonant.Equals("ㄱ") && (!nextFirstConsonant.Equals("ㅇ")) && (nasalSounds.Contains(nextFirstConsonant) || nextFirstConsonant.Equals("ㄹ"))) {
				// ex) 막론 = 망론 >> 망논 
				firstLastConsonant = "ㅇ";
			} else if (firstLastConsonant.Equals("ㄷ") && (!nextFirstConsonant.Equals("ㅇ")) && (nasalSounds.Contains(nextFirstConsonant) || nextFirstConsonant.Equals("ㄹ"))) {
				// ex) 슬롯머신 = 슬론머신
				firstLastConsonant = "ㄴ";
			} else if (firstLastConsonant.Equals("ㅂ") && (!nextFirstConsonant.Equals("ㅇ")) && (nasalSounds.Contains(nextFirstConsonant) || nextFirstConsonant.Equals("ㄹ"))) {
				// ex) 밥먹자 = 밤먹자 >> 밤먹짜
				firstLastConsonant = "ㅁ";
			}

			// 4'. 유음화
			if (firstLastConsonant.Equals("ㄴ") && nextFirstConsonant.Equals("ㄹ")) {
				// ex) 만리 = 말리
				firstLastConsonant = "ㄹ";
			} else if (firstLastConsonant.Equals("ㄹ") && nextFirstConsonant.Equals("ㄴ")) {
				// ex) 칼날 = 칼랄
				nextFirstConsonant = "ㄹ";
			}

			// 4''. ㄹ비음화
			if (nextFirstConsonant.Equals("ㄹ") && nasalSounds.Contains(nextFirstConsonant)) {
				// ex) 담력 = 담녁
				firstLastConsonant = "ㄴ";
			}


			// 4'''. 자음동화
			if (firstLastConsonant.Equals("ㄴ") && nextFirstConsonant.Equals("ㄱ")) {
				// ex) ~라는 감정 = ~라능 감정
				firstLastConsonant = "ㅇ";
			}

			// return results
			if (returnCharIndex == 0) {
				// return result of first target character
				return new Hashtable() {
					[0] = firstCharSeparated[0],
					[1] = firstCharSeparated[1],
					[2] = firstLastConsonant
				};
			} else if (returnCharIndex == 1) {
				// return result of second target character
				return new Hashtable() {
					[0] = nextFirstConsonant,
					[1] = nextCharSeparated[1],
					[2] = nextCharSeparated[2]
				};
			} else {
				// 두 글자 다 반환
				return new Hashtable() {
					[0] = firstCharSeparated[0],
					[1] = firstCharSeparated[1],
					[2] = firstLastConsonant,
					[3] = nextFirstConsonant,
					[4] = nextCharSeparated[1],
					[5] = nextCharSeparated[2]
				};
			}
		}

		public static Hashtable Variate(string character) {
			Hashtable separated = Separate(character);

			if (separated[2].Equals("ㄽ") || separated[2].Equals("ㄾ") || separated[2].Equals("ㄼ") || separated[2].Equals("ㅀ")) {
				separated[2] = "ㄹ";
			} else if (separated[2].Equals("ㄵ") || separated[2].Equals("ㅅ") || separated[2].Equals("ㅆ") || separated[2].Equals("ㅈ") || separated[2].Equals("ㅉ") || separated[2].Equals("ㅊ")) {
				separated[2] = "ㄷ";
			} else if (separated[2].Equals("ㅃ") || separated[2].Equals("ㅍ") || separated[2].Equals("ㄿ") || separated[2].Equals("ㅄ")) {
				separated[2] = "ㅂ";
			} else if (separated[2].Equals("ㄲ") || separated[2].Equals("ㅋ") || separated[2].Equals("ㄺ") || separated[2].Equals("ㄳ")) {
				separated[2] = "ㄱ";
			} else if (separated[2].Equals("ㄻ")) {
				separated[2] = "ㅁ";
			} else if (separated[2].Equals("ㄶ")) {
				separated[2] = "ㄴ";
			}


			return separated;

		}
		private static Hashtable Variate(Hashtable separated) {

			if (separated[2].Equals("ㄽ") || separated[2].Equals("ㄾ") || separated[2].Equals("ㄼ") || separated[2].Equals("ㅀ")) {
				separated[2] = "ㄹ";
			} else if (separated[2].Equals("ㄵ") || separated[2].Equals("ㅅ") || separated[2].Equals("ㅆ") || separated[2].Equals("ㅈ") || separated[2].Equals("ㅉ") || separated[2].Equals("ㅊ")) {
				separated[2] = "ㄷ";
			} else if (separated[2].Equals("ㅃ") || separated[2].Equals("ㅍ") || separated[2].Equals("ㄿ") || separated[2].Equals("ㅄ")) {
				separated[2] = "ㅂ";
			} else if (separated[2].Equals("ㄲ") || separated[2].Equals("ㅋ") || separated[2].Equals("ㄺ") || separated[2].Equals("ㄳ")) {
				separated[2] = "ㄱ";
			} else if (separated[2].Equals("ㄻ")) {
				separated[2] = "ㅁ";
			} else if (separated[2].Equals("ㄶ")) {
				separated[2] = "ㄴ";
			}

			return separated;
		}

		private static Hashtable Variate(string firstChar, string nextChar, int returnCharIndex = 0) {
			// 글자 넣어도 쓸 수 있음

			Hashtable firstCharSeparated = Separate(firstChar);
			Hashtable nextCharSeparated = Separate(nextChar);
			return Variate(firstCharSeparated, nextCharSeparated, returnCharIndex);
		}

		public static Hashtable Variate(Note? prevNeighbour, Note note, Note? nextNeighbour) {
			// prevNeighbour와 note와 nextNeighbour의 음원변동된 가사를 반환
			// prevNeighbour : VV 정렬에 사용
			// nextNeighbour : VC 정렬에 사용
			// 뒤의 노트가 없으면 리턴되는 값의 6~8번 인덱스가 null로 채워진다.

			int whereYeonEum = -1;

			string?[] lyrics = new string?[] { prevNeighbour?.lyric, note.lyric, nextNeighbour?.lyric };

			if (!IsHangeul(lyrics[0])) {
				// 앞노트 한국어 아니거나 null일 경우 null처리
				if (lyrics[0] != null) { lyrics[0] = null; }
			} else if (!IsHangeul(lyrics[2])) {
				// 뒤노트 한국어 아니거나 null일 경우 null처리
				if (lyrics[2] != null) { lyrics[2] = null; }
			}
			if ((lyrics[0] != null) && lyrics[0].StartsWith('!')) {
				if (lyrics[0] != null) { lyrics[0] = null; } // 0번가사 없는 걸로 간주함 null냥냥
			}
			if ((lyrics[1] != null) && lyrics[1].StartsWith('!')) {
				lyrics[1] = lyrics[1].TrimStart('!');
				if (lyrics[0] != null) { lyrics[0] = null; } // 0번가사 없는 걸로 간주함 null[!냥]냥
				if (lyrics[2] != null) { lyrics[2] = null; } // 2번가사도 없는 걸로 간주함 null[!냥]null
			}
			if ((lyrics[2] != null) && lyrics[2].StartsWith('!')) {
				if (lyrics[2] != null) { lyrics[2] = null; } // 2번가사 없는 걸로 간주함 냥냥b
			}

			if ((lyrics[0] != null) && lyrics[0].EndsWith('.')) {
				lyrics[0] = lyrics[0].TrimEnd('.');
				whereYeonEum = 0;
			}
			if ((lyrics[1] != null) && lyrics[1].EndsWith('.')) {
				lyrics[1] = lyrics[1].TrimEnd('.');
				whereYeonEum = 1;
			}
			if ((lyrics[2] != null) && lyrics[2].EndsWith('.')) {
				lyrics[2] = lyrics[2].TrimEnd('.');
			}

			// 음운변동 적용 --
			if ((lyrics[0] == null) && (lyrics[2] != null)) {
				if (whereYeonEum == 1) {
					// 현재 노트에서 단어가 끝났다고 가정
					Hashtable result = new Hashtable() {
						[0] = "null", // 앞 글자 없음
						[1] = "null",
						[2] = "null"
					};
					Hashtable thisNoteSeparated = Variate(Variate(lyrics[1]), Separate(lyrics[2]), -1); // 현 글자 / 끝글자처럼 음운변동시켜서 음원변동 한 번 더 하기

					result.Add(3, thisNoteSeparated[0]); // 현 글자
					result.Add(4, thisNoteSeparated[1]);
					result.Add(5, thisNoteSeparated[2]);

					result.Add(6, thisNoteSeparated[3]); // 뒤 글자
					result.Add(7, thisNoteSeparated[4]);
					result.Add(8, thisNoteSeparated[5]);

					return result;
				} else {
					Hashtable result = new Hashtable() {
						[0] = "null", // 앞 글자 없음
						[1] = "null",
						[2] = "null"
					};

					if (IsHangeul(lyrics[2])) {
						Hashtable thisNoteSeparated = Variate(lyrics[1], lyrics[2], -1); // 현글자 뒤글자

						result.Add(3, thisNoteSeparated[0]); // 현 글자
						result.Add(4, thisNoteSeparated[1]);
						result.Add(5, thisNoteSeparated[2]);

						result.Add(6, thisNoteSeparated[3]);
						result.Add(7, thisNoteSeparated[4]);
						result.Add(8, thisNoteSeparated[5]);
					} else {
						Hashtable thisNoteSeparated = Variate(lyrics[1]);
						result.Add(3, thisNoteSeparated[0]); // 현 글자
						result.Add(4, thisNoteSeparated[1]);
						result.Add(5, thisNoteSeparated[2]);

						result.Add(6, "null");
						result.Add(7, "null");
						result.Add(8, "null");
					}


					return result;
				}
			} else if ((lyrics[0] != null) && (lyrics[2] == null)) {
				if (whereYeonEum == 1) {
					// 현재 노트에서 단어가 끝났다고 가정
					Hashtable result = Variate(Separate(lyrics[0]), Variate(lyrics[1]), 0); // 첫 글자
					Hashtable thisNoteSeparated = Variate(Variate(Separate(lyrics[0]), Variate(lyrics[1]), 1)); // 현 글자 / 끝글자처럼 음운변동시켜서 음원변동 한 번 더 하기

					result.Add(3, thisNoteSeparated[0]); // 현 글자
					result.Add(4, thisNoteSeparated[1]);
					result.Add(5, thisNoteSeparated[2]);

					result.Add(6, "null"); // 뒤 글자 없음
					result.Add(7, "null");
					result.Add(8, "null");

					return result;
				} else if (whereYeonEum == 0) {
					// 앞 노트에서 단어가 끝났다고 가정 
					Hashtable result = Variate(Variate(lyrics[0]), Separate(lyrics[1]), 0); // 첫 글자
					Hashtable thisNoteSeparated = Variate(Variate(Variate(lyrics[0]), Separate(lyrics[1]), 1)); // 첫 글자와 현 글자 / 앞글자를 끝글자처럼 음운변동시켜서 음원변동 한 번 더 하기

					result.Add(3, thisNoteSeparated[0]); // 현 글자
					result.Add(4, thisNoteSeparated[1]);
					result.Add(5, thisNoteSeparated[2]);

					result.Add(6, "null"); // 뒤 글자 없음
					result.Add(7, "null");
					result.Add(8, "null");

					return result;
				} else {
					Hashtable result = Variate(lyrics[0], lyrics[1], 0); // 첫 글자
					Hashtable thisNoteSeparated = Variate(Variate(lyrics[0], lyrics[1], 1)); // 첫 글자와 현 글자 / 뒷글자 없으니까 글자 혼자 있는걸로 음운변동 한 번 더 시키기

					result.Add(3, thisNoteSeparated[0]); // 현 글자
					result.Add(4, thisNoteSeparated[1]);
					result.Add(5, thisNoteSeparated[2]);

					result.Add(6, "null"); // 뒤 글자 없음
					result.Add(7, "null");
					result.Add(8, "null");

					return result;
				}
			} else if ((lyrics[0] != null) && (lyrics[2] != null)) {
				if (whereYeonEum == 1) {
					// 현재 노트에서 단어가 끝났다고 가정 / 무 [릎.] 위
					Hashtable result = Variate(Separate(lyrics[0]), Variate(lyrics[1]), 1); // 첫 글자
					Hashtable thisNoteSeparated = Variate(Variate(Separate(lyrics[0]), Variate(lyrics[1]), 1), Separate(lyrics[2]), -1);// 현글자와 다음 글자 / 현 글자를 끝글자처럼 음운변동시켜서 음원변동 한 번 더 하기

					result.Add(3, thisNoteSeparated[0]); // 현 글자
					result.Add(4, thisNoteSeparated[1]);
					result.Add(5, thisNoteSeparated[2]);

					result.Add(6, thisNoteSeparated[3]); // 뒤 글자
					result.Add(7, thisNoteSeparated[4]);
					result.Add(8, thisNoteSeparated[5]);

					return result;
				} else if (whereYeonEum == 0) {
					// 앞 노트에서 단어가 끝났다고 가정 / 릎. [위] 놓
					Hashtable result = Variate(Variate(lyrics[0]), Separate(lyrics[1]), 0); // 첫 글자
					Hashtable thisNoteSeparated = Variate(Variate(Variate(lyrics[0]), Separate(lyrics[1]), 1), Separate(lyrics[2]), -1); // 현 글자와 뒤 글자 / 앞글자 끝글자처럼 음운변동시켜서 음원변동 한 번 더 하기

					result.Add(3, thisNoteSeparated[0]); // 현 글자
					result.Add(4, thisNoteSeparated[1]);
					result.Add(5, thisNoteSeparated[2]);

					result.Add(6, thisNoteSeparated[3]); // 뒤 글자
					result.Add(7, thisNoteSeparated[4]);
					result.Add(8, thisNoteSeparated[5]);

					return result;
				} else {
					Hashtable result = Variate(lyrics[0], lyrics[1], 0);
					Hashtable thisNoteSeparated = Variate(Variate(lyrics[0], lyrics[1], 1), Separate(lyrics[2]), -1);

					result.Add(3, thisNoteSeparated[0]); // 현 글자
					result.Add(4, thisNoteSeparated[1]);
					result.Add(5, thisNoteSeparated[2]);

					result.Add(6, thisNoteSeparated[3]); // 뒤 글자
					result.Add(7, thisNoteSeparated[4]);
					result.Add(8, thisNoteSeparated[5]);

					return result;
				}
			} else {

				Hashtable result = new Hashtable() {
					// 첫 글자 >> 비어 있음
					[0] = "null",
					[1] = "null",
					[2] = "null"
				};

				Hashtable thisNoteSeparated = Variate(lyrics[1]); // 현 글자

				result.Add(3, thisNoteSeparated[0]); // 현 글자
				result.Add(4, thisNoteSeparated[1]);
				result.Add(5, thisNoteSeparated[2]);


				result.Add(6, "null"); // 뒤 글자 비어있음
				result.Add(7, "null");
				result.Add(8, "null");

				return result;
			}
		}

		public static String Variate(String? prevNeighbour, String note, String? nextNeighbour) {
			// prevNeighbour와 note와 nextNeighbour의 음원변동된 가사를 반환
			// prevNeighbour : VV 정렬에 사용
			// nextNeighbour : VC 정렬에 사용
			// 뒤의 노트가 없으면 리턴되는 값의 6~8번 인덱스가 null로 채워진다.

			int whereYeonEum = -1;

			string?[] lyrics = new string?[] { prevNeighbour, note, nextNeighbour };

			if (!IsHangeul(lyrics[0])) {
				// 앞노트 한국어 아니거나 null일 경우 null처리
				if (lyrics[0] != null) { lyrics[0] = null; }
			} else if (!IsHangeul(lyrics[2])) {
				// 뒤노트 한국어 아니거나 null일 경우 null처리
				if (lyrics[2] != null) { lyrics[2] = null; }
			}
			if ((lyrics[0] != null) && lyrics[0].StartsWith('!')) {
				if (lyrics[0] != null) { lyrics[0] = null; } // 0번가사 없는 걸로 간주함 null냥냥
			}
			if ((lyrics[1] != null) && lyrics[1].StartsWith('!')) {
				lyrics[1] = lyrics[1].TrimStart('!');
				if (lyrics[0] != null) { lyrics[0] = null; } // 0번가사 없는 걸로 간주함 null[!냥]냥
				if (lyrics[2] != null) { lyrics[2] = null; } // 2번가사도 없는 걸로 간주함 null[!냥]null
			}
			if ((lyrics[2] != null) && lyrics[2].StartsWith('!')) {
				if (lyrics[2] != null) { lyrics[2] = null; } // 2번가사 없는 걸로 간주함 냥냥b
			}

			if ((lyrics[0] != null) && lyrics[0].EndsWith('.')) {
				lyrics[0] = lyrics[0].TrimEnd('.');
				whereYeonEum = 0;
			}
			if ((lyrics[1] != null) && lyrics[1].EndsWith('.')) {
				lyrics[1] = lyrics[1].TrimEnd('.');
				whereYeonEum = 1;
			}
			if ((lyrics[2] != null) && lyrics[2].EndsWith('.')) {
				lyrics[2] = lyrics[2].TrimEnd('.');
			}

			// 음운변동 적용 --
			if ((lyrics[0] == null) && (lyrics[2] != null)) {
				if (whereYeonEum == 1) {
					// 현재 노트에서 단어가 끝났다고 가정
					Hashtable result = new Hashtable() {
						[0] = "null", // 앞 글자 없음
						[1] = "null",
						[2] = "null"
					};
					Hashtable thisNoteSeparated = Variate(Variate(lyrics[1]), Separate(lyrics[2]), -1); // 현 글자 / 끝글자처럼 음운변동시켜서 음원변동 한 번 더 하기

					result.Add(3, thisNoteSeparated[0]); // 현 글자
					result.Add(4, thisNoteSeparated[1]);
					result.Add(5, thisNoteSeparated[2]);

					result.Add(6, thisNoteSeparated[3]); // 뒤 글자
					result.Add(7, thisNoteSeparated[4]);
					result.Add(8, thisNoteSeparated[5]);

					return Merge(new Hashtable {
						[0] = (string)result[3],
						[1] = (string)result[4],
						[2] = (string)result[5]
					});
				} else {
					Hashtable result = new Hashtable() {
						[0] = "null", // 앞 글자 없음
						[1] = "null",
						[2] = "null"
					};

					Hashtable thisNoteSeparated = Variate(lyrics[1], lyrics[2], -1); // 현글자 뒤글자

					result.Add(3, thisNoteSeparated[0]); // 현 글자
					result.Add(4, thisNoteSeparated[1]);
					result.Add(5, thisNoteSeparated[2]);

					result.Add(6, thisNoteSeparated[3]); // 뒤 글자 없음
					result.Add(7, thisNoteSeparated[4]);
					result.Add(8, thisNoteSeparated[5]);

					return Merge(result, 3);
				}
			} else if ((lyrics[0] != null) && (lyrics[2] == null)) {
				if (whereYeonEum == 1) {
					// 현재 노트에서 단어가 끝났다고 가정
					Hashtable result = Variate(Separate(lyrics[0]), Variate(lyrics[1]), 0); // 첫 글자
					Hashtable thisNoteSeparated = Variate(Variate(Separate(lyrics[0]), Variate(lyrics[1]), 1)); // 현 글자 / 끝글자처럼 음운변동시켜서 음원변동 한 번 더 하기

					result.Add(3, thisNoteSeparated[0]); // 현 글자
					result.Add(4, thisNoteSeparated[1]);
					result.Add(5, thisNoteSeparated[2]);

					result.Add(6, "null"); // 뒤 글자 없음
					result.Add(7, "null");
					result.Add(8, "null");

					return Merge(result, 3);
				} else if (whereYeonEum == 0) {
					// 앞 노트에서 단어가 끝났다고 가정 
					Hashtable result = Variate(Variate(lyrics[0]), Separate(lyrics[1]), 0); // 첫 글자
					Hashtable thisNoteSeparated = Variate(Variate(Variate(lyrics[0]), Separate(lyrics[1]), 1)); // 첫 글자와 현 글자 / 앞글자를 끝글자처럼 음운변동시켜서 음원변동 한 번 더 하기

					result.Add(3, thisNoteSeparated[0]); // 현 글자
					result.Add(4, thisNoteSeparated[1]);
					result.Add(5, thisNoteSeparated[2]);

					result.Add(6, "null"); // 뒤 글자 없음
					result.Add(7, "null");
					result.Add(8, "null");

					return Merge(result, 3);
				} else {
					Hashtable result = Variate(lyrics[0], lyrics[1], 0); // 첫 글자
					Hashtable thisNoteSeparated = Variate(Variate(lyrics[0], lyrics[1], 1)); // 첫 글자와 현 글자 / 뒷글자 없으니까 글자 혼자 있는걸로 음운변동 한 번 더 시키기

					result.Add(3, thisNoteSeparated[0]); // 현 글자
					result.Add(4, thisNoteSeparated[1]);
					result.Add(5, thisNoteSeparated[2]);

					result.Add(6, "null"); // 뒤 글자 없음
					result.Add(7, "null");
					result.Add(8, "null");

					return Merge(result, 3);
				}
			} else if ((lyrics[0] != null) && (lyrics[2] != null)) {
				if (whereYeonEum == 1) {
					// 현재 노트에서 단어가 끝났다고 가정 / 무 [릎.] 위
					Hashtable result = Variate(Separate(lyrics[0]), Variate(lyrics[1]), 1); // 첫 글자
					Hashtable thisNoteSeparated = Variate(Variate(Separate(lyrics[0]), Variate(lyrics[1]), 1), Separate(lyrics[2]), -1);// 현글자와 다음 글자 / 현 글자를 끝글자처럼 음운변동시켜서 음원변동 한 번 더 하기

					result.Add(3, thisNoteSeparated[0]); // 현 글자
					result.Add(4, thisNoteSeparated[1]);
					result.Add(5, thisNoteSeparated[2]);

					result.Add(6, thisNoteSeparated[3]); // 뒤 글자
					result.Add(7, thisNoteSeparated[4]);
					result.Add(8, thisNoteSeparated[5]);

					return Merge(result, 3);
				} else if (whereYeonEum == 0) {
					// 앞 노트에서 단어가 끝났다고 가정 / 릎. [위] 놓
					Hashtable result = Variate(Variate(lyrics[0]), Separate(lyrics[1]), 0); // 첫 글자
					Hashtable thisNoteSeparated = Variate(Variate(Variate(lyrics[0]), Separate(lyrics[1]), 1), Separate(lyrics[2]), -1); // 현 글자와 뒤 글자 / 앞글자 끝글자처럼 음운변동시켜서 음원변동 한 번 더 하기

					result.Add(3, thisNoteSeparated[0]); // 현 글자
					result.Add(4, thisNoteSeparated[1]);
					result.Add(5, thisNoteSeparated[2]);

					result.Add(6, thisNoteSeparated[3]); // 뒤 글자
					result.Add(7, thisNoteSeparated[4]);
					result.Add(8, thisNoteSeparated[5]);

					return Merge(result, 3);
				} else {
					Hashtable result = Variate(lyrics[0], lyrics[1], 0);
					Hashtable thisNoteSeparated = Variate(Variate(lyrics[0], lyrics[1], 1), Separate(lyrics[2]), -1);

					result.Add(3, thisNoteSeparated[0]); // 현 글자
					result.Add(4, thisNoteSeparated[1]);
					result.Add(5, thisNoteSeparated[2]);

					result.Add(6, thisNoteSeparated[3]); // 뒤 글자
					result.Add(7, thisNoteSeparated[4]);
					result.Add(8, thisNoteSeparated[5]);

					return Merge(result, 3);
				}
			} else {
				Hashtable result = new Hashtable() {
					// 첫 글자 >> 비어 있음
					[0] = "null",
					[1] = "null",
					[2] = "null"
				};

				Hashtable thisNoteSeparated = Variate(lyrics[1]); // 현 글자

				result.Add(3, thisNoteSeparated[0]); // 현 글자
				result.Add(4, thisNoteSeparated[1]);
				result.Add(5, thisNoteSeparated[2]);


				result.Add(6, "null"); // 뒤 글자 비어있음
				result.Add(7, "null");
				result.Add(8, "null");

				return Merge(result, 3);
			}
		}

		public static Note[] ChangeLyric(Note[] group, string lyric) {
			// for ENUNU Phonemizer
			var oldNote = group[0];
			group[0] = new Note {
				lyric = lyric,
				phoneticHint = oldNote.phoneticHint,
				tone = oldNote.tone,
				position = oldNote.position,
				duration = oldNote.duration,
				phonemeAttributes = oldNote.phonemeAttributes,
			};
			return group;
		}

		public static void ModifyLyrics(Hashtable lyricSeparated, string lyric, Dictionary<string, string[]> firstConsonants, Dictionary<string, string[]> vowels, Dictionary<string, string[]> lastConsonants, string semivowelSeparator) {
			lyric += firstConsonants[(string)lyricSeparated[3]][0];
			if (vowels[(string)lyricSeparated[4]][1] != "") {
				// this vowel contains semivowel
				lyric += semivowelSeparator + vowels[(string)lyricSeparated[4]][1] + vowels[(string)lyricSeparated[4]][2];
			} else {
				lyric += " " + vowels[(string)lyricSeparated[4]][2];
			}

			lyric += lastConsonants[(string)lyricSeparated[5]][0];
		}

		public static void RomanizeNotes(Note[][] groups, bool _modify_lyrics = false, Dictionary<string, string[]> firstConsonants = null, Dictionary<string, string[]> vowels = null, Dictionary<string, string[]> lastConsonants = null, string semivowelSeparator = " ") {
			// for ENUNU & DIFFS Phonemizer

			int noteIdx = 0;
			string lyric;
			bool modifyLyrics = (!_modify_lyrics || firstConsonants == null || vowels == null || lastConsonants == null) ? false : true;

			Note[] currentNote;
			Note[]? prevNote = null;
			Note[]? nextNote;

			Note? prevNote_;
			Note? nextNote_;

			List<string> ResultLyrics = new List<string>();

			foreach (Note[] group in groups) {
				currentNote = groups[noteIdx];
				string originalLyric; // uses this when no variation needed
				originalLyric = currentNote[0].lyric;

				if (groups.Length > noteIdx + 1 && IsHangeul(groups[noteIdx + 1][0].lyric)) {
					nextNote = groups[noteIdx + 1];
				} else {
					nextNote = null;
				}

				if (prevNote != null) {
					prevNote_ = prevNote[0];
					if (prevNote[0].position + prevNote.Sum(note => note.duration) != currentNote[0].position) {
						prevNote_ = null;
					}
				} else { prevNote_ = null; }

				if (nextNote != null) {
					nextNote_ = nextNote[0];

					if (nextNote[0].position != currentNote[0].position + currentNote.Sum(note => note.duration)) {
						nextNote_ = null;
					}
				} else { nextNote_ = null; }

				lyric = originalLyric;

				if (!IsHangeul(currentNote[0].lyric)) {
					ResultLyrics.Add(currentNote[0].lyric);
					prevNote = currentNote;
					noteIdx++;
					continue;
				}


				Hashtable lyricSeparated = Variate(prevNote_, currentNote[0], nextNote_);

				if (modifyLyrics) {
					ModifyLyrics(lyricSeparated, lyric, firstConsonants, vowels, lastConsonants, semivowelSeparator);
				} else {
					lyric = Merge(lyricSeparated, 3);
				}

				ResultLyrics.Add(lyric.Trim());

				prevNote = currentNote;

				noteIdx++;

			}
			Enumerable.Zip(groups, ResultLyrics.ToArray(), ChangeLyric).Last();
		}


		public abstract class BaseIniManager {
			protected USinger singer;
			protected Hashtable iniSetting = new Hashtable();
			protected string iniFileName;
			protected string filePath;
			protected List<IniBlock> blocks;

			public BaseIniManager() { }

			public void Initialize(USinger singer, string iniFileName, Hashtable defaultIniSetting) {
				this.singer = singer;
				this.iniFileName = iniFileName;
				iniSetting = defaultIniSetting;
				filePath = Path.Combine(singer.Location, iniFileName);
				try {
					using (StreamReader reader = new StreamReader(filePath, Encoding.UTF8)) {
						List<IniBlock> blocks = Ini.ReadBlocks(reader, filePath, @"\[\w+\]");
						if (blocks.Count == 0) {
							throw new IOException($"[{iniFileName}] is empty.");
						}
						this.blocks = blocks;
						IniSetUp(iniSetting); // you can override IniSetUp() to use.
					};
				} catch (IOException e) {
					Log.Error(e, $"failed to read {iniFileName}, Making new {iniFileName}...");
					using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8)) {
						iniSetting = defaultIniSetting;
						try {
							writer.Write(ConvertSettingsToString());
							writer.Close();
						} catch (IOException e_) {
							Log.Error(e_, $"[{iniFileName}] Failed to Write new {iniFileName}.");
						}
					};
					using (StreamReader reader = new StreamReader(filePath, Encoding.UTF8)) {
						List<IniBlock> blocks = Ini.ReadBlocks(reader, filePath, @"\[\w+\]");
						this.blocks = blocks;
					};
				}
			}

			protected virtual void IniSetUp(Hashtable iniSetting) {
			}

			protected string ConvertSettingsToString() {
				string result = "";
				foreach (DictionaryEntry section in iniSetting) {
					result += $"[{section.Key}]\n";
					foreach (DictionaryEntry key in (Hashtable)iniSetting[section.Key]) {
						result += $"{key.Key}={key.Value}\n";
					}
				}
				return result;
			}
			protected void SetOrReadThisValue(string sectionName, string keyName, bool defaultValue, out bool resultValue) {
				List<IniLine> iniLines = blocks.Find(block => block.header == $"[{sectionName}]").lines;
				if (!iniSetting.ContainsKey(sectionName)) {
					iniSetting.Add(sectionName, new Hashtable());
				}
				if (iniLines != null) {
					string result = iniLines.Find(l => l.line.Trim().Split("=")[0] == keyName).line.Trim().Split("=")[1];
					if (result != null) {
						try {
							((Hashtable)iniSetting[sectionName]).Add(keyName, result);
						} catch (ArgumentException) {
							((Hashtable)iniSetting[sectionName])[keyName] = result;
						}

						resultValue = result.ToLower() == "true" ? true : false;
					} else {
						try {
							((Hashtable)iniSetting[sectionName]).Add(keyName, defaultValue.ToString());
						} catch (ArgumentException) {
							((Hashtable)iniSetting[sectionName])[keyName] = defaultValue.ToString();
						}
						resultValue = defaultValue;
					}
				} else {
					using (StreamWriter writer = new StreamWriter(filePath)) {
						((Hashtable)iniSetting[sectionName]).Add(keyName, defaultValue.ToString().ToLower());
						resultValue = defaultValue;
						try {
							writer.Write(ConvertSettingsToString());
						} catch (IOException e) {
							Log.Error(e, $"[{iniFileName}] Failed to Write new {iniFileName}.");
						}

						Log.Information($"[{iniFileName}] failed to parse setting '{keyName}', modified {defaultValue} as default value.");
					};
				}
			}

			protected string SetOrReadThisValue(string sectionName, string keyName, string defaultValue) {
				string resultValue;
				List<IniLine> iniLines = blocks.Find(block => block.header == $"[{sectionName}]").lines;
				if (!iniSetting.ContainsKey(sectionName)) {
					iniSetting.Add(sectionName, new Hashtable());
				}
				if (iniLines != null) {
					string result = iniLines.Find(l => l.line.Trim().Split("=")[0] == keyName).line.Trim().Split("=")[1];
					if (result != null) {
						try {
							((Hashtable)iniSetting[sectionName]).Add(keyName, result);
						} catch (ArgumentException) {
							((Hashtable)iniSetting[sectionName])[keyName] = result;
						}
						resultValue = result;
					} else {
						try {
							((Hashtable)iniSetting[sectionName]).Add(keyName, defaultValue);
						} catch (ArgumentException) {
							((Hashtable)iniSetting[sectionName])[keyName] = defaultValue;
						}
						resultValue = defaultValue;
					}
				} else {
					StreamWriter writer = new StreamWriter(filePath);
					((Hashtable)iniSetting[sectionName]).Add(keyName, defaultValue);
					resultValue = defaultValue;
					try {
						writer.Write(ConvertSettingsToString());
						writer.Close();
					} catch (IOException e) {
						Log.Error(e, $"[{iniFileName}] Failed to Write new {iniFileName}.");
					}
					Log.Information($"[{iniFileName}] failed to parse setting '{keyName}', modified {defaultValue} as default value.");
				}
				return resultValue;
			}

			protected void SetOrReadThisValue(string sectionName, string keyName, int defaultValue, out int resultValue) {
				List<IniLine> iniLines = blocks.Find(block => block.header == $"[{sectionName}]").lines;
				if (!iniSetting.ContainsKey(sectionName)) {
					iniSetting.Add(sectionName, new Hashtable());
				}
				if (iniLines != null) {
					string result = iniLines.Find(l => l.line.Trim().Split("=")[0] == keyName).line.Trim().Split("=")[1];
					if (result != null && int.TryParse(result, out var resultInt)) {
						try {
							((Hashtable)iniSetting[sectionName]).Add(keyName, result);
						} catch (ArgumentException) {
							((Hashtable)iniSetting[sectionName])[keyName] = result;
						}
						resultValue = resultInt;
					} else {
						try {
							((Hashtable)iniSetting[sectionName]).Add(keyName, defaultValue.ToString());
						} catch (ArgumentException) {
							((Hashtable)iniSetting[sectionName])[keyName] = defaultValue.ToString();
						}
						resultValue = defaultValue;
					}
				} else {
					StreamWriter writer = new StreamWriter(filePath);
					((Hashtable)iniSetting[sectionName]).Add(keyName, defaultValue);
					resultValue = defaultValue;
					try {
						writer.Write(ConvertSettingsToString());
						writer.Close();
					} catch (IOException e) {
						Log.Error(e, $"[{iniFileName}] Failed to Write new {iniFileName}.");
					}
					Log.Information($"[{iniFileName}] failed to parse setting '{keyName}', modified {defaultValue} as default value.");
				}
			}
		}
		public class JamoDictionary {
			public FirstConsonantData[] firstConsonants;
			public PlainVowelData[] plainVowels;
			public SemivowelData[] semivowels;
			public FinalConsonantData[] finalConsonants;
			public JamoDictionary() { }
			public JamoDictionary(FirstConsonantData[] firstConsonants, PlainVowelData[] plainVowels, SemivowelData[] semivowels, FinalConsonantData[] finalConsonants) {
				this.firstConsonants = firstConsonants;
				this.plainVowels = plainVowels;
				this.semivowels = semivowels;
				this.finalConsonants = finalConsonants;
			}
			public struct FirstConsonantData {
				public string grapheme; // ㄱ
				public string phoneme; // g
				public FirstConsonantData(string grapheme, string phoneme) {
					this.grapheme = grapheme;
					this.phoneme = phoneme;
				}
			}

			public struct PlainVowelData {
				public string grapheme; // ㅏ
				public string phoneme; // a

				public PlainVowelData(string grapheme, string phoneme) {
					this.grapheme = grapheme;
					this.phoneme = phoneme;
				}
			}
			public struct SemivowelData {
				public string grapheme; // w
				public string phoneme; // w

				public SemivowelData(string grapheme, string phoneme) {
					this.grapheme = grapheme;
					this.phoneme = phoneme;
				}
			}

			public struct FinalConsonantData {
				public string grapheme; // ㄱ
				public string phoneme; // K
				public FinalConsonantData(string grapheme, string phoneme) {
					this.grapheme = grapheme;
					this.phoneme = phoneme;
				}
			}
		}
	}

}
