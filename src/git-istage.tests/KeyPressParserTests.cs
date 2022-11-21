using Xunit;

namespace GitIStage.Tests
{
    public class KeyPressParserTests
    {
        [Fact]
        public void KeyPressParser_ParseKey()
        {
            var parser = new KeyPressParser();
            var result = parser.Parse("shift+ctrl+R");

            Assert.True(result.Succeeded);
            Assert.Equal(ConsoleKey.R, result.Key);
            Assert.Equal(ConsoleModifiers.Shift | ConsoleModifiers.Control, result.Modifiers);
        }
        [Fact]
        public void KeyPressParser_ParseMinusKey()
        {
            var parser = new KeyPressParser();
            var result = parser.Parse("-");

            Assert.True(result.Succeeded);
            Assert.Equal(ConsoleKey.OemMinus, result.Key);
        }
        [Fact]
        public void KeyPressParser_ParsePlusKey()
        {
            var parser = new KeyPressParser();
            var result = parser.Parse("+");

            Assert.True(result.Succeeded);
            Assert.Equal(ConsoleKey.OemPlus, result.Key);
        }
        [Fact]
        public void KeyPressParser_ParseCtrlPlusKey()
        {
            var parser = new KeyPressParser();
            var result = parser.Parse("ctrl++");

            Assert.True(result.Succeeded);
            Assert.Equal(ConsoleKey.OemPlus, result.Key);
            Assert.Equal(ConsoleModifiers.Control, result.Modifiers);
        }
    }
}
