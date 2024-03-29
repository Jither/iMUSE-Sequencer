﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Midi.Helpers;

public static class PatchNameHelper
{
    /// <summary>
    /// Patch names in the General MIDI standard. Note: 0-indexed!
    /// </summary>
    public static readonly IReadOnlyList<string> GeneralMidiNames = new List<string>
    {
        "Acoustic Grand Piano",
        "Bright Acoustic Piano",
        "Electric Grand Piano",
        "Honky-tonk Piano",
        "Electric Piano 1",
        "Electric Piano 2",
        "Harpsichord",
        "Clavinet",
        "Celesta",
        "Glockenspiel",
        "Music Box",
        "Vibraphone",
        "Marimba",
        "Xylophone",
        "Tubular Bells",
        "Dulcimer",
        "Drawbar Organ",
        "Percussive Organ",
        "Rock Organ",
        "Church Organ",
        "Reed Organ",
        "Accordion",
        "Harmonica",
        "Tango Accordion",
        "Acoustic Guitar (nylon)",
        "Acoustic Guitar (steel)",
        "Electric Guitar (jazz)",
        "Electric Guitar (clean)",
        "Electric Guitar (muted)",
        "Overdriven Guitar",
        "Distortion Guitar",
        "Guitar harmonics",
        "Acoustic Bass",
        "Electric Bass (finger)",
        "Electric Bass (pick)",
        "Fretless Bass",
        "Slap Bass 1",
        "Slap Bass 2",
        "Synth Bass 1",
        "Synth Bass 2",
        "Violin",
        "Viola",
        "Cello",
        "Contrabass",
        "Tremolo Strings",
        "Pizzicato Strings",
        "Orchestral Harp",
        "Timpani",
        "String Ensemble 1",
        "String Ensemble 2",
        "Synth Strings 1",
        "Synth Strings 2",
        "Choir Aahs",
        "Voice Oohs",
        "Synth Voice",
        "Orchestra Hit",
        "Trumpet",
        "Trombone",
        "Tuba",
        "Muted Trumpet",
        "French Horn",
        "Brass Section",
        "Synth Brass 1",
        "Synth Brass 2",
        "Soprano Sax",
        "Alto Sax",
        "Tenor Sax",
        "Baritone Sax",
        "Oboe",
        "English Horn",
        "Bassoon",
        "Clarinet",
        "Piccolo",
        "Flute",
        "Recorder",
        "Pan Flute",
        "Blown Bottle",
        "Shakuhachi",
        "Whistle",
        "Ocarina",
        "Lead 1 (square)",
        "Lead 2 (sawtooth)",
        "Lead 3 (calliope)",
        "Lead 4 (chiff)",
        "Lead 5 (charang)",
        "Lead 6 (voice)",
        "Lead 7 (fifths)",
        "Lead 8 (bass + lead)",
        "Pad 1 (new age)",
        "Pad 2 (warm)",
        "Pad 3 (polysynth)",
        "Pad 4 (choir)",
        "Pad 5 (bowed)",
        "Pad 6 (metallic)",
        "Pad 7 (halo)",
        "Pad 8 (sweep)",
        "FX 1 (rain)",
        "FX 2 (soundtrack)",
        "FX 3 (crystal)",
        "FX 4 (atmosphere)",
        "FX 5 (brightness)",
        "FX 6 (goblins)",
        "FX 7 (echoes)",
        "FX 8 (sci-fi)",
        "Sitar",
        "Banjo",
        "Shamisen",
        "Koto",
        "Kalimba",
        "Bag pipe",
        "Fiddle",
        "Shanai",
        "Tinkle Bell",
        "Agogo",
        "Steel Drums",
        "Woodblock",
        "Taiko Drum",
        "Melodic Tom",
        "Synth Drum",
        "Reverse Cymbal",
        "Guitar Fret Noise",
        "Breath Noise",
        "Seashore",
        "Bird Tweet",
        "Telephone Ring",
        "Helicopter",
        "Applause",
        "Gunshot",
    };

    /// <summary>
    /// Factory patch names for Roland MT-32. Note: 0-indexed!
    /// </summary>
    public static readonly IReadOnlyList<string> MT32Names = new List<string>
    {
        "Acou Piano 1",
        "Acou Piano 2",
        "Acou Piano 3",
        "Elec Piano 1",
        "Elec Piano 2",
        "Elec Piano 3",
        "Elec Piano 4",
        "Honkytonk",
        "Elec Org 1",
        "Elec Org 2",
        "Elec Org 3",
        "Elec Org 4",
        "Pipe Org 1",
        "Pipe Org 2",
        "Pipe Org 3",
        "Accordion",

        "Harpsi 1",
        "Harpsi 2",
        "Harpsi 3",
        "Clavi 1",
        "Clavi 2",
        "Clavi 3",
        "Celesta 1",
        "Celesta 2",
        "Syn Brass 1",
        "Syn Brass 2",
        "Syn Brass 3",
        "Syn Brass 4",
        "Syn Bass 1",
        "Syn Bass 2",
        "Syn Bass 3",
        "Syn Bass 4",

        "Fantasy",
        "Harmo Pan",
        "Chorale",
        "Glasses",
        "Soundtrack",
        "Atmosphere",
        "Warm Bell",
        "Funny Vox",
        "Echo Bell",
        "Ice Rain",
        "Oboe 2001",
        "Echo Pan",
        "Doctor Solo",
        "Schooldaze",
        "Bell Singer",
        "Square Wave",
        
        "Str Sect 1",
        "Str Sect 2",
        "Str Sect 3",
        "Pizzicato",
        "Violin 1",
        "Violin 2",
        "Cello 1",
        "Cello 2",
        "Contrabass",
        "Harp 1",
        "Harp 2",
        "Guitar 1",
        "Guitar 2",
        "Elec Gtr 1",
        "Elec Gtr 2",
        "Sitar",

        "Acou Bass 1",
        "Acou Bass 2",
        "Elec Bass 1",
        "Elec Bass 2",
        "Slap Bass 1",
        "Slap Bass 2",
        "Fretless 1",
        "Fretless 2",
        "Flute 1",
        "Flute 2",
        "Piccolo 1",
        "Piccolo 2",
        "Recorder",
        "Pan Pipes",
        "Sax 1",
        "Sax 2",

        "Sax 3",
        "Sax 4",
        "Clarinet 1",
        "Clarinet 2",
        "Oboe",
        "Engl Horn",
        "Bassoon",
        "Harmonica",
        "Trumpet 1",
        "Trumpet 2",
        "Trombone 1",
        "Trombone 2",
        "Fr Horn 1",
        "Fr Horn 2",
        "Tuba",
        "Brs Sect 1",

        "Brs Sect 2",
        "Vibe 1",
        "Vibe 2",
        "Syn Mallet",
        "Wind Bell",
        "Glock",
        "Tube Bell",
        "Xylophone",
        "Marimba",
        "Koto",
        "Sho",
        "Shakuhachi",
        "Whistle 1",
        "Whistle 2",
        "Bottle Blow",
        "Breathpipe",

        "Timpani",
        "Melodic Tom",
        "Deep Snare",
        "Elec Perc 1",
        "Elec Perc 2",
        "Taiko",
        "Taiko Rim",
        "Cymbal",
        "Castanets",
        "Orche Hit",
        "Triangle",
        "Telephone",
        "Bird Tweet",
        "One Note Jam",
        "Water Bells",
        "Jungle Tune"
    };
}
