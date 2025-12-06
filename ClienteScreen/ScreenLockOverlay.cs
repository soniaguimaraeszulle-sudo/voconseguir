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

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern bool BlockInput(bool fBlockIt);

    public ScreenLockOverlay()
    {
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
                // Ao travar: manter TRAVA visível no cliente
                _showBehind = false;
                Console.WriteLine("[LOCK] ========== TRAVA ATIVADA ==========");

                _lockForm?.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (_lockForm != null)
                        {
                            // Garantir que o overlay fique em primeiro plano e capture entrada
                            _lockForm.TopMost = true;
                            _lockForm.BringToFront();
                            _lockForm.WindowState = FormWindowState.Maximized;
                            _lockForm.Activate();
                            SetForegroundWindow(_lockForm.Handle);
                            Console.WriteLine("[LOCK] Overlay trazido para frente");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[LOCK] Erro ao ativar overlay: {ex.Message}");
                    }
                    _lockForm?.Refresh();

                    // Ativar bloqueio local de input para evitar interação do usuário local
                    try
                    {
                        InputBlocker.Start();
                        var ok = BlockInput(true);
                        Console.WriteLine($"[LOCK] BlockInput(true) called, return={ok}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[LOCK] Aviso: falha ao bloquear input: {ex.Message}");
                    }
                }));
            }
            else
            {
                // Ao destravar
                _showBehind = false;
                Console.WriteLine("[LOCK] ========== TRAVA DESATIVADA ==========");

                _lockForm?.BeginInvoke(new Action(() =>
                {
                    _lockForm?.Refresh();

                    // Desativar bloqueio local
                    try
                    {
                        InputBlocker.Stop();
                        var ok = BlockInput(false);
                        Console.WriteLine($"[LOCK] BlockInput(false) called, return={ok}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[LOCK] Aviso: falha ao desbloquear input: {ex.Message}");
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
            Text = "";
            BackColor = Color.Black;
            ForeColor = Color.White;
            DoubleBuffered = true;
            Opacity = 0;

            Console.WriteLine($"[LOCK-FORM] Form configurado: TopMost={TopMost}, Opacity={Opacity}, Enabled={Enabled}");

            // Timer para animar transições
            _updateTimer = new System.Windows.Forms.Timer();
            _updateTimer.Interval = 50;
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();

            // Bloquear entrada quando travado
            KeyDown += (s, e) => 
            { 
                if (_parent.IsLocked)
                {
                    e.SuppressKeyPress = true;
                }
            };
            // Capturar eventos do mouse para prevenir passagem para janelas abaixo
            MouseDown += (s, e) => { /* consumir */ };
            MouseMove += (s, e) => { /* consumir */ };
            MouseClick += (s, e) => { /* consumir */ };
            KeyPreview = true;
            AllowDrop = false;

            // Desabilitar fechar por Alt+F4
            FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                    e.Cancel = true;
            };

            Console.WriteLine("[LOCK-FORM] LockForm criado com sucesso");
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            if (_parent._shouldClose)
                return;

            double oldOpacity = Opacity;
            bool enabled = Enabled;

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
            }

            if (Math.Abs(oldOpacity - Opacity) > 0.01 || enabled != Enabled)
            {
                Console.WriteLine($"[LOCK-TIMER] IsLocked={_parent.IsLocked}, ShowBehind={_parent.ShowBehind}, Opacity: {oldOpacity:F2} → {Opacity:F2}, Enabled={Enabled}");
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Console.WriteLine($"[LOCK-PAINT] OnPaint - IsLocked={_parent.IsLocked}, Opacity={Opacity:F2}");

            // Se não está travado e opacity é baixa, limpar com preto
            if (!_parent.IsLocked && Opacity < 0.1)
            {
                g.Clear(Color.Black);
                Console.WriteLine("[LOCK-PAINT] Não renderizando (não travado)");
                return;
            }

            if (_parent.IsLocked)
            {
                Console.WriteLine("[LOCK-PAINT] Renderizando TRAVA VISÍVEL");

                // Fundo PRETO SÓLIDO
                g.Clear(Color.Black);

                // Texto "TRAVA" ENORME em BRANCO
                using (var font = new Font("Arial", 150, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.White))
                {
                    var text = "TRAVA";
                    var size = g.MeasureString(text, font);
                    int textX = (Width - (int)size.Width) / 2;
                    int textY = (Height - (int)size.Height) / 2;

                    // Desenhar texto branco grande
                    g.DrawString(text, font, brush, textX, textY);
                    Console.WriteLine($"[LOCK-PAINT] Texto TRAVA desenhado em X={textX}, Y={textY}, Size={size}");
                }

                // Informação embaixo
                using (var font = new Font("Arial", 16, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.Red))
                {
                    var text = "CLIENTE TRAVADO - NÃO TENTE INTERAGIR";
                    var textSize = g.MeasureString(text, font);
                    int x = (Width - (int)textSize.Width) / 2;
                    g.DrawString(text, font, brush, x, Height - 100);
                }
            }
            else
            {
                // Quando não travado, limpar
                g.Clear(Color.Black);
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
