namespace GitIStage.Tests;

public class ConsoleKeyBindingTests
{
    [Fact]
    public void ConsoleKeyBinding_ParseKey()
    {
        var result = ConsoleKeyBinding.Parse("shift+ctrl+R");

        Assert.Equal(ConsoleKey.R, result.Key);
        Assert.Equal(ConsoleModifiers.Shift | ConsoleModifiers.Control, result.Modifiers);
    }

    [Fact]
    public void ConsoleKeyBinding_ParseMinusKey()
    {
        var result = ConsoleKeyBinding.Parse("-");

        Assert.Equal(ConsoleKey.OemMinus, result.Key);
    }

    [Fact]
    public void ConsoleKeyBinding_ParsePlusKey()
    {
        var result = ConsoleKeyBinding.Parse("+");

        Assert.Equal(ConsoleKey.OemPlus, result.Key);
    }

    [Fact]
    public void ConsoleKeyBinding_ParseCtrlPlusKey()
    {
        var result = ConsoleKeyBinding.Parse("ctrl++");

        Assert.Equal(ConsoleKey.OemPlus, result.Key);
        Assert.Equal(ConsoleModifiers.Control, result.Modifiers);
    }
}