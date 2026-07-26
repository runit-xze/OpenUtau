using System.IO;
using System.Linq;
using System.Text;
using OpenUtau.Core.Ustx;

//reference: https://github.com/sdercolin/utaformatix-data/blob/main/lib/csharp/UtaFormatix.Data

namespace OpenUtau.Core.Format {
	public struct UfNote {
		public int key;
		public int tickOn;
		public int tickOff;
		public string lyric;
		public string? phoneme;
	}

	public struct UfPitch {
		public int[] ticks;
		public double?[] values;
		public bool isAbsolute;
	}

	public struct UfTempo {
		public int tickPosition;
		public double bpm;
	}

	public struct UfTimeSignature {
		public int measurePosition;
		public int numerator;
		public int denominator;
	}

	public struct UfTrack {
		public string name;
		public UfNote[] notes;
		public UfPitch? pitch;
	}

	public struct UfProject {
		public string name;
		public UfTrack[] tracks;
		public UfTimeSignature[]? timeSignatures;
		public UfTempo[] tempos;
		public int measurePrefix;
	}

	public struct UfFile {
		public UfProject project;
		public int formatVersion;
	}

	public static class Ufdata {
		static UVoicePart ParsePart(UfTrack ufTrack, UProject project) {
			var part = new UVoicePart();
			part.name = ufTrack.name;
			part.position = 0;
			foreach (var ufNote in ufTrack.notes) {
				var note = project.CreateNote(
					ufNote.key,
					ufNote.tickOn,
					ufNote.tickOff - ufNote.tickOn
				);
				note.lyric = ufNote.lyric;
				if (note.lyric == "-") {
					note.lyric = "+~";
				}
				part.notes.Add(note);
			}
			part.Duration = ufTrack.notes[^1].tickOff;
			return part;
		}

		public static UProject Load(string file) {
			UProject project = new UProject();
			Ustx.AddDefaultExpressions(project);
			project.FilePath = file;

			var ufProject = Json.Deserialize<UfFile>(File.ReadAllText(file, Encoding.UTF8)).project;

			//parse tempo
			project.tempos = ufProject.tempos
				.Select(t => new UTempo(t.tickPosition, t.bpm))
				.ToList();
			//parse timeSignature
			project.timeSignatures = ufProject.timeSignatures
				.Select(t => new UTimeSignature(t.measurePosition, t.numerator, t.denominator))
				.ToList();
			//parse tracks
			var parts = ufProject.tracks
				.Where(tr => tr.notes.Length > 0)
				.Select(tr => ParsePart(tr, project))
				.ToList();
			foreach (var part in parts) {
				var track = new UTrack(project);
				track.TrackNo = project.tracks.Count;
				part.trackNo = track.TrackNo;
				part.AfterLoad(project, track);
				project.tracks.Add(track);
				project.parts.Add(part);
			}

			project.ValidateFull();
			return project;
		}
	}
}
