using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Platform;
using Sidekick.Avalonia.BlazorWebView.Helper;

namespace Sidekick.Avalonia.BlazorWebView.NativeControls;

public class EmbedWebView : NativeControlHost
{
    public BlazorWebView BlazorWebView { get; private set; }

    private IPlatformHandle CreateLinux(IPlatformHandle parent)
    {
        LinuxHelper.EnsureInitialized();
        
        BlazorWebView = new BlazorWebView(App.ServiceProvider);
        BlazorWebView.Show();
        return new PlatformHandle(BlazorWebView.Handle, "BlazorWebView.Linux");
    }

    private void DestroyLinux(IPlatformHandle handle)
    {
        BlazorWebView?.Dispose();
        BlazorWebView = null;
        base.DestroyNativeControlCore(handle);
    }

    private IPlatformHandle CreateWin32(IPlatformHandle parent)
    {
        throw new NotImplementedException();
    }

    private void DestroyWin32(IPlatformHandle handle)
    {
        throw new NotImplementedException();
    }

    private IPlatformHandle CreateOSX(IPlatformHandle parent)
    {
        throw new NotImplementedException();
    }

    private void DestroyOSX(IPlatformHandle handle)
    {
        throw new NotImplementedException();
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) 
            return CreateLinux(parent);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
            return CreateWin32(parent);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) 
            return CreateOSX(parent);
        return base.CreateNativeControlCore(parent);
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            DestroyLinux(control);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            DestroyWin32(control);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            DestroyOSX(control);
        else
            base.DestroyNativeControlCore(control);
    }
}
