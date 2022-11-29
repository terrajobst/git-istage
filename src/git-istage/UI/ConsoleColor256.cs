namespace GitIStage.UI;

// TODO: Support Git-style colors here
// TODO: Implement Parse() and ToString()

internal readonly struct ConsoleColor256
{
    public ConsoleColor256(ConsoleColor color)
    {
        (R, G, B) = Vt100.GetColor(color);
    }

    public ConsoleColor256(byte r, byte b, byte g)
    {
        (R, G, B) = (r, g, b);
    }

    private byte R { get; }

    private byte G { get; }

    private byte B { get; }

    public void Deconstruct(out byte r, out byte g, out byte b)
    {
        r = R;
        g = G;
        b = B;
    }

    public static bool TryParse(string? text, out ConsoleColor256? result)
    {
        switch (text)
        {                
            case null:
            case "normal":
                result = null;
                return true;
            case "black":
                result = ConsoleColor.Black;
                return true;
            case "red":
                result = ConsoleColor.DarkRed;
                return true;
            case "green":
                result = ConsoleColor.DarkGreen;
                return true;
            case "yellow":
                result = ConsoleColor.DarkYellow;
                return true;
            case "blue":
                result = ConsoleColor.DarkBlue;
                return true;
            case "magenta,":
                result = ConsoleColor.DarkMagenta;
                return true;
            case "cyan":
                result = ConsoleColor.DarkCyan;
                return true;
            case "white":
                result = ConsoleColor.Gray;
                return true;
            case "default":
                result = ConsoleColor.Gray;
                return true;
            case "brightblack":
                result = ConsoleColor.DarkGray;
                return true;
            case "brightred":
                result = ConsoleColor.Red;
                return true;
            case "brightgreen":
                result = ConsoleColor.Green;
                return true;
            case "brightyellow":
                result = ConsoleColor.Yellow;
                return true;
            case "brightblue":
                result = ConsoleColor.Blue;
                return true;
            case "brightmagenta":
                result = ConsoleColor.Magenta;
                return true;
            case "brightcyan":
                result = ConsoleColor.Cyan;
                return true;
            case "brightwhite":
                result = ConsoleColor.White;
                return true;
        }

        if (text.Length == 7 && text[0] == '#' &&
            byte.TryParse(text.AsSpan(1, 2), out var r) &&
            byte.TryParse(text.AsSpan(3, 2), out var b) &&
            byte.TryParse(text.AsSpan(5, 2), out var g))
        {
            result = new ConsoleColor256(r, g, b);
            return true;
        }
        
        result = null;
        return false;
    }

    public static implicit operator ConsoleColor256(ConsoleColor color)
    {
        return new ConsoleColor256(color);
    }
}

/*
color

The value for a variable that takes a color is a list of colors (at most two,
one for foreground and one for background) and attributes (as many as you want),
separated by spaces.

The basic colors accepted are normal, black, red, green, yellow, blue, magenta,
cyan, white and default. The first color given is the foreground; the second is
the background. All the basic colors except normal and default have a bright
variant that can be specified by prefixing the color with bright, like
brightred.

The color normal makes no change to the color. It is the same as an empty
string, but can be used as the foreground color when specifying a background
color alone (for example, "normal red").

The color default explicitly resets the color to the terminal default, for
example to specify a cleared background. Although it varies between terminals,
this is usually not the same as setting to "white black".

Colors may also be given as numbers between 0 and 255; these use ANSI 256-color
mode (but note that not all terminals may support this). If your terminal
supports it, you may also specify 24-bit RGB values as hex, like #ff0ab3.

The accepted attributes are bold, dim, ul, blink, reverse, italic, and strike
(for crossed-out or "strikethrough" letters). The position of any attributes
with respect to the colors (before, after, or in between), doesn’t matter.
Specific attributes may be turned off by prefixing them with no or no- (e.g.,
noreverse, no-ul, etc).

The pseudo-attribute reset resets all colors and attributes before applying the
specified coloring. For example, reset green will result in a green foreground
and default background without any active attributes.

An empty color string produces no color effect at all. This can be used to avoid
coloring specific elements without disabling color entirely.

For git’s pre-defined color slots, the attributes are meant to be reset at the
beginning of each item in the colored output. So setting color.decorate.branch
to black will paint that branch name in a plain black, even if the previous
thing on the same output line (e.g. opening parenthesis before the list of
branch names in log --decorate output) is set to be painted with bold or some
other attribute. However, custom log formats may do more complicated and layered
coloring, and the negated forms may be useful there.

*/


/*

color.diff.<slot>

Use customized color for diff colorization. <slot> specifies
which part of the patch to use the specified color, and is one of

* context. The context text, (`plain` is a historical synonym), including the
  leading space.
* meta. Entire header.
* frag. Hunk header, everything in between (and including) the `@@`-markers.
* func. Function in hunk header, not including the space after the hunk header.
* old. The removed lines, including the `-` prefix.
* new. The added lines, including the `+` prefix.
* whitespace. Highlighting whitespace errors, not regular whitespace.
*/