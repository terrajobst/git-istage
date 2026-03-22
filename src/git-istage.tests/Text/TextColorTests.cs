namespace GitIStage.Tests;

public class TextColorTests
{
    [Fact]
    public void FromHex_RRGGBB()
    {
        var color = TextColor.FromHex("#FF8800");

        color.R.Should().Be(0xFF);
        color.G.Should().Be(0x88);
        color.B.Should().Be(0x00);
        color.Alpha.Should().Be(255);
    }

    [Fact]
    public void FromHex_RRGGBBAA()
    {
        var color = TextColor.FromHex("#ADD6FF26");

        color.R.Should().Be(0xAD);
        color.G.Should().Be(0xD6);
        color.B.Should().Be(0xFF);
        color.Alpha.Should().Be(0x26);
    }

    [Fact]
    public void FromHex_RGB()
    {
        var color = TextColor.FromHex("#FFF");

        color.R.Should().Be(0xFF);
        color.G.Should().Be(0xFF);
        color.B.Should().Be(0xFF);
        color.Alpha.Should().Be(255);
    }

    [Fact]
    public void FromHex_RGB_Expands_Digits()
    {
        var color = TextColor.FromHex("#A3F");

        color.R.Should().Be(0xAA);
        color.G.Should().Be(0x33);
        color.B.Should().Be(0xFF);
    }

    [Fact]
    public void FromHex_RGBA()
    {
        var color = TextColor.FromHex("#ccc3");

        color.R.Should().Be(0xCC);
        color.G.Should().Be(0xCC);
        color.B.Should().Be(0xCC);
        color.Alpha.Should().Be(0x33);
    }

    [Fact]
    public void FromHex_Is_Case_Insensitive()
    {
        var upper = TextColor.FromHex("#AABBCC");
        var lower = TextColor.FromHex("#aabbcc");

        upper.Should().Be(lower);
    }

    [Fact]
    public void FromHex_Throws_For_Missing_Hash()
    {
        var act = () => TextColor.FromHex("FF0000");

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void FromHex_Throws_For_Empty_String()
    {
        var act = () => TextColor.FromHex("");

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void FromHex_Throws_For_Invalid_Length()
    {
        var act = () => TextColor.FromHex("#12");

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void FromHex_Throws_For_Invalid_Hex_Characters()
    {
        var act = () => TextColor.FromHex("#GGHHII");

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void FromHex_Black()
    {
        var color = TextColor.FromHex("#000000");

        color.R.Should().Be(0);
        color.G.Should().Be(0);
        color.B.Should().Be(0);
    }

    [Fact]
    public void FromHex_Short_Black()
    {
        var color = TextColor.FromHex("#000");

        color.R.Should().Be(0);
        color.G.Should().Be(0);
        color.B.Should().Be(0);
    }
}
