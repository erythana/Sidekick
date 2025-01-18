namespace Sidekick.Avalonia.BlazorWebView.Helper;

internal class LinuxHelper
{
    private static bool isInitialized;

    public static void EnsureInitialized()
    {
        if (isInitialized) return;
        isInitialized = true;

        WebKit.Module.Initialize(); //TODO: only needs to be called once probably per app lifetime
    }
}
