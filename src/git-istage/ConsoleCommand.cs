using System;

namespace GitIStage
{
    internal sealed class ConsoleCommand
    {
        private readonly Action _handler;
        private readonly ConsoleKey _key;
        public readonly string Description;
        private readonly ConsoleModifiers _modifiers;

        public ConsoleCommand(Action handler, ConsoleKey key, string description)
        {
            _handler = handler;
            _key = key;
            Description = description;
            _modifiers = 0;
        }

        public ConsoleCommand(Action handler, ConsoleKey key, ConsoleModifiers modifiers, string description)
        {
            _handler = handler;
            _key = key;
            _modifiers = modifiers;

            Description = description;
        }

        public void Execute()
        {
            _handler();
        }

        public bool MatchesKey(ConsoleKeyInfo keyInfo)
        {
            return _key == keyInfo.Key && _modifiers == keyInfo.Modifiers;
        }

        public string GetCommandShortcut()
        {
            if (_modifiers != 0)
                return $"{_modifiers.ToString()} + {_key.ToString()}";
            else
                return _key.ToString();
        }
    }
}