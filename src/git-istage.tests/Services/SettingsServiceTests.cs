namespace GitIStage.Tests;

public class SettingsServiceTests : SettingsServiceTestsBase
{
    [Fact]
    public void SettingsService_Reports_Invalid_Json()
    {
        var settings = """
            [
            """;

        WriteSettings(settings);

        var action = () => SettingsService.GetUserKeyBindings();

        action.Should()
              .Throw<GitIStageStartupException>()
              .WithMessage("*is not valid JSON*");
    }

    [Fact(Skip = "We first need to be able to construct the command service")]
    public void SettingsService_Reports_Invalid_Command()
    {
        var settings = """
            {
                "an-unknown-command": {
                    "keyBindings": ["Ctrl+A"]
                }
            }
            """;

        WriteSettings(settings);

        var action = () => SettingsService.GetUserKeyBindings();

        action.Should()
              .Throw<GitIStageStartupException>()
              .WithMessage("*is not valid JSON*");
    }

    [Fact]
    public void SettingsService_Reports_Invalid_Binding()
    {
        var settings = """
            {
                "an-unknown-command": {
                    "keyBindings": ["Ctrl+"]
                }
            }
            """;

        WriteSettings(settings);

        var action = () => SettingsService.GetUserKeyBindings();

        action.Should()
              .Throw<GitIStageStartupException>()
              .WithMessage("*invalid key binding*");
    }

    [Fact]
    public void SettingsService_Ignores_NonExistingSettings()
    {
        var bindings = SettingsService.GetUserKeyBindings();
        bindings.Should().BeEmpty();
    }

    [Fact]
    public void SettingsService_Ignores_EmptyBinding()
    {
        var settings = """
            {
                "Exit" : {
                }
            }
            """;

        WriteSettings(settings);

        var bindings = SettingsService.GetUserKeyBindings();
        bindings.Should().BeEmpty();
    }

    [Fact]
    public void SettingsService_Ignores_NullBindings()
    {
        var settings = """
            {
                "Exit" : {
                     "keyBindings": null
               }
            }
            """;

        WriteSettings(settings);

        var bindings = SettingsService.GetUserKeyBindings();
        bindings.Should().BeEmpty();
    }

    [Fact]
    public void SettingsService_Ignores_EmptyBindings()
    {
        var settings = """
            {
                "Exit" : {
                     "keyBindings": []
               }
            }
            """;

        WriteSettings(settings);

        var bindings = SettingsService.GetUserKeyBindings();
        bindings.Should().BeEmpty();
    }

    [Fact]
    public void SettingsService_Ignores_NullBinding()
    {
        var settings = """
            {
                "Exit" : {
                     "keyBindings": [null]
               }
            }
            """;

        WriteSettings(settings);

        var bindings = SettingsService.GetUserKeyBindings();
        bindings.Should().BeEmpty();
    }

    [Fact]
    public void SettingsService_Reads_Key_Single()
    {
        var settings = """
            {
                "Exit" : {
                     "keyBindings": ["Ctrl+A"]
               }
            }
            """;

        var expectedKey = new ConsoleKeyBinding(ConsoleModifiers.Control, ConsoleKey.A);

        WriteSettings(settings);

        SettingsService.GetUserKeyBindings()
                         .Should().ContainKey("Exit")
                         .WhoseValue
                         .Should().ContainSingle().Which.Should().Be(expectedKey);
    }

    [Fact]
    public void SettingsService_Reads_Key_Multiple()
    {
        var settings = """
            {
                "Exit" : {
                     "keyBindings": ["Ctrl+A", "Alt+B"]
               }
            }
            """;

        var expectedKeys = new ConsoleKeyBinding[]
        {
            new(ConsoleModifiers.Control, ConsoleKey.A),
            new(ConsoleModifiers.Alt, ConsoleKey.B)
        };

        WriteSettings(settings);

        SettingsService.GetUserKeyBindings()
                         .Should().ContainKey("Exit")
                         .WhoseValue
                         .Should().BeEquivalentTo(expectedKeys);
    }
}
