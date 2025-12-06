using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

/// <summary>
/// Captura de tela usando GDI (CopyFromScreen)
/// Ignora janelas LAYERED automaticamente (como overlays)
/// </summary>
public class GdiScreenCapturer : IDisposable
{
    private readonly int _monitorIndex;
    private Rectangle _bounds;
    private Bitmap? _bitmap;
    private Graphics? _graphics;

    // Estruturas para capturar cursor
    [StructLayout(LayoutKind.Sequential)]
    private struct CURSORINFO
    {
        public int cbSize;
        public int flags;
        public IntPtr hCursor;
        public POINT ptScreenPos;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    private const int CURSOR_SHOWING = 0x0001;

    [DllImport("user32.dll")]
    private static extern bool GetCursorInfo(out CURSORINFO pci);

    [DllImport("user32.dll")]
    private static extern bool DrawIcon(IntPtr hDC, int x, int y, IntPtr hIcon);

    public GdiScreenCapturer(int monitorIndex)
    {
        _monitorIndex = monitorIndex;

        // Obter bounds do monitor
        var screens = System.Windows.Forms.Screen.AllScreens;
        if (monitorIndex >= 0 && monitorIndex < screens.Length)
        {
            _bounds = screens[monitorIndex].Bounds;
        }
        else
        {
            _bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
        }

        // Criar bitmap e graphics uma vez
        _bitmap = new Bitmap(_bounds.Width, _bounds.Height, PixelFormat.Format24bppRgb);
        _graphics = Graphics.FromImage(_bitmap);
    }

    /// <summary>
    /// Captura frame e retorna como JPEG
    /// </summary>
    public byte[] CaptureFrameJpeg(out int width, out int height)
    {
        width = _bounds.Width;
        height = _bounds.Height;

        if (_bitmap == null || _graphics == null)
        {
            return Array.Empty<byte>();
        }

        try
        {
            // CAPTURA USANDO GDI - Ignora janelas LAYERED automaticamente!
            // Isso é o equivalente ao seu exemplo com CopyFromScreen
            _graphics.CopyFromScreen(
                _bounds.X,
                _bounds.Y,
                0,
                0,
                _bounds.Size,
                CopyPixelOperation.SourceCopy
            );

            // Capturar cursor
            try
            {
                CURSORINFO cursorInfo = new CURSORINFO();
                cursorInfo.cbSize = Marshal.SizeOf(typeof(CURSORINFO));

                if (GetCursorInfo(out cursorInfo))
                {
                    if (cursorInfo.flags == CURSOR_SHOWING)
                    {
                        // Ajustar posição do cursor relativa ao monitor
                        int cursorX = cursorInfo.ptScreenPos.x - _bounds.X;
                        int cursorY = cursorInfo.ptScreenPos.y - _bounds.Y;

                        // Desenhar cursor se estiver dentro dos bounds
                        if (cursorX >= 0 && cursorX < _bounds.Width && cursorY >= 0 && cursorY < _bounds.Height)
                        {
                            IntPtr hdc = _graphics.GetHdc();
                            try
                            {
                                DrawIcon(hdc, cursorX, cursorY, cursorInfo.hCursor);
                            }
                            finally
                            {
                                _graphics.ReleaseHdc(hdc);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignorar erros de cursor
            }

            // Converter para JPEG
            using var ms = new MemoryStream();
            _bitmap.Save(ms, ImageFormat.Jpeg);
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GDI-CAPTURE] Erro ao capturar: {ex.Message}");
            return Array.Empty<byte>();
        }
    }

    public void Dispose()
    {
        _graphics?.Dispose();
        _bitmap?.Dispose();
    }
}
