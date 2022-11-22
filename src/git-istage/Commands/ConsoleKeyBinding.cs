using System.Text.RegularExpressions;

namespace GitIStage.Commands;

internal readonly struct ConsoleKeyBinding
{
    public ConsoleKeyBinding(ConsoleModifiers modifiers, ConsoleKey key)
    {
        Key = key;
        Modifiers = modifiers;
    }

    public ConsoleModifiers Modifiers { get; }

    public ConsoleKey Key { get; }

    public bool Matches(ConsoleKeyInfo keyInfo)
    {
        return Key == keyInfo.Key && Modifiers == keyInfo.Modifiers;
    }

    public static ConsoleKeyBinding Parse(string text)
    {
        if (TryParse(text, out var result))
            return result;

        throw new FormatException($"Invalid key binding: {text}");
    }
    
    public static bool TryParse(string text, out ConsoleKeyBinding result)
    {
        var (modifiersText, keyText, succeeded) = SplitLastOf(text);
        if (succeeded)
        {
            if (TryParseModifiers(modifiersText, out var modifiers) &&
                TryParseKey(keyText, out var key))
            {
                result = new ConsoleKeyBinding(modifiers, key);
                return true;
            }
        }

        result = default;
        return false;
    }
    
    private static bool TryParseKey(string s, out ConsoleKey key)
    {
        key = 0;

        if (IsKey(s, "F1")) { key = ConsoleKey.F1; return true; }
        if (IsKey(s, "F2")) { key = ConsoleKey.F2; return true; }
        if (IsKey(s, "F3")) { key = ConsoleKey.F3; return true; }
        if (IsKey(s, "F4")) { key = ConsoleKey.F4; return true; }
        if (IsKey(s, "F5")) { key = ConsoleKey.F5; return true; }
        if (IsKey(s, "F6")) { key = ConsoleKey.F6; return true; }
        if (IsKey(s, "F7")) { key = ConsoleKey.F7; return true; }
        if (IsKey(s, "F8")) { key = ConsoleKey.F8; return true; }
        if (IsKey(s, "F9")) { key = ConsoleKey.F9; return true; }
        if (IsKey(s, "F10")) { key = ConsoleKey.F10; return true; }
        if (IsKey(s, "F11")) { key = ConsoleKey.F11; return true; }
        if (IsKey(s, "F12")) { key = ConsoleKey.F12; return true; }
        if (IsKey(s, "F13")) { key = ConsoleKey.F13; return true; }
        if (IsKey(s, "F14")) { key = ConsoleKey.F14; return true; }
        if (IsKey(s, "F15")) { key = ConsoleKey.F15; return true; }
        if (IsKey(s, "F16")) { key = ConsoleKey.F16; return true; }
        if (IsKey(s, "F17")) { key = ConsoleKey.F17; return true; }
        if (IsKey(s, "F18")) { key = ConsoleKey.F18; return true; }
        if (IsKey(s, "F19")) { key = ConsoleKey.F19; return true; }
        if (IsKey(s, "F20")) { key = ConsoleKey.F20; return true; }
        if (IsKey(s, "F21")) { key = ConsoleKey.F21; return true; }
        if (IsKey(s, "F22")) { key = ConsoleKey.F22; return true; }
        if (IsKey(s, "F23")) { key = ConsoleKey.F23; return true; }
        if (IsKey(s, "F24")) { key = ConsoleKey.F24; return true; }
        if (IsKey(s, "D0")) { key = ConsoleKey.D0; return true; }
        if (IsKey(s, "D1")) { key = ConsoleKey.D1; return true; }
        if (IsKey(s, "D2")) { key = ConsoleKey.D2; return true; }
        if (IsKey(s, "D3")) { key = ConsoleKey.D3; return true; }
        if (IsKey(s, "D4")) { key = ConsoleKey.D4; return true; }
        if (IsKey(s, "D5")) { key = ConsoleKey.D5; return true; }
        if (IsKey(s, "D6")) { key = ConsoleKey.D6; return true; }
        if (IsKey(s, "D7")) { key = ConsoleKey.D7; return true; }
        if (IsKey(s, "D8")) { key = ConsoleKey.D8; return true; }
        if (IsKey(s, "D9")) { key = ConsoleKey.D9; return true; }
        if (IsKey(s, "0")) { key = ConsoleKey.D0; return true; }
        if (IsKey(s, "1")) { key = ConsoleKey.D1; return true; }
        if (IsKey(s, "2")) { key = ConsoleKey.D2; return true; }
        if (IsKey(s, "3")) { key = ConsoleKey.D3; return true; }
        if (IsKey(s, "4")) { key = ConsoleKey.D4; return true; }
        if (IsKey(s, "5")) { key = ConsoleKey.D5; return true; }
        if (IsKey(s, "6")) { key = ConsoleKey.D6; return true; }
        if (IsKey(s, "7")) { key = ConsoleKey.D7; return true; }
        if (IsKey(s, "8")) { key = ConsoleKey.D8; return true; }
        if (IsKey(s, "9")) { key = ConsoleKey.D9; return true; }
        if (IsKey(s, "A")) { key = ConsoleKey.A; return true; }
        if (IsKey(s, "B")) { key = ConsoleKey.B; return true; }
        if (IsKey(s, "C")) { key = ConsoleKey.C; return true; }
        if (IsKey(s, "D")) { key = ConsoleKey.D; return true; }
        if (IsKey(s, "E")) { key = ConsoleKey.E; return true; }
        if (IsKey(s, "F")) { key = ConsoleKey.F; return true; }
        if (IsKey(s, "G")) { key = ConsoleKey.G; return true; }
        if (IsKey(s, "H")) { key = ConsoleKey.H; return true; }
        if (IsKey(s, "P")) { key = ConsoleKey.P; return true; }
        if (IsKey(s, "Q")) { key = ConsoleKey.Q; return true; }
        if (IsKey(s, "R")) { key = ConsoleKey.R; return true; }
        if (IsKey(s, "S")) { key = ConsoleKey.S; return true; }
        if (IsKey(s, "T")) { key = ConsoleKey.T; return true; }
        if (IsKey(s, "U")) { key = ConsoleKey.U; return true; }
        if (IsKey(s, "V")) { key = ConsoleKey.V; return true; }
        if (IsKey(s, "W")) { key = ConsoleKey.W; return true; }
        if (IsKey(s, "X")) { key = ConsoleKey.X; return true; }
        if (IsKey(s, "Y")) { key = ConsoleKey.Y; return true; }
        if (IsKey(s, "Z")) { key = ConsoleKey.Z; return true; }
        if (IsKey(s, "I")) { key = ConsoleKey.I; return true; }
        if (IsKey(s, "J")) { key = ConsoleKey.J; return true; }
        if (IsKey(s, "K")) { key = ConsoleKey.K; return true; }
        if (IsKey(s, "L")) { key = ConsoleKey.L; return true; }
        if (IsKey(s, "M")) { key = ConsoleKey.M; return true; }
        if (IsKey(s, "N")) { key = ConsoleKey.N; return true; }
        if (IsKey(s, "O")) { key = ConsoleKey.O; return true; }
        if (IsKey(s, "Add")) { key = ConsoleKey.Add; return true; }
        if (IsKey(s, "Applications")) { key = ConsoleKey.Applications; return true; }
        if (IsKey(s, "Attention")) { key = ConsoleKey.Attention; return true; }
        if (IsKey(s, "Backspace")) { key = ConsoleKey.Backspace; return true; }
        if (IsKey(s, "BrowserBack")) { key = ConsoleKey.BrowserBack; return true; }
        if (IsKey(s, "BrowserFavorites")) { key = ConsoleKey.BrowserFavorites; return true; }
        if (IsKey(s, "BrowserForward")) { key = ConsoleKey.BrowserForward; return true; }
        if (IsKey(s, "BrowserHome")) { key = ConsoleKey.BrowserHome; return true; }
        if (IsKey(s, "BrowserRefresh")) { key = ConsoleKey.BrowserRefresh; return true; }
        if (IsKey(s, "BrowserSearch")) { key = ConsoleKey.BrowserSearch; return true; }
        if (IsKey(s, "BrowserStop")) { key = ConsoleKey.BrowserStop; return true; }
        if (IsKey(s, "Clear")) { key = ConsoleKey.Clear; return true; }
        if (IsKey(s, "CrSel")) { key = ConsoleKey.CrSel; return true; }
        if (IsKey(s, "Decimal")) { key = ConsoleKey.Decimal; return true; }
        if (IsKey(s, "Delete")) { key = ConsoleKey.Delete; return true; }
        if (IsKey(s, "Divide")) { key = ConsoleKey.Divide; return true; }
        if (IsKey(s, "DownArrow")) { key = ConsoleKey.DownArrow; return true; }
        if (IsKey(s, "End")) { key = ConsoleKey.End; return true; }
        if (IsKey(s, "Enter")) { key = ConsoleKey.Enter; return true; }
        if (IsKey(s, "EraseEndOfFile")) { key = ConsoleKey.EraseEndOfFile; return true; }
        if (IsKey(s, "Escape")) { key = ConsoleKey.Escape; return true; }
        if (IsKey(s, "Esc")) { key = ConsoleKey.Escape; return true; }
        if (IsKey(s, "Execute")) { key = ConsoleKey.Execute; return true; }
        if (IsKey(s, "ExSel")) { key = ConsoleKey.ExSel; return true; }
        if (IsKey(s, "Help")) { key = ConsoleKey.Help; return true; }
        if (IsKey(s, "Home")) { key = ConsoleKey.Home; return true; }
        if (IsKey(s, "Insert")) { key = ConsoleKey.Insert; return true; }
        if (IsKey(s, "LaunchApp1")) { key = ConsoleKey.LaunchApp1; return true; }
        if (IsKey(s, "LaunchApp2")) { key = ConsoleKey.LaunchApp2; return true; }
        if (IsKey(s, "LaunchMail")) { key = ConsoleKey.LaunchMail; return true; }
        if (IsKey(s, "LaunchMediaSelect")) { key = ConsoleKey.LaunchMediaSelect; return true; }
        if (IsKey(s, "LeftArrow")) { key = ConsoleKey.LeftArrow; return true; }
        if (IsKey(s, "LeftWindows")) { key = ConsoleKey.LeftWindows; return true; }
        if (IsKey(s, "MediaNext")) { key = ConsoleKey.MediaNext; return true; }
        if (IsKey(s, "MediaPlay")) { key = ConsoleKey.MediaPlay; return true; }
        if (IsKey(s, "MediaPrevious")) { key = ConsoleKey.MediaPrevious; return true; }
        if (IsKey(s, "MediaStop")) { key = ConsoleKey.MediaStop; return true; }
        if (IsKey(s, "Multiply")) { key = ConsoleKey.Multiply; return true; }
        if (IsKey(s, "NoName")) { key = ConsoleKey.NoName; return true; }
        if (IsKey(s, "NumPad0")) { key = ConsoleKey.NumPad0; return true; }
        if (IsKey(s, "NumPad1")) { key = ConsoleKey.NumPad1; return true; }
        if (IsKey(s, "NumPad2")) { key = ConsoleKey.NumPad2; return true; }
        if (IsKey(s, "NumPad3")) { key = ConsoleKey.NumPad3; return true; }
        if (IsKey(s, "NumPad4")) { key = ConsoleKey.NumPad4; return true; }
        if (IsKey(s, "NumPad5")) { key = ConsoleKey.NumPad5; return true; }
        if (IsKey(s, "NumPad6")) { key = ConsoleKey.NumPad6; return true; }
        if (IsKey(s, "NumPad7")) { key = ConsoleKey.NumPad7; return true; }
        if (IsKey(s, "NumPad8")) { key = ConsoleKey.NumPad8; return true; }
        if (IsKey(s, "NumPad9")) { key = ConsoleKey.NumPad9; return true; }
        if (IsKey(s, "Oem1")) { key = ConsoleKey.Oem1; return true; }
        if (IsKey(s, "Oem102")) { key = ConsoleKey.Oem102; return true; }
        if (IsKey(s, "Oem2")) { key = ConsoleKey.Oem2; return true; }
        if (IsKey(s, "Oem3")) { key = ConsoleKey.Oem3; return true; }
        if (IsKey(s, "Oem4")) { key = ConsoleKey.Oem4; return true; }
        if (IsKey(s, "Oem5")) { key = ConsoleKey.Oem5; return true; }
        if (IsKey(s, "Oem6")) { key = ConsoleKey.Oem6; return true; }
        if (IsKey(s, "Oem7")) { key = ConsoleKey.Oem7; return true; }
        if (IsKey(s, "Oem8")) { key = ConsoleKey.Oem8; return true; }
        if (IsKey(s, "OemClear")) { key = ConsoleKey.OemClear; return true; }
        if (IsKey(s, "OemComma")) { key = ConsoleKey.OemComma; return true; }
        if (IsKey(s, ",")) { key = ConsoleKey.OemComma; return true; }
        if (IsKey(s, "OemMinus")) { key = ConsoleKey.OemMinus; return true; }
        if (IsKey(s, "-")) { key = ConsoleKey.OemMinus; return true; }
        if (IsKey(s, "OemPeriod")) { key = ConsoleKey.OemPeriod; return true; }
        if (IsKey(s, ".")) { key = ConsoleKey.OemPeriod; return true; }
        if (IsKey(s, "OemPlus")) { key = ConsoleKey.OemPlus; return true; }
        if (IsKey(s, "+")) { key = ConsoleKey.OemPlus; return true; }
        if (IsKey(s, "Pa1")) { key = ConsoleKey.Pa1; return true; }
        if (IsKey(s, "Packet")) { key = ConsoleKey.Packet; return true; }
        if (IsKey(s, "PageDown")) { key = ConsoleKey.PageDown; return true; }
        if (IsKey(s, "PageUp")) { key = ConsoleKey.PageUp; return true; }
        if (IsKey(s, "Pause")) { key = ConsoleKey.Pause; return true; }
        if (IsKey(s, "Play")) { key = ConsoleKey.Play; return true; }
        if (IsKey(s, "Print")) { key = ConsoleKey.Print; return true; }
        if (IsKey(s, "PrintScreen")) { key = ConsoleKey.PrintScreen; return true; }
        if (IsKey(s, "Process")) { key = ConsoleKey.Process; return true; }
        if (IsKey(s, "RightArrow")) { key = ConsoleKey.RightArrow; return true; }
        if (IsKey(s, "RightWindows")) { key = ConsoleKey.RightWindows; return true; }
        if (IsKey(s, "Select")) { key = ConsoleKey.Select; return true; }
        if (IsKey(s, "Separator")) { key = ConsoleKey.Separator; return true; }
        if (IsKey(s, "Sleep")) { key = ConsoleKey.Sleep; return true; }
        if (IsKey(s, "Spacebar")) { key = ConsoleKey.Spacebar; return true; }
        if (IsKey(s, "Space")) { key = ConsoleKey.Spacebar; return true; }
        if (IsKey(s, "Subtract")) { key = ConsoleKey.Subtract; return true; }
        if (IsKey(s, "Tab")) { key = ConsoleKey.Tab; return true; }
        if (IsKey(s, "UpArrow")) { key = ConsoleKey.UpArrow; return true; }
        if (IsKey(s, "VolumeDown")) { key = ConsoleKey.VolumeDown; return true; }
        if (IsKey(s, "VolumeMute")) { key = ConsoleKey.VolumeMute; return true; }
        if (IsKey(s, "VolumeUp")) { key = ConsoleKey.VolumeUp; return true; }
        if (IsKey(s, "Zoom")) { key = ConsoleKey.Zoom; return true; }
        return false;
    }

    private static bool IsKey(string c, string text)
    {
        return string.Compare(c, text, StringComparison.OrdinalIgnoreCase) == 0;
    }

    private static bool TryParseModifiers(string modifier, out ConsoleModifiers modifiers)
    {
        modifiers = 0;
        return TryParseModifiersImpl(modifier, ref modifiers);
    }

    private static bool TryParseModifiersImpl(string modifiers, ref ConsoleModifiers mods)
    {
        while (true)
        {
            var (others, modifier, succeeded) = SplitLastOf(modifiers);
            if (!succeeded) return false;

            if (modifier.Length == 0) return true;

            if (!TryParseModifier(modifier, out var mod)) return false;

            mods |= mod;

            modifiers = others;
        }
    }

    private static bool TryParseModifier(string modifier, out ConsoleModifiers mods)
    {
        mods = 0;

        var mapping = new Dictionary<string, ConsoleModifiers>
        {
            ["altgr"] = ConsoleModifiers.Control | ConsoleModifiers.Alt,
            ["alt"] = ConsoleModifiers.Alt,
            ["shift"] = ConsoleModifiers.Shift,
            ["ctrl"] = ConsoleModifiers.Control,
            ["control"] = ConsoleModifiers.Control,
        };

        var mod = modifier.ToLowerInvariant();
        if (mapping.ContainsKey(mod))
        {
            mods = mapping[mod];
            return true;
        }

        return false;
    }

    private static (string, string, bool) SplitLastOf(string text)
    {
        if (text.Length == 0)
            return ("", text, true);

        // language=regex
        var pattern = @"(?:(?<first>.+)\+)?(?<second>.+)";
        var regex = new Regex(pattern);
        var match = regex.Match(text);

        return (
            match.Groups["first"].Value,
            match.Groups["second"].Value,
            match.Success
        );
    }
    
    public override string ToString()
    {
        var key = Key switch
        {
            ConsoleKey.UpArrow => Key.ToString().Replace("Arrow", ""),
            ConsoleKey.DownArrow => Key.ToString().Replace("Arrow", ""),
            ConsoleKey.LeftArrow => Key.ToString().Replace("Arrow", ""),
            ConsoleKey.RightArrow => Key.ToString().Replace("Arrow", ""),
            ConsoleKey.OemPlus => "+",
            ConsoleKey.OemMinus => "-",
            ConsoleKey.Oem4 => "[",
            ConsoleKey.Oem6 => "]",
            ConsoleKey.Oem7 => "'",
            ConsoleKey.D0 => Key.ToString().Replace("D", ""),
            ConsoleKey.D1 => Key.ToString().Replace("D", ""),
            ConsoleKey.D2 => Key.ToString().Replace("D", ""),
            ConsoleKey.D3 => Key.ToString().Replace("D", ""),
            ConsoleKey.D4 => Key.ToString().Replace("D", ""),
            ConsoleKey.D5 => Key.ToString().Replace("D", ""),
            ConsoleKey.D6 => Key.ToString().Replace("D", ""),
            ConsoleKey.D7 => Key.ToString().Replace("D", ""),
            ConsoleKey.D8 => Key.ToString().Replace("D", ""),
            ConsoleKey.D9 => Key.ToString().Replace("D", ""),
            _ => Key.ToString()
        };

        if (Key == ConsoleKey.Oem2 && Modifiers == ConsoleModifiers.Shift)
            return "?";

        return Modifiers != 0 ? $"{Modifiers.ToString().Replace("Control", "Ctrl")} + {key}" : key;
    }
}