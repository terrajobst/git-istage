namespace GitIStage.Text;

[Flags]
public enum TextAttributes
{
    None,
    Bold      = 0x0001,
    Dim       = 0x0002,
    Italic    = 0x0004,
    Underline = 0x0008,
    Blink     = 0x0010,
    Blink2    = 0x0020,
    Reverse   = 0x0040,
    Conceal   = 0x0080,
    Strike    = 0x0100
}