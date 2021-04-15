using Jither.Midi.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Midi.Helpers
{
	public static class MidiHelper
	{
		private static readonly string[] PITCH_CLASS_NAMES = new[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
		private static readonly string[] PITCH_CLASS_NAMES_FLAT = new[] { "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "Cb" }; // Note the Cb instead of B (for Cb minor)
		private static readonly Dictionary<MidiController, string> controllerNames = new();

		static MidiHelper()
        {
			foreach (var value in Enum.GetValues<MidiController>())
            {
				string name = Enum.GetName(value);
				name = StringConverter.PascalToKebabCase(name);
				controllerNames.Add(value, name);
            }
        }

		public static string NoteNumberToName(int number)
		{
			int octave = number / 12 - 1;
			int pitchClass = number % 12;
			return PITCH_CLASS_NAMES[pitchClass] + octave;
		}

		public static string GetKeySignatureName(int accidentals, int mode)
		{
			int pitchClass = 0;
			string modeName = null;
			switch (mode)
			{
				case 0:
					pitchClass = (accidentals * 7) % 12;
					modeName = "major";
					break;
				case 1:
					pitchClass = (accidentals * 7 + 9) % 12;
					modeName = "minor";
					break;
			}
			if (pitchClass < 0)
			{
				pitchClass = 12 + pitchClass;
			}
			if (accidentals > 7 || accidentals < -7 || modeName == null)
			{
				return $"Unknown {modeName ?? $"mode {mode}"} key with {Math.Abs(accidentals)} {(accidentals >= 0 ? "sharps" : "flats")}";
			}
			return (accidentals >= 0 ? PITCH_CLASS_NAMES[pitchClass] : PITCH_CLASS_NAMES_FLAT[pitchClass]) + " " + modeName;
		}

		public static string GetControllerName(MidiController controller)
        {
			if (!controllerNames.TryGetValue(controller, out string name))
            {
				return null;
            }
			return name;
        }
	}
}
