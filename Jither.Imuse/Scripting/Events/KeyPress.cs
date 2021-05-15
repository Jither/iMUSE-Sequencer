using Jither.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Imuse.Scripting.Events
{
    [Flags]
    public enum Modifiers
    {
        None = 0,
        Alt = 1,
        Shift = 2,
        Ctrl = 4
    }

    public enum Key
    {
        D0 = 48,
        D1 = 49,
        D2 = 50,
        D3 = 51,
        D4 = 52,
        D5 = 53,
        D6 = 54,
        D7 = 55,
        D8 = 56,
        D9 = 57,

        A = 65,
        B = 66,
        C = 67,
        D = 68,
        E = 69,
        F = 70,
        G = 71,
        H = 72,
        I = 73,
        J = 74,
        K = 75,
        L = 76,
        M = 77,
        N = 78,
        O = 79,
        P = 80,
        Q = 81,
        R = 82,
        S = 83,
        T = 84,
        U = 85,
        V = 86,
        W = 87,
        X = 88,
        Y = 89,
        Z = 90,

        F1 = 112,
        F2 = 113,
        F3 = 114,
        F4 = 115,
        F5 = 116,
        F6 = 117,
        F7 = 118,
        F8 = 119,
        F9 = 120,
        F10 = 121,
        F11 = 122,
        F12 = 123,

        Esc = 27
    }

    /// <summary>
    /// Platform independent Key + Modifier.
    /// </summary>
    /// <remarks>
    /// Currently only supports common shortcut keys (i.e. no special keys, OEM keys etc.)
    /// </remarks>
    public class KeyPress : IEquatable<KeyPress>
    {
        public Key Key { get; }
        public Modifiers Modifiers { get; }

        public KeyPress(Key key, Modifiers modifiers)
        {
            Key = key;
            Modifiers = modifiers;
        }

        public static bool TryParse(string str, out KeyPress keyPress)
        {
            keyPress = null;

            var parts = str.Split("+");
            if (!keysByName.TryGetValue(parts[^1], out Key key))
            {
                return false;
            }

            Modifiers modifiers = Modifiers.None;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                switch (parts[0].ToLower())
                {
                    case "ctrl":
                    case "control":
                        modifiers |= Modifiers.Ctrl;
                        break;
                    case "alt":
                        modifiers |= Modifiers.Alt;
                        break;
                    case "shift":
                        modifiers |= Modifiers.Shift;
                        break;
                    default:
                        return false;
                }
            }

            keyPress = new KeyPress(key, modifiers);
            return true;
        }

        public override string ToString()
        {
            var parts = new List<string>();
            if (Modifiers.HasFlag(Modifiers.Shift))
            {
                parts.Add("Shift");
            }
            if (Modifiers.HasFlag(Modifiers.Ctrl))
            {
                parts.Add("Ctrl");
            }
            if (Modifiers.HasFlag(Modifiers.Alt))
            {
                parts.Add("Alt");
            }

            parts.Add(GetKeyName(Key));

            return String.Join("+", parts);
        }

        private string GetKeyName(Key key)
        {
            if (key >= Key.D0 && key <= Key.Z)
            {
                return ((char)key).ToString();
            }

            if (key >= Key.F1 && key <= Key.F12)
            {
                return "F" + (key - Key.F1 + 1);
            }

            return key.GetDisplayName();

            throw new NotImplementedException($"GetKeyName not implemented for {key}");
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as KeyPress);
        }

        public bool Equals(KeyPress other)
        {
            if (other is null)
            {
                return false;
            }

            return (Key == other.Key) && (Modifiers == other.Modifiers);
        }

        public static bool operator ==(KeyPress a, KeyPress b)
        {
            if (a is null)
            {
                return b is null;
            }
            return a.Equals(b);
        }

        public static bool operator !=(KeyPress a, KeyPress b) => !(a == b);

        public override int GetHashCode()
        {
            return HashCode.Combine(Key, Modifiers);
        }

        private static readonly Dictionary<string, Key> keysByName = new(StringComparer.OrdinalIgnoreCase)
        {
            ["a"] = Key.A,
            ["b"] = Key.B,
            ["c"] = Key.C,
            ["d"] = Key.D,
            ["e"] = Key.E,
            ["f"] = Key.F,
            ["g"] = Key.G,
            ["h"] = Key.H,
            ["i"] = Key.I,
            ["j"] = Key.J,
            ["k"] = Key.K,
            ["l"] = Key.L,
            ["m"] = Key.M,
            ["n"] = Key.N,
            ["o"] = Key.O,
            ["p"] = Key.P,
            ["q"] = Key.Q,
            ["r"] = Key.R,
            ["s"] = Key.S,
            ["t"] = Key.T,
            ["u"] = Key.U,
            ["v"] = Key.V,
            ["w"] = Key.W,
            ["x"] = Key.X,
            ["y"] = Key.Y,
            ["z"] = Key.Z,

            ["0"] = Key.D0,
            ["1"] = Key.D1,
            ["2"] = Key.D2,
            ["3"] = Key.D3,
            ["4"] = Key.D4,
            ["5"] = Key.D5,
            ["6"] = Key.D6,
            ["7"] = Key.D7,
            ["8"] = Key.D8,
            ["9"] = Key.D9,

            ["f1"] = Key.F1,
            ["f2"] = Key.F2,
            ["f3"] = Key.F3,
            ["f4"] = Key.F4,
            ["f5"] = Key.F5,
            ["f6"] = Key.F6,
            ["f7"] = Key.F7,
            ["f8"] = Key.F8,
            ["f9"] = Key.F9,
            ["f10"] = Key.F10,

            ["esc"] = Key.Esc
        };
    }
}
