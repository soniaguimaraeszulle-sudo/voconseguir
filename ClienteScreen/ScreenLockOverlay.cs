using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Overlay fullscreen que bloqueia interações visualmente e exibe "TRAVA"
/// Executado em thread separada para evitar deadlock com UI principal
/// NÃO requer privilégios de administrador - apenas bloqueio visual
/// </summary>
public class ScreenLockOverlay
{
    private Form? _lockForm;
    private Thread _uiThread;
    private bool _isLocked = false;
    private bool _showBehind = false;
    private volatile bool _shouldClose = false;
    private readonly object _lockObj = new object();

    // DWM attribute to exclude a window from capture (may not be supported on all Windows versions)
    private const int DWMWA_EXCLUDED_FROM_CAPTURE = 13;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    public ScreenLockOverlay()
    {
        Console.WriteLine("[LOCK] Inicializando overlay de trava (bloqueio visual - sem admin)");

        // Iniciar UI da trava em thread separada
        _uiThread = new Thread(UIThreadProc)
        {
            IsBackground = true,
            Name = "ScreenLockOverlayThread"
        };
        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.Start();

        // Aguardar que a form foi criada
        Thread.Sleep(500);
    }

    private void UIThreadProc()
    {
        try
        {
            _lockForm = new LockForm(this);
            _lockForm.Show();
            Application.Run(_lockForm);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LOCK] Erro na thread UI: {ex.Message}");
        }
    }

    public void SetLocked(bool locked)
    {
        lock (_lockObj)
        {
            if (_isLocked == locked) return;
            _isLocked = locked;

            if (locked)
            {
                // Ao travar: CLIENTE vê o overlay E SERVIDOR também vê o overlay (não excluir da captura)
                _showBehind = false; // servidor também vê o overlay inicialmente
                Console.WriteLine("[LOCK] ========== TRAVA ATIVADA ==========");
                Console.WriteLine("[LOCK] Cliente E Servidor veem o overlay TRAVA");

                    _lockForm?.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            if (_lockForm != null)
                            {
                                // NÃO excluir da captura - servidor deve ver o overlay também
                                int val = 0;
                                var hr = DwmSetWindowAttribute(_lockForm.Handle, DWMWA_EXCLUDED_FROM_CAPTURE, ref val, sizeof(int));
                                Console.WriteLine($"[LOCK] DwmSetWindowAttribute(EXCLUDED_FROM_CAPTURE=0) - Servidor VÊ o overlay: 0x{hr:X}");

                                // Garantir que o overlay fique em primeiro plano e capture entrada
                                try
                                {
                                    Console.WriteLine("[LOCK] Ativando overlay: TopMost, BringToFront, Maximize, Focus");
                                    _lockForm.TopMost = true;
                                    _lockForm.BringToFront();
                                    _lockForm.WindowState = FormWindowState.Maximized;
                                    _lockForm.Focus();
                                    _lockForm.Activate();
                                    SetForegroundWindow(_lockForm.Handle);
                                    Console.WriteLine("[LOCK] Overlay ativado com sucesso");
                                }
                                catch (Exception exf)
                                {
                                    Console.WriteLine($"[LOCK] Aviso ao forçar foco do overlay: {exf.Message}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[LOCK] Erro ao setar DWM attribute na ativação: {ex.Message}");
                        }
                        _lockForm?.Refresh();
                    }));
            }
            else
            {
                // Ao destravar: desativar overlay completamente
                _showBehind = false;
                Console.WriteLine("[LOCK] ========== TRAVA DESATIVADA ==========");

                _lockForm?.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (_lockForm != null)
                        {
                            int val = 0;
                            var hr = DwmSetWindowAttribute(_lockForm.Handle, DWMWA_EXCLUDED_FROM_CAPTURE, ref val, sizeof(int));
                            Console.WriteLine($"[LOCK] DwmSetWindowAttribute(EXCLUDED_FROM_CAPTURE=0): 0x{hr:X}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[LOCK] Erro ao resetar DWM attribute na desativação: {ex.Message}");
                    }
                    _lockForm?.Refresh();
                }));
            }
        }
    }

    /// <summary>
    /// Alterna visualização "por trás" do overlay.
    /// Cliente continua travado, mas servidor pode ver por trás do overlay.
    /// </summary>
    public void SetPeekBehind(bool peek)
    {
        lock (_lockObj)
        {
            // Só funciona se estiver travado
            if (!_isLocked)
            {
                Console.WriteLine("[LOCK] AVISO: SetPeekBehind chamado mas cliente não está travado!");
                return;
            }

            _showBehind = peek;

            if (peek)
            {
                Console.WriteLine("[LOCK] ========== SERVIDOR VENDO POR TRÁS ==========");
                Console.WriteLine("[LOCK] Cliente continua travado, mas servidor vê a tela real");

                _lockForm?.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (_lockForm != null)
                        {
                            // Excluir da captura - servidor vê por trás
                            int val = 1;
                            var hr = DwmSetWindowAttribute(_lockForm.Handle, DWMWA_EXCLUDED_FROM_CAPTURE, ref val, sizeof(int));
                            Console.WriteLine($"[LOCK] DwmSetWindowAttribute(EXCLUDED_FROM_CAPTURE=1) - Servidor vê POR TRÁS: 0x{hr:X}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[LOCK] Erro ao ativar peek behind: {ex.Message}");
                    }
                }));
            }
            else
            {
                Console.WriteLine("[LOCK] ========== SERVIDOR VOLTOU A VER OVERLAY ==========");
                Console.WriteLine("[LOCK] Cliente continua travado, servidor vê o overlay novamente");

                _lockForm?.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (_lockForm != null)
                        {
                            // Re-incluir na captura - servidor vê overlay
                            int val = 0;
                            var hr = DwmSetWindowAttribute(_lockForm.Handle, DWMWA_EXCLUDED_FROM_CAPTURE, ref val, sizeof(int));
                            Console.WriteLine($"[LOCK] DwmSetWindowAttribute(EXCLUDED_FROM_CAPTURE=0) - Servidor VÊ overlay: 0x{hr:X}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[LOCK] Erro ao desativar peek behind: {ex.Message}");
                    }
                }));
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
        private System.Windows.Forms.Timer _updateTimer;
        private System.Windows.Forms.Timer _refocusTimer;

        public LockForm(ScreenLockOverlay parent)
        {
            _parent = parent;

            Console.WriteLine("[LOCK-FORM] Criando LockForm (overlay fullscreen)");

            // Configurar form como overlay fullscreen
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            TopMost = true;
            ShowInTaskbar = false;
            ControlBox = false;
            MaximizeBox = false;
            MinimizeBox = false;
            Text = "";
            BackColor = Color.Black;
            ForeColor = Color.White;
            DoubleBuffered = true;
            Opacity = 0;
            KeyPreview = true;
            AllowDrop = false;

            // Prevenir que seja movido ou redimensionado
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(0, 0);
            Size = Screen.PrimaryScreen.Bounds.Size;

            Console.WriteLine($"[LOCK-FORM] Form configurado: TopMost={TopMost}, Opacity={Opacity}, Enabled={Enabled}");

            // Timer para animar transições
            _updateTimer = new System.Windows.Forms.Timer();
            _updateTimer.Interval = 50;
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();

            // Timer para manter foco quando travado (força foco a cada 500ms)
            _refocusTimer = new System.Windows.Forms.Timer();
            _refocusTimer.Interval = 500;
            _refocusTimer.Tick += RefocusTimer_Tick;
            _refocusTimer.Start();

            // Bloquear TODAS as teclas quando travado
            KeyDown += (s, e) =>
            {
                if (_parent.IsLocked)
                {
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                    Console.WriteLine($"[LOCK-FORM] Bloqueou tecla: {e.KeyCode}");
                }
            };

            KeyPress += (s, e) =>
            {
                if (_parent.IsLocked)
                {
                    e.Handled = true;
                    Console.WriteLine($"[LOCK-FORM] Bloqueou KeyPress: {e.KeyChar}");
                }
            };

            KeyUp += (s, e) =>
            {
                if (_parent.IsLocked)
                {
                    e.Handled = true;
                }
            };

            // Capturar TODOS os eventos do mouse
            MouseDown += (s, e) =>
            {
                if (_parent.IsLocked)
                {
                    Console.WriteLine($"[LOCK-FORM] Bloqueou MouseDown: {e.Button} em ({e.X}, {e.Y})");
                }
            };
            MouseMove += (s, e) => { /* consumir silenciosamente */ };
            MouseClick += (s, e) =>
            {
                if (_parent.IsLocked)
                {
                    Console.WriteLine($"[LOCK-FORM] Bloqueou MouseClick: {e.Button}");
                }
            };
            MouseDoubleClick += (s, e) =>
            {
                if (_parent.IsLocked)
                {
                    Console.WriteLine($"[LOCK-FORM] Bloqueou MouseDoubleClick: {e.Button}");
                }
            };
            MouseWheel += (s, e) =>
            {
                if (_parent.IsLocked)
                {
                    Console.WriteLine($"[LOCK-FORM] Bloqueou MouseWheel: {e.Delta}");
                }
            };

            // Prevenir que perca o foco
            LostFocus += (s, e) =>
            {
                if (_parent.IsLocked)
                {
                    Console.WriteLine("[LOCK-FORM] AVISO: Overlay perdeu foco! Recuperando...");
                    try
                    {
                        TopMost = true;
                        BringToFront();
                        Activate();
                        Focus();
                    }
                    catch { }
                }
            };

            // Desabilitar fechar por Alt+F4
            FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing && !_parent._shouldClose)
                {
                    e.Cancel = true;
                    Console.WriteLine("[LOCK-FORM] Tentativa de fechar bloqueada (Alt+F4 ou X)");
                }
            };

            Console.WriteLine("[LOCK-FORM] LockForm criado com sucesso");
        }

        private void RefocusTimer_Tick(object? sender, EventArgs e)
        {
            if (_parent.IsLocked && !Focused)
            {
                try
                {
                    TopMost = true;
                    BringToFront();
                    Activate();
                    Focus();
                }
                catch { }
            }
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            if (_parent._shouldClose)
                return;

            double oldOpacity = Opacity;
            bool oldEnabled = Enabled;

            if (_parent.IsLocked)
            {
                // Sempre manter a trava visível no cliente quando travado.
                if (Opacity < 0.95)
                    Opacity = Math.Min(1.0, Opacity + 0.15);

                if (!Enabled)
                {
                    Enabled = true;
                    Console.WriteLine("[LOCK-TIMER] Reabilitando form para bloquear entrada");
                }
            }
            else
            {
                // Fade out quando desbloqueado
                if (Opacity > 0.05)
                    Opacity = Math.Max(0, Opacity - 0.15);
                else if (Enabled)
                {
                    Enabled = false;
                }
            }

            if (Math.Abs(oldOpacity - Opacity) > 0.01 || oldEnabled != Enabled)
            {
                Console.WriteLine($"[LOCK-TIMER] IsLocked={_parent.IsLocked}, Opacity: {oldOpacity:F2} → {Opacity:F2}, Enabled={Enabled}");
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.Clear(Color.Black);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Se não está travado e opacity é baixa, não renderizar nada
            if (!_parent.IsLocked && Opacity < 0.1)
            {
                return;
            }

            if (_parent.IsLocked)
            {
                // Tela travada: fundo preto + texto "TRAVA"
                using (var font = new Font("Arial", 120, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.White))
                using (var shadowBrush = new SolidBrush(Color.DarkGray))
                {
                    var text = "TRAVA";
                    var size = g.MeasureString(text, font);

                    // Sombra
                    g.DrawString(text, font, shadowBrush, (Width - (int)size.Width) / 2 + 3, (Height - (int)size.Height) / 2 + 3);

                    // Texto principal
                    g.DrawString(text, font, brush, (Width - (int)size.Width) / 2, (Height - (int)size.Height) / 2);
                }

                // Informação embaixo
                using (var font = new Font("Arial", 14))
                using (var brush = new SolidBrush(Color.LimeGreen))
                {
                    var text = "Cliente Travado - Aguarde autorização do servidor";
                    g.DrawString(text, font, brush, 20, Height - 60);
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Só permitir fechar se o pai solicitar
            if (!_parent._shouldClose)
            {
                e.Cancel = true;
                return;
            }

            _updateTimer?.Stop();
            _refocusTimer?.Stop();
            base.OnFormClosing(e);
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
                const int WM_MBUTTONUP = 0x0208;
                const int WM_MOUSEWHEEL = 0x020A;
                const int WM_MOUSEHWHEEL = 0x020E;
                const int WM_KEYDOWN = 0x0100;
                const int WM_KEYUP = 0x0101;
                const int WM_CHAR = 0x0102;
                const int WM_SYSKEYDOWN = 0x0104;
                const int WM_SYSKEYUP = 0x0105;
                const int WM_SYSCHAR = 0x0106;

                int msg = m.Msg;

                // Consumir TODAS as mensagens de entrada quando travado
                if (msg == WM_MOUSEMOVE || msg == WM_LBUTTONDOWN || msg == WM_LBUTTONUP ||
                    msg == WM_RBUTTONDOWN || msg == WM_RBUTTONUP || msg == WM_MBUTTONDOWN || msg == WM_MBUTTONUP ||
                    msg == WM_MOUSEWHEEL || msg == WM_MOUSEHWHEEL || msg == WM_KEYDOWN ||
                    msg == WM_KEYUP || msg == WM_CHAR || msg == WM_SYSKEYDOWN || msg == WM_SYSKEYUP || msg == WM_SYSCHAR)
                {
                    // consumir a mensagem — não chamar base.WndProc para evitar repassar
                    return;
                }
            }

            base.WndProc(ref m);
        }

        // Sobrescrever CreateParams para adicionar estilos extras
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;

                // WS_EX_TOPMOST: sempre no topo
                cp.ExStyle |= 0x00000008;

                // WS_EX_NOACTIVATE: não roubar foco de outras janelas (apenas quando não travado)
                // Quando travado, queremos roubar o foco
                if (!_parent.IsLocked)
                {
                    cp.ExStyle |= 0x08000000;
                }

                return cp;
            }
        }
    }
}
