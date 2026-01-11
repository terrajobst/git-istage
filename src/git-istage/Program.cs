using System.Diagnostics;
using System.Text.Json;
using GitIStage.Services;
using GitIStage.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GitIStage;

internal static class Program
{
    private static async Task<int> Main()
    {
        try
        {
            await Run();
            return 0;
        }
        catch (GitIStageStartupException ex)
        {
            Console.WriteLine(ex.Message);
            return -100;
        }
        catch (Exception ex) when (!Debugger.IsAttached)
        {
            Console.WriteLine($"fatal: unhandled error {ex}");
            return -500;
        }
    }

    private static async Task Run()
    {
        using var host = Host.CreateDefaultBuilder()
                             .ConfigureServices(ConfigureServices)
                             .ConfigureLogging(l => l.ClearProviders())
                             .Build();
        host.Start();

        var application = host.Services.GetRequiredService<Application>();
        application.Run();

        await host.StopAsync();
        await host.WaitForShutdownAsync();
    }

    private static void ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<GitEnvironment>();
        serviceCollection.AddSingleton<UserEnvironment>();
        serviceCollection.AddSingleton<Application>();
        serviceCollection.AddSingleton<KeyboardService>();
        serviceCollection.AddSingleton<GitService>();
        serviceCollection.AddSingleton<PatchingService>();
        serviceCollection.AddSingleton<DocumentService>();
        serviceCollection.AddSingleton<UIService>();
        serviceCollection.AddSingleton<CommandService>();
        serviceCollection.AddSingleton<KeyBindingService>();
    }
}

internal sealed class GitIStageStartupException : Exception
{
    public GitIStageStartupException(string message)
        : base(message)
    {
    }
}

internal sealed class GitCommandFailedException : Exception
{
    public GitCommandFailedException(string message)
        : base(message)
    {
    }
}

internal static class ExceptionBuilder
{
    public static Exception NotAGitRepository()
    {
        return new GitIStageStartupException("fatal: Not a git repository");
    }

    public static Exception GitNotFound()
    {
        return new GitIStageStartupException("fatal: git is not in your path");
    }

    public static Exception KeyBindingsAreInvalidJson(string fileName, JsonException jsonException)
    {
        var message = $"fatal: user key bindings in '{fileName}' is not valid JSON: {jsonException.Message}";
        return new GitIStageStartupException(message);
    }

    public static Exception KeyBindingsReferToInvalidCommand(string fileName, string command)
    {
        var message = $"fatal: user key bindings in '{fileName}' refer to an invalid command {command}.";
        return new GitIStageStartupException(message);
    }

    public static Exception KeyBindingInvalidBinding(string fileName, string command, string bindingText)
    {
        var message = $"fatal: user key bindings in '{fileName}' uses an invalid key binding for command '{command}': '{bindingText}'.";
        return new GitIStageStartupException(message);
    }

    public static Exception GitCommandFailed(string command, IReadOnlyList<string> commandLog)
    {
        var message = $"Git command failed: git {command}{Environment.NewLine}{string.Join(Environment.NewLine, commandLog)}";
        return new GitCommandFailedException(message);
    }
}
