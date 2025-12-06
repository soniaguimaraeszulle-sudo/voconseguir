using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Overlay fullscreen que bloqueia interações e exibe "TRAVA"
/// Executado em thread separada para evitar deadlock com UI principal
/// </summary>
public class ScreenLockOverlay
{
    private Form? _lockForm;
    private Thread _uiThread;
    private bool _isLocked = false;
    private bool _showBehind = false;
    private volatile bool _shouldClose = false;
    private readonly object _lockObj = new object();

    // Constantes para janela Layered (não capturada)
    private const int WS_EX_LAYERED = 0x80000;
    private const int WS_EX_TRANSPARENT = 0x20;
    private const int WS_EX_TOPMOST = 0x8;
    private const int GWL_EXSTYLE = -20;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", EntryPoint = "BlockInput")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool BlockInput([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)] bool fBlockIt);

    public ScreenLockOverlay()
    {
        Console.WriteLine("[LOCK] Inicializando overlay em thread separada...");

        // Thread separada para UI
        _uiThread = new Thread(() =>
        {
            try
            {
                _lockForm = new LockForm(this);
                _lockForm.Opacity = 0; // Começa invisível
                _lockForm.Show();
                Console.WriteLine("[LOCK] Form criado e rodando message loop");
                Application.Run(_lockForm);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOCK] ERRO na thread UI: {ex}");
            }
        })
        {
            IsBackground = false, // NÃO background para garantir que não morre
            Name = "OverlayThread"
        };
        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.Start();

        // Aguardar form ser criado
        Thread.Sleep(1000);
        Console.WriteLine("[LOCK] Overlay inicializado");
    }

    public void SetLocked(bool locked)
    {
        lock (_lockObj)
        {
            if (_isLocked == locked) return;
            _isLocked = locked;

            if (locked)
            {
                Console.WriteLine("[LOCK] ========== TRAVA ATIVADA ==========");

                try
                {
                    if (_lockForm != null && !_lockForm.IsDisposed)
                    {
                        _lockForm.Invoke(new Action(() =>
                        {
                            Console.WriteLine($"[LOCK] Configurando overlay - Handle válido: {_lockForm.Handle != IntPtr.Zero}");

                            // Configurar e mostrar
                            _lockForm.FormBorderStyle = FormBorderStyle.None;
                            _lockForm.WindowState = FormWindowState.Maximized;
                            _lockForm.TopMost = true;
                            _lockForm.Opacity = 1.0;
                            _lockForm.Visible = true;
                            _lockForm.Show();
                            _lockForm.BringToFront();
                            _lockForm.Activate();

                            Console.WriteLine($"[LOCK] Overlay configurado: Visible={_lockForm.Visible}, Opacity={_lockForm.Opacity}, TopMost={_lockForm.TopMost}");

                            // Bloquear input
                            try
                            {
                                InputBlocker.Start();
                                BlockInput(true);
                                Console.WriteLine("[LOCK] Input bloqueado");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[LOCK] Erro ao bloquear input: {ex.Message}");
                            }

                            _lockForm.Refresh();
                        }));
                    }
                    else
                    {
                        Console.WriteLine("[LOCK] ERRO: Form é null ou disposed!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LOCK] ERRO ao ativar overlay: {ex}");
                }
            }
            else
            {
                Console.WriteLine("[LOCK] ========== TRAVA DESATIVADA ==========");

                try
                {
                    if (_lockForm != null && !_lockForm.IsDisposed)
                    {
                        _lockForm.Invoke(new Action(() =>
                        {
                            _lockForm.Opacity = 0;
                            _lockForm.Hide();
                            Console.WriteLine("[LOCK] Overlay escondido");

                            // Desbloquear input
                            try
                            {
                                InputBlocker.Stop();
                                BlockInput(false);
                                Console.WriteLine("[LOCK] Input desbloqueado");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[LOCK] Erro ao desbloquear input: {ex.Message}");
                            }
                        }));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LOCK] ERRO ao desativar overlay: {ex}");
                }
            }
        }
    }



    public bool IsLocked => _isLocked;
    public bool ShowBehind => _showBehind;

    public void Close()
    {
        try
        {
            _shouldClose = true;
            _lockForm?.Invoke(new Action(() =>
            {
                _lockForm?.Close();
            }));

            // Aguardar thread terminar
            if (_uiThread?.IsAlive == true)
            {
                if (!_uiThread.Join(2000))
                {
                    Console.WriteLine("[LOCK] Aviso: thread não terminou a tempo");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LOCK] Erro ao fechar: {ex.Message}");
        }
    }

    // Form interna para renderizar o overlay
    private class LockForm : Form
    {
        private readonly ScreenLockOverlay _parent;

        public LockForm(ScreenLockOverlay parent)
        {
            _parent = parent;

            Console.WriteLine("[LOCK-FORM] Criando LockForm");

            // Configuração mínima necessária
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            TopMost = true;
            ShowInTaskbar = false;
            ControlBox = false;
            Text = "Lock Overlay";
            BackColor = Color.Red; // Vermelho para teste
            ForeColor = Color.Yellow;
            DoubleBuffered = true;
            StartPosition = FormStartPosition.Manual;
            Bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;

            Console.WriteLine($"[LOCK-FORM] Bounds={Bounds}, Screen={System.Windows.Forms.Screen.PrimaryScreen.Bounds}");

            // Bloquear entrada quando travado
            KeyDown += (s, e) =>
            {
                if (_parent.IsLocked)
                    e.SuppressKeyPress = true;
            };
            KeyPreview = true;

            // Desabilitar fechar por Alt+F4
            FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing && !_parent._shouldClose)
                    e.Cancel = true;
            };

            // Quando a janela for criada, configurar como LAYERED para não ser capturada
            HandleCreated += (s, e) =>
            {
                try
                {
                    int currentStyle = GetWindowLong(Handle, GWL_EXSTYLE);
                    // Adicionar WS_EX_LAYERED mas SEM WS_EX_TRANSPARENT (para capturar input)
                    SetWindowLong(Handle, GWL_EXSTYLE, currentStyle | WS_EX_LAYERED);
                    Console.WriteLine($"[LOCK-FORM] Janela configurada como LAYERED (não será capturada)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LOCK-FORM] Erro ao configurar LAYERED: {ex.Message}");
                }
            };

            Console.WriteLine("[LOCK-FORM] Form criado");
        }

        // Sobrescrever CreateParams para adicionar WS_EX_LAYERED desde o início
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TOPMOST;
                return cp;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            Console.WriteLine($"[LOCK-PAINT] OnPaint - IsLocked={_parent.IsLocked}, Visible={Visible}, Opacity={Opacity}");

            if (!_parent.IsLocked)
                return;

            // Fundo VERMELHO
            g.Clear(Color.Red);

            // Texto AMARELO gigante
            using (var font = new Font("Arial", 250, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.Yellow))
            {
                var text = "TRAVA";
                var size = g.MeasureString(text, font);
                float x = (Width - size.Width) / 2;
                float y = (Height - size.Height) / 2;
                g.DrawString(text, font, brush, x, y);
                Console.WriteLine($"[LOCK-PAINT] Texto desenhado: {text} em {x},{y}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Só permitir fechar se o pai solicitar ou se o Windows estiver fechando
            if (_parent._shouldClose)
                base.OnFormClosing(e);
            else
                e.Cancel = true;
        }

        // Interceptar mensagens de entrada para evitar que cheguem às janelas abaixo
        protected override void WndProc(ref Message m)
        {
            if (_parent.IsLocked)
            {
                const int WM_MOUSEMOVE = 0x0200;
                const int WM_LBUTTONDOWN = 0x0201;
                const int WM_LBUTTONUP = 0x0202;
                const int WM_RBUTTONDOWN = 0x0204;
                const int WM_RBUTTONUP = 0x0205;
                const int WM_MBUTTONDOWN = 0x0207;
                const int WM_MOUSEWHEEL = 0x020A;
                const int WM_MOUSEHWHEEL = 0x020E;
                const int WM_KEYDOWN = 0x0100;
                const int WM_KEYUP = 0x0101;
                const int WM_CHAR = 0x0102;
                const int WM_SYSKEYDOWN = 0x0104;
                const int WM_SYSKEYUP = 0x0105;

                int msg = m.Msg;
                if (msg == WM_MOUSEMOVE || msg == WM_LBUTTONDOWN || msg == WM_LBUTTONUP ||
                    msg == WM_RBUTTONDOWN || msg == WM_RBUTTONUP || msg == WM_MBUTTONDOWN ||
                    msg == WM_MOUSEWHEEL || msg == WM_MOUSEHWHEEL || msg == WM_KEYDOWN ||
                    msg == WM_KEYUP || msg == WM_CHAR || msg == WM_SYSKEYDOWN || msg == WM_SYSKEYUP)
                {
                    // consumir a mensagem — não chamar base.WndProc para evitar repassar
                    return;
                }
            }

            base.WndProc(ref m);
        }
    }
}
