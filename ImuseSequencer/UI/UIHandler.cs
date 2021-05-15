using Jither.Imuse.Scripting.Events;
using Jither.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ImuseSequencer.UI
{
    public class UIHandler
    {
        private static readonly Logger logger = LogProvider.Get(nameof(UIHandler));

        private class KeyHandler
        {
            public KeyPress Key { get; }
            public string Name { get; }
            public Func<KeyPress, bool> Callback { get; }

            public KeyHandler(KeyPress key, string name, Func<KeyPress, bool> callback)
            {
                Key = key;
                Name = name;
                Callback = callback;
            }
        }

        private readonly Dictionary<KeyPress, KeyHandler> keyHandlersByKey = new();
        private readonly List<KeyHandler> keyHandlers = new();

        public void Run()
        {
            while (true)
            {
                // TODO: Remove hack
                Thread.Sleep(50);
                if (Console.KeyAvailable)
                {
                    var keyInfo = Console.ReadKey(intercept: true);
                    var keyPress = ToKeyPress(keyInfo);
                    if (keyHandlersByKey.TryGetValue(keyPress, out var handler))
                    {
                        if (!handler.Callback(keyPress))
                        {
                            break;
                        }
                    }
                }
            }
        }

        public void OutputMenu()
        {
            logger.Info("[darkblue]Keyboard commands:[/]");
            var handlers = this.keyHandlers.Select(k => $"[yellow]{k.Key, 20}[/]  {k.Name}");
            foreach (var handler in handlers)
            {
                logger.Info(handler);
            }
            logger.Info("");
        }

        public void RegisterKeyPress(string keyStr, string name, Func<KeyPress, bool> callback)
        {
            if (!KeyPress.TryParse(keyStr, out var key))
            {
                throw new ArgumentException($"Unknown keypress: {key}");
            }
            RegisterKeyPress(key, name, callback);
        }

        public void RegisterKeyPress(KeyPress key, string name, Func<KeyPress, bool> callback)
        {
            if (keyHandlersByKey.ContainsKey(key))
            {
                throw new ArgumentException($"Keypress {key} is already registered");
            }

            var handler = new KeyHandler(key, name, callback);
            keyHandlersByKey.Add(key, handler);
            keyHandlers.Add(handler);
        }

        private KeyPress ToKeyPress(ConsoleKeyInfo consoleKey)
        {
            var mods = Modifiers.None;
            if (consoleKey.Modifiers.HasFlag(ConsoleModifiers.Shift))
            {
                mods |= Modifiers.Shift;
            }
            if (consoleKey.Modifiers.HasFlag(ConsoleModifiers.Alt))
            {
                mods |= Modifiers.Alt;
            }
            if (consoleKey.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                mods |= Modifiers.Ctrl;
            }

            // Quick hack: Key actually maps 1:1 to the ConsoleKey enumeration
            var key = (Key)consoleKey.Key;

            return new KeyPress(key, mods);
        }
    }
}
