using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

public interface IScreenCapturer : IDisposable
{
    byte[] CaptureFrameJpeg(out int width, out int height);
}

public class DesktopDuplicationCapturer : IScreenCapturer
{
    public int MonitorIndex { get; set; } = 0;

    public DesktopDuplicationCapturer() { }

    public DesktopDuplicationCapturer(int monitorIndex)
    {
        MonitorIndex = monitorIndex;
    }

    public byte[] CaptureFrameJpeg(out int width, out int height)
    {
        // Seleciona monitor com fallback
        var screens = Screen.AllScreens ?? Array.Empty<Screen>();
        Screen screen = Screen.PrimaryScreen ?? (screens.Length > 0 ? screens[0] : null!);

        if (screens.Length > 0)
        {
            if (MonitorIndex >= 0 && MonitorIndex < screens.Length)
                screen = screens[MonitorIndex];
            else
                screen = screens[0];
        }

        var bounds = screen.Bounds;
        width = bounds.Width;
        height = bounds.Height;

        using var bmp = new Bitmap(width, height);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bmp.Size);
        }

        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Jpeg);
        return ms.ToArray();
    }

    public void Dispose()
    {
        // Nada especial pra liberar aqui
    }
}
