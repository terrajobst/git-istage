using System.Collections.Concurrent;

namespace GitIStage.Services;

internal sealed class Application
{
    private readonly ConcurrentQueue<Message> _messageQueue = new();
    private readonly AutoResetEvent _messageQueueNonEmptyEvent = new(false);
    private readonly AutoResetEvent _readKeyEvent = new(false);
    private readonly UIService _uiService;
    private readonly CommandService _commandService;

    public Application(UIService uiService, CommandService commandService)
    {
        _uiService = uiService;
        _commandService = commandService;
    }

    public void Run()
    {
        SynchronizationContext.SetSynchronizationContext(new ApplicationSynchronizationContext(this));

        Console.TreatControlCAsInput = false;
        Console.CancelKeyPress += (_, _) =>
        {
            Exit();
        };

        _ = Task.Run(() =>
        {
            var width = Console.WindowWidth;
            var height = Console.WindowHeight;

            while (true)
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
        });

        _ = Task.Run(() =>
        {
            while (true)
            {
                _readKeyEvent.WaitOne();
                var key = Console.ReadKey(intercept: true);
                Enqueue(new KeyPressedMessage(key));
            }
        });

        _uiService.Show();
        try
        {
            var done = false;

            while (!done)
            {
                try
                {
                    var key = ReadKey();
                    var command = _commandService.GetCommand(key);
                    command?.Execute();
                }
                catch (ApplicationQuitException)
                {
                    done = true;
                }
            }
        }
        finally
        {
            _uiService.Hide();
        }
    }

    public void Exit()
    {
        Enqueue(new QuitMessage());
    }

    public void Invoke(Action action)
    {
        Enqueue(new InvokeMessage(action));
    }

    private void Enqueue(Message message)
    {
        _messageQueue.Enqueue(message);
        _messageQueueNonEmptyEvent.Set();
    }

    public ConsoleKeyInfo ReadKey()
    {
        _readKeyEvent.Set();

        var lastInfo = (ConsoleKeyInfo?)null;
        while (lastInfo is null)
        {
            _messageQueueNonEmptyEvent.WaitOne();

            while (_messageQueue.TryDequeue(out var message))
            {
                switch (message)
                {
                    case QuitMessage:
                        throw new ApplicationQuitException();

                    case InvokeMessage invokeMessage:
                        invokeMessage.Action();
                        break;

                    case KeyPressedMessage keyPressedMessage:
                        lastInfo = keyPressedMessage.KeyInfo;
                        break;

                    case WindowSizeChangedMessage:
                        _uiService.Resize();
                        break;
                }
            }
        }

        return lastInfo.Value;
    }

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
        private readonly Application _application;

        public ApplicationSynchronizationContext(Application application)
        {
            _application = application;
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            _application.Enqueue(new InvokeMessage(() => d(state)));
        }
    }

    private sealed class ApplicationQuitException : Exception
    {
    }
}
