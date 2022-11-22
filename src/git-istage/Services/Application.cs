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

        while (!_done)
        {
            var width = Console.WindowWidth;
            var height = Console.WindowHeight;

            var key = Console.ReadKey(true);
            var command = _commandService.GetCommand(key);
            command?.Execute();

            if (width != Console.WindowWidth || height != Console.WindowHeight)
                _uiService.ResizeScreen();
        }

        _uiService.Hide();
    }

    public void Exit()
    {
        _done = true;
    }
}