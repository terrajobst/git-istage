using System.Collections.Concurrent;

namespace GitIStage.UI;

// TODO: Couple of issues:
//
// - Quitting isn't done nicely

public static class Terminal
{
    private static readonly ConcurrentQueue<Message> MessageQueue = new();
    private static readonly AutoResetEvent MessageQueueNonEmptyEvent = new(false);
    private static readonly CancellationTokenSource CancellationTokenSource = new();
    private static readonly AutoResetEvent ReadKeyEvent = new(false);

    private static void Enqueue(Message message)
    {
        MessageQueue.Enqueue(message);
        MessageQueueNonEmptyEvent.Set();
    }
    
    public static void Initialize()
    {
        SynchronizationContext.SetSynchronizationContext(new ApplicationSynchronizationContext());
        
        Console.TreatControlCAsInput = false;
        Console.CancelKeyPress += (_, _) =>
        {
            Enqueue(new QuitMessage());
        };
        
        Task.Run(() =>
        {
            var width = Console.WindowWidth;
            var height = Console.WindowHeight;

            while (!CancellationTokenSource.Token.IsCancellationRequested)
            {
                if (width != Console.WindowWidth ||
                    height != Console.WindowHeight)
                {
                    width = Console.WindowWidth;
                    height = Console.WindowHeight;
                    Enqueue(new WindowSizeChangedMessage());
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(100));
            }
        }, CancellationTokenSource.Token);

        Task.Run(() =>
        {
            while (!CancellationTokenSource.Token.IsCancellationRequested)
            {
                ReadKeyEvent.WaitOne();
                var key = Console.ReadKey(intercept: true);
                Enqueue(new KeyPressedMessage(key));
            }
        }, CancellationTokenSource.Token);
    }

    public static void Clear()
    {
        Console.Clear();
    }

    public static ConsoleKeyInfo ReadKey()
    {
        ReadKeyEvent.Set();

        var lastInfo = (ConsoleKeyInfo?)null;
        while (lastInfo is null)
        {
            MessageQueueNonEmptyEvent.WaitOne();

            while (MessageQueue.TryDequeue(out var message))
            {
                switch (message)
                {
                    case QuitMessage:
                        // TODO: This isn't robust as folks can reconfigure the keys
                        // We simplify simulate the key sequence Esc + Q
                        // This will make sure that we're in the main ReadKey() loop and then simply request to quit.
                        Enqueue(new KeyPressedMessage(new ConsoleKeyInfo((char)27,  ConsoleKey.Escape, false, false, false)));
                        Enqueue(new KeyPressedMessage(new ConsoleKeyInfo('q',  ConsoleKey.Q, false, false, false)));
                        CancellationTokenSource.Cancel();
                        break;

                    case InvokeMessage invokeMessage:
                        invokeMessage.Action();
                        break;

                    case KeyPressedMessage keyPressedMessage:
                        lastInfo = keyPressedMessage.KeyInfo;
                        break;

                    case WindowSizeChangedMessage:
                        WindowSizeChanged?.Invoke(null, EventArgs.Empty);
                        break;
                }
            }
        }

        return lastInfo.Value;
    }

    public static void Write(ReadOnlySpan<char> value)
    {
        Console.Write(value);
    }

    public static void WriteLine(ReadOnlySpan<char> value)
    {
        Console.WriteLine(value);
    }

    public static int WindowHeight => Console.WindowHeight;
    
    public static int WindowWidth => Console.WindowWidth;

    public static event EventHandler? WindowSizeChanged;

    private abstract class Message
    {
    }

    private sealed class QuitMessage : Message
    {
    }
    
    private sealed class WindowSizeChangedMessage : Message
    {
    }
    
    private sealed class KeyPressedMessage : Message
    {
        public KeyPressedMessage(ConsoleKeyInfo keyInfo)
        {
            KeyInfo = keyInfo;
        }

        public ConsoleKeyInfo KeyInfo { get; }
    }

    private sealed class InvokeMessage : Message
    {
        public InvokeMessage(Action action)
        {
            Action = action;
        }

        public Action Action { get; }
    }

    private sealed class ApplicationSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state)
        {
            Enqueue(new InvokeMessage(() => d(state)));
        }
    }
}