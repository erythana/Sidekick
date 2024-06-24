using Sidekick.Common.Settings;

namespace Sidekick.Common.Keybinds;

/// <summary>
///     Interface for keybind handlers
/// </summary>
public abstract class KeybindHandler
{
    protected KeybindHandler(ISettingsService settingsService)
    {
        settingsService.OnSettingsChanged += OnOnSettingsChanged;
    }

    private void OnOnSettingsChanged()
    {
        _ = Task.Run(
            async () =>
            {
                Keybinds = await GetKeybinds();
            });
    }

    /// <summary>
    /// Gets the keybinds that this handler handles.
    /// </summary>
    /// <returns>The list of keybinds.</returns>
    public List<string?> Keybinds { get; private set; } =
    [
    ];

    /// <summary>
    /// Gets the keybinds that this handler handles.
    /// </summary>
    /// <returns>The list of keybinds.</returns>
    protected abstract Task<List<string?>> GetKeybinds();

    /// <summary>
    ///     When a keypress occurs, check if this keybind should be executed
    /// </summary>
    /// <param name="keybind">The keybind that was pressed</param>
    /// <returns>True if we need to execute this keybind</returns>
    public abstract bool IsValid(string keybind);

    /// <summary>
    ///     Executes when a valid keybind is detected
    /// </summary>
    /// <param name="keybind">The keybind that was pressed</param>
    /// <returns>A task</returns>
    public abstract Task Execute(string keybind);
}