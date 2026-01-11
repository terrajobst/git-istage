using GitIStage.UI;

namespace GitIStage.Services;

internal sealed class Application
{
    private readonly UIService _uiService;
    private readonly CommandService _commandService;

    private bool _done;

    public Application(UIService uiService, CommandService commandService)
    {
        _uiService = uiService;
        _commandService = commandService;
    }

    public void Run()
    {
        _uiService.Show();
        try
        {
            while (!_done)
            {
                var width = Terminal.WindowWidth;
                var height = Terminal.WindowHeight;

                var key = Terminal.ReadKey();
                var command = _commandService.GetCommand(key);
                command?.Execute();

                if (width != Terminal.WindowWidth || height != Terminal.WindowHeight)
                    _uiService.ResizeScreen();
            }
        }
        finally
        {
            _uiService.Hide();
        }
    }

    public void Exit()
    {
        _done = true;
    }
}