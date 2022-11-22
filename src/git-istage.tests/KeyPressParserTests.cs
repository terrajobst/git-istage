using GitIStage.Commands;
using Xunit;

namespace GitIStage.Tests;

public class KeyPressParserTests
{
    [Fact]
    public void KeyPressParser_ParseKey()
    {
        var result = ConsoleKeyBinding.Parse("shift+ctrl+R");

        Assert.Equal(ConsoleKey.R, result.Key);
        Assert.Equal(ConsoleModifiers.Shift | ConsoleModifiers.Control, result.Modifiers);
    }
    [Fact]
    public void KeyPressParser_ParseMinusKey()
    {
        var result = ConsoleKeyBinding.Parse("-");

        Assert.Equal(ConsoleKey.OemMinus, result.Key);
    }

    [Fact]
    public void KeyPressParser_ParsePlusKey()
    {
        var result = ConsoleKeyBinding.Parse("+");

        Assert.Equal(ConsoleKey.OemPlus, result.Key);
    }

    [Fact]
    public void KeyPressParser_ParseCtrlPlusKey()
    {
        var result = ConsoleKeyBinding.Parse("ctrl++");

        Assert.Equal(ConsoleKey.OemPlus, result.Key);
        Assert.Equal(ConsoleModifiers.Control, result.Modifiers);
    }
}