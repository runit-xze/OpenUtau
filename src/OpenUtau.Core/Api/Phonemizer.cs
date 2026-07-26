using System;
using System.Collections.Generic;
using System.Globalization;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Api {
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class PhonemizerAttribute : Attribute {
		public string Name { get; private set; }
		public string Tag { get; private set; }
		public string Author { get; private set; }
		public string Language { get; private set; }
		public string Engine { get; private set; }

		public PhonemizerAttribute(string name, string tag, string author = null, string language = null, string engine = null) {
			Name = name;
			Tag = tag;
			Author = author;
			Language = language;
			Engine = engine;
		}
	}

	public abstract class Phonemizer {
		public struct Note {
			public string lyric;

			public string phoneticHint;

			public int tone;

			public int position;

			public int duration;

			public PhonemeAttributes[] phonemeAttributes;

			public override string ToString() => $"\"{lyric}\" pos:{position}";
		}

		public struct PhonemeAttributes {
			public int index;
			public double? consonantStretchRatio;
			public int? toneShift;
			public int? alternate;
			public string? voiceColor;
		}

		public struct PhonemeExpression {
			public string abbr;
			public float value;
		}

		public struct Phoneme {
			public int? index;

			public string phoneme;

			public int position;

			public Exception? error;

			public List<PhonemeExpression> expressions;

			public override string ToString() => $"\"{phoneme}\" pos:{position}";
		}

		public struct Result {
			public Phoneme[] phonemes;
		}

		public string Name { get; set; }
		public string Tag { get; set; }
		public string Language { get; set; }
		public string Engine { get; set; }
		internal Exception? SetUpException { get; set; }

		protected double bpm;
		protected TimeAxis timeAxis;

		public abstract void SetSinger(USinger singer);

		public virtual bool LegacyMapping => false;

		public UProject? project;
		public UTrack? track;

		public virtual void SetUp(Note[][] notes, UProject project, UTrack track) {
			this.project = project;
			this.track = track;
		}

		public abstract Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevs);

		public virtual void CleanUp() { }

		public override string ToString() => $"[{Tag}] {Name}";

		public void SetTiming(TimeAxis timeAxis) {
			this.timeAxis = timeAxis;
			bpm = timeAxis.GetBpmAtTick(0);
		}

		public string DictionariesPath => PathManager.Inst.DictionariesPath;
		public string PluginDir => PathManager.Inst.PluginsPath;

		[Obsolete] // TODO: update usages
		protected double TickToMs(int tick) {
			return timeAxis.TickPosToMsPos(tick);
		}

		[Obsolete] // TODO: update usages
		protected int MsToTick(double ms) {
			return timeAxis.MsPosToTickPos(ms);
		}

		public static IList<string> ToUnicodeElements(string lyric) {
			var result = new List<string>();
			var etor = StringInfo.GetTextElementEnumerator(lyric);
			while (etor.MoveNext()) {
				result.Add(etor.GetTextElement());
			}
			return result;
		}

		public bool Testing { get; set; } = false;

		protected void OnAsyncInitStarted() {
			if (!Testing) {
				DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, "Initializing phonemizer..."));
			}
		}

		protected void OnAsyncInitFinished() {
			if (!Testing) {
				DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, ""));
				DocManager.Inst.ExecuteCmd(new ValidateProjectNotification());
				DocManager.Inst.ExecuteCmd(new PreRenderNotification());
			}
		}

		protected Result MakeSimpleResult(string phoneme) {
			return new Result() {
				phonemes = new Phoneme[] {
					new Phoneme() {
						phoneme = phoneme
					}
				}
			};
		}

		public double GetParentConsonantStretchRatio() {
			if (project != null && track != null) {
				if (track.TryGetExpDescriptor(project, Core.Format.Ustx.VEL, out var trackVEL)) {
					return Math.Pow(2, 1.0 - trackVEL.CustomDefaultValue / 100.0);
				}
			}
			return 1;
		}

		public int GetParentToneShift() {
			if (project != null && track != null) {
				if (track.TryGetExpDescriptor(project, Core.Format.Ustx.SHFT, out var trackTS)) {
					return (int)trackTS.CustomDefaultValue;
				}
			}
			return 0;
		}

		public int? GetParentAlternate() {
			if (project != null && track != null) {
				if (track.TryGetExpDescriptor(project, Core.Format.Ustx.ALT, out var trackAlt)) {
					if (trackAlt.CustomDefaultValue != 0) {
						return (int)trackAlt.CustomDefaultValue;
					}
				}
			}
			return null;
		}

		public string GetParentVoiceColor() {
			if (project != null && track != null && track.VoiceColorExp != null) {
				if (track.TryGetExpDescriptor(project, Core.Format.Ustx.CLR, out var trackCLR)) {
					int index = (int)trackCLR.CustomDefaultValue;
					if (index >= 0 && index < track.VoiceColorExp.options.Length) {
						return track.VoiceColorExp.options[index];
					}
				}
			}
			return string.Empty;
		}

		public static string MapPhoneme(string phoneme, int tone, string color, string alt, USinger singer) {
			if (singer.TryGetMappedOto(phoneme + alt, tone, color, out var otoAlt)) {
				return otoAlt.Alias;
			}
			if (singer.TryGetMappedOto(phoneme, tone, color, out var oto)) {
				return oto.Alias;
			}
			return phoneme;
		}
	}
}
