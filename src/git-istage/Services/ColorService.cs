using System.Text.Json;
using System.Text.Json.Serialization;
using GitIStage.UI;

namespace GitIStage.Services;

internal sealed class ColorService : IDisposable
{
    private readonly string _fileName;
    private readonly FileSystemWatcher _watcher;

    public ColorService(UserEnvironment userEnvironment)
    {
        var dataDirectory = Path.Join(userEnvironment.UserHomeDirectory, ".git-istage");
        _fileName = Path.Join(dataDirectory, "colors.json");
        _watcher = new FileSystemWatcher();
        _watcher.Path = dataDirectory;
        _watcher.Changed += Watcher_Changed;
        _watcher.EnableRaisingEvents = true;
        
        InitializeDefaults();
        UpdateColors();
    }

    public void Dispose()
    {
        _watcher.Dispose();
    }

    public TextColor DiffContext { get; private set; }

    public TextColor DiffMeta { get; private set; }

    public TextColor DiffHunk { get; private set; }

    public TextColor DiffFunction { get; private set; }

    public TextColor DiffOld { get; private set; }

    public TextColor DiffNew { get; private set; }

    public TextColor DiffWhitespaceError { get; private set; }

    private void UpdateColors()
    {
        if (!File.Exists(_fileName))
        {
            InitializeDefaults();
            return;
        }
        
        try
        {
            var content = File.ReadAllText(_fileName);
            var options = new JsonSerializerOptions(JsonSerializerDefaults.General)
            {
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            var data = JsonSerializer.Deserialize<Dictionary<string, ColorJsonShape?>>(content, options);
            UpdateColors(data);
        }
        catch (Exception)
        {
        }
    }

    private void UpdateColors(Dictionary<string, ColorJsonShape?>? data)
    {
        DiffContext = GetTextColor(data, "diff.context");
        DiffMeta = GetTextColor(data, "diff.meta");
        DiffHunk = GetTextColor(data, "diff.frag");
        DiffFunction = GetTextColor(data, "diff.func");
        DiffOld = GetTextColor(data, "diff.old");
        DiffNew = GetTextColor(data, "diff.new");
        DiffWhitespaceError = GetTextColor(data, "diff.whitespace");
    }

    private static TextColor GetTextColor(Dictionary<string, ColorJsonShape?>? data, string key)
    {
        if (data is null ||
            !data.TryGetValue(key, out var jsonColor) ||
            jsonColor is null ||
            !ConsoleColor256.TryParse(jsonColor.Foreground, out var foreground) ||
            !ConsoleColor256.TryParse(jsonColor.Background, out var background)) 
        {
            return new TextColor(ConsoleColor.DarkGray, ConsoleColor.Black);
        }

        return new TextColor(foreground, background);
    }

    private void InitializeDefaults()
    {
        DiffContext = new TextColor(ConsoleColor.DarkGray, ConsoleColor.Black);
        DiffMeta = new TextColor(ConsoleColor.White, ConsoleColor.Black);
        DiffHunk = new TextColor(ConsoleColor.DarkCyan, ConsoleColor.Black);
        DiffFunction = new TextColor(ConsoleColor.DarkGray, ConsoleColor.Black);
        DiffOld = new TextColor(ConsoleColor.DarkGreen, ConsoleColor.Black);
        DiffNew = new TextColor(ConsoleColor.DarkRed, ConsoleColor.Black);
        DiffWhitespaceError = new TextColor(ConsoleColor.White, ConsoleColor.DarkRed);
    }

    private void Watcher_Changed(object sender, FileSystemEventArgs e)
    {
        if (!string.Equals(e.FullPath, _fileName, StringComparison.Ordinal))
            return;
        
        UpdateColors();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Changed;

    private sealed class ColorJsonShape
    {
        [JsonPropertyName("foreground")]
        public string? Foreground { get; init; }

        [JsonPropertyName("background")]
        public string? Background { get; init; }
    }
}

/*
color

The value for a variable that takes a color is a list of colors (at most two,
one for foreground and one for background) and attributes (as many as you want),
separated by spaces.

The basic colors accepted are normal, black, red, green, yellow, blue, magenta,
cyan, white and default. The first color given is the foreground; the second is
the background. All the basic colors except normal and default have a bright
variant that can be specified by prefixing the color with bright, like
brightred.

The color normal makes no change to the color. It is the same as an empty
string, but can be used as the foreground color when specifying a background
color alone (for example, "normal red").

The color default explicitly resets the color to the terminal default, for
example to specify a cleared background. Although it varies between terminals,
this is usually not the same as setting to "white black".

Colors may also be given as numbers between 0 and 255; these use ANSI 256-color
mode (but note that not all terminals may support this). If your terminal
supports it, you may also specify 24-bit RGB values as hex, like #ff0ab3.

The accepted attributes are bold, dim, ul, blink, reverse, italic, and strike
(for crossed-out or "strikethrough" letters). The position of any attributes
with respect to the colors (before, after, or in between), doesn’t matter.
Specific attributes may be turned off by prefixing them with no or no- (e.g.,
noreverse, no-ul, etc).

The pseudo-attribute reset resets all colors and attributes before applying the
specified coloring. For example, reset green will result in a green foreground
and default background without any active attributes.

An empty color string produces no color effect at all. This can be used to avoid
coloring specific elements without disabling color entirely.

For git’s pre-defined color slots, the attributes are meant to be reset at the
beginning of each item in the colored output. So setting color.decorate.branch
to black will paint that branch name in a plain black, even if the previous
thing on the same output line (e.g. opening parenthesis before the list of
branch names in log --decorate output) is set to be painted with bold or some
other attribute. However, custom log formats may do more complicated and layered
coloring, and the negated forms may be useful there.

*/


/*

color.diff.<slot>

Use customized color for diff colorization. <slot> specifies
which part of the patch to use the specified color, and is one of

* context. The context text, (`plain` is a historical synonym), including the
  leading space.
* meta. Entire header.
* frag. Hunk header, everything in between (and including) the `@@`-markers.
* func. Function in hunk header, not including the space after the hunk header.
* old. The removed lines, including the `-` prefix.
* new. The added lines, including the `+` prefix.
* whitespace. Highlighting whitespace errors, not regular whitespace.
*/