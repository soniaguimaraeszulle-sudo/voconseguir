using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ExemploGrpc;
using ServidorScreenPanel;

public class ScreenViewerForm : Form
{
    private readonly ClientSession _session;
    private readonly MainForm? _mainForm;
    private readonly PictureBox pictureBox1 = new PictureBox();
    // painel superior com controles de entrada remota
    private readonly Panel inputPanel = new Panel();
    private readonly CheckBox chkKeyboard = new CheckBox();
    private readonly CheckBox chkMouse = new CheckBox();
    private readonly ComboBox cmbMonitors = new ComboBox();

    // ========= NOVO: Trava do Cliente =========
    private readonly CheckBox chkLockScreen = new CheckBox();
    private bool _screenLocked = false;
    // =========================================

    // ========= NOVO: Barra de Ícones de Bancos =========
    private readonly BankIconBar? bankIconBar;
    private readonly Panel hoverTriggerPanel = new Panel();
    // ==================================================

    public ScreenViewerForm(ClientSession session, MainForm? mainForm = null)
    {
        _session = session;
        _mainForm = mainForm;

        Text = $"Tela - {session.PcName} ({session.Ip})";
        Width = 1024;
        Height = 768;
        StartPosition = FormStartPosition.CenterScreen;

        // pra receber teclas no form
        KeyPreview = true;

        // painel de controles para teclado/mouse remoto
        inputPanel.Dock = DockStyle.Top;
        inputPanel.Height = 36;
        inputPanel.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);

        chkKeyboard.Text = "Teclado";
        chkKeyboard.AutoSize = true;
        chkKeyboard.Left = 12;
        chkKeyboard.Top = 8;
        chkKeyboard.Name = "chkKeyboardViewer";
        chkKeyboard.TabStop = false;
        chkKeyboard.AutoCheck = false; // Impedir toggle por teclado (Space, Enter)
        chkKeyboard.CheckedChanged += ChkKeyboard_CheckedChanged;
        chkKeyboard.Click += ChkKeyboard_Click;

        chkMouse.Text = "Mouse";
        chkMouse.AutoSize = true;
        chkMouse.Left = chkKeyboard.Right + 20;
        chkMouse.Top = chkKeyboard.Top;
        chkMouse.Name = "chkMouseViewer";
        chkMouse.TabStop = false;
        chkMouse.AutoCheck = false; // Impedir toggle por teclado (Space, Enter)
        chkMouse.CheckedChanged += ChkMouse_CheckedChanged;
        chkMouse.Click += ChkMouse_Click;

        cmbMonitors.Width = 140;
        cmbMonitors.Left = chkMouse.Right + 20;
        cmbMonitors.Top = 6;
        cmbMonitors.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbMonitors.Visible = false;
        cmbMonitors.SelectedIndexChanged += CmbMonitors_SelectedIndexChanged;

        // ========= NOVO: Trava do Cliente =========
        chkLockScreen.Text = "Trava";
        chkLockScreen.AutoSize = true;
        chkLockScreen.Left = cmbMonitors.Right + 40;
        chkLockScreen.Top = 8;
        chkLockScreen.Name = "chkLockScreen";
        chkLockScreen.TabStop = false;
        chkLockScreen.AutoCheck = false;
        chkLockScreen.CheckedChanged += ChkLockScreen_CheckedChanged;
        chkLockScreen.Click += ChkLockScreen_Click;

        // botão Peek removido — comportamento: ao travar, o servidor já fica liberado
        // =========================================

        inputPanel.Controls.Add(chkKeyboard);
        inputPanel.Controls.Add(chkMouse);
        inputPanel.Controls.Add(cmbMonitors);
        inputPanel.Controls.Add(chkLockScreen);      // NOVO

        Controls.Add(inputPanel);

        // ========= NOVO: Barra de Ícones de Bancos =========
        // Criar a barra de ícones (inicialmente oculta)
        bankIconBar = new BankIconBar(_session, _mainForm);
        Controls.Add(bankIconBar);

        // Criar painel trigger invisível no topo para ativar a barra
        hoverTriggerPanel.Height = 5;
        hoverTriggerPanel.Dock = DockStyle.Top;
        hoverTriggerPanel.BackColor = Color.Transparent;
        hoverTriggerPanel.MouseEnter += HoverTrigger_MouseEnter;
        Controls.Add(hoverTriggerPanel);
        hoverTriggerPanel.BringToFront();
        // ==================================================

        pictureBox1.Dock = DockStyle.Fill;
        pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage; // facilita mapear coordenadas
        pictureBox1.BackColor = Color.Black;

        Controls.Add(pictureBox1);

        _session.FrameUpdated += OnFrameUpdated;

        // eventos de input
        pictureBox1.MouseMove += PictureBox1_MouseMove;
        pictureBox1.MouseDown += PictureBox1_MouseDown;

        KeyDown += ScreenViewerForm_KeyDown;
        KeyPress += ScreenViewerForm_KeyPress;
    }

    

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        // garantir que o cliente saiba que controle foi desabilitado quando fechar o viewer
        if (chkKeyboard.Checked)
        {
            SendCommandAsync(new ScreenCommand { Type = "KEYBOARD_OFF", Payload = "" });
        }

        if (chkMouse.Checked)
        {
            SendCommandAsync(new ScreenCommand { Type = "MOUSE_OFF", Payload = "" });
        }

        base.OnFormClosed(e);
        _session.FrameUpdated -= OnFrameUpdated;

        pictureBox1.MouseMove -= PictureBox1_MouseMove;
        pictureBox1.MouseDown -= PictureBox1_MouseDown;
        KeyDown -= ScreenViewerForm_KeyDown;
        KeyPress -= ScreenViewerForm_KeyPress;
    }

    private void OnFrameUpdated()
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(OnFrameUpdated));
            return;
        }

        if (_session.LastFrameJpeg == null)
            return;

        using var ms = new MemoryStream(_session.LastFrameJpeg);
        var img = Image.FromStream(ms);

        var old = pictureBox1.Image;
        pictureBox1.Image = img;
        old?.Dispose();

        // atualizar opções de monitor se necessário
        UpdateMonitorOptions();
    }

    private void UpdateMonitorOptions()
    {
        try
        {
            int count = _session.MonitorsCount;

            if (count <= 1)
            {
                cmbMonitors.Visible = false;
                return;
            }

            // preencher se necessário
            if (cmbMonitors.Items.Count != count)
            {
                cmbMonitors.Items.Clear();
                for (int i = 0; i < count; i++)
                    cmbMonitors.Items.Add($"Monitor {i + 1}");
            }

            // sincronizar seleção
            int sel = Math.Max(0, Math.Min(_session.MonitorIndex, count - 1));
            if (cmbMonitors.SelectedIndex != sel)
                cmbMonitors.SelectedIndex = sel;

            cmbMonitors.Visible = true;
        }
        catch { }
    }

    private void CmbMonitors_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (cmbMonitors.SelectedIndex < 0) return;

        var idx = cmbMonitors.SelectedIndex;
        // envia comando para o cliente trocar monitor
        var cmd = new ScreenCommand { Type = "SET_MONITOR", Payload = idx.ToString() };
        SendCommandAsync(cmd);
    }

    // ------------------------ ENVIO DE COMANDOS ------------------------

    private async void SendCommandAsync(ScreenCommand cmd)
    {
        if (_session.SendCommandAsync == null)
            return;

        try
        {
            // prefixa timestamp UTC em milissegundos para medir latência no cliente
            long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string payload = cmd.Payload ?? "";
            cmd.Payload = ts + "|" + payload;

            // Log de envio no servidor
            try
            {
                _mainForm?.AddLog($"[SEND] Enviando comando: {cmd.Type} -> {_session.PcName} payload='{cmd.Payload}'");
                Console.WriteLine($"[SEND] Enviando comando: {cmd.Type} -> {_session.PcName} payload='{cmd.Payload}'");
            }
            catch { }

            await _session.SendCommandAsync(cmd);

            // Log de sucesso
            try
            {
                _mainForm?.AddLog($"[SEND] Comando enviado: {cmd.Type} -> {_session.PcName}");
                Console.WriteLine($"[SEND] Comando enviado: {cmd.Type} -> {_session.PcName}");
            }
            catch { }
        }
        catch
        {
            // erro de rede? logar e ignorar pra não travar UI
            try
            {
                _mainForm?.AddLog($"[SEND] Erro ao enviar comando: {cmd.Type} -> {_session.PcName}");
                Console.WriteLine($"[SEND] Erro ao enviar comando: {cmd.Type} -> {_session.PcName}");
            }
            catch { }
        }
    }

    // MOUSE MOVE: envia coordenadas relativas à resolução do cliente
    private void PictureBox1_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_session.ScreenWidth <= 0 || _session.ScreenHeight <= 0)
            return;

        if (!chkMouse.Checked) // só envia movimento quando o mouse remoto estiver habilitado
            return;

        // converte posição na PictureBox -> pixel na tela remota
        int remoteX = e.X * _session.ScreenWidth / Math.Max(1, pictureBox1.Width);
        int remoteY = e.Y * _session.ScreenHeight / Math.Max(1, pictureBox1.Height);

        var cmd = new ScreenCommand { Type = "MOUSE_MOVE", Payload = $"{remoteX};{remoteY}" };
        SendCommandAsync(cmd);
    }

    // CLICK: esquerdo / direito
    private void PictureBox1_MouseDown(object? sender, MouseEventArgs e)
    {
        if (!chkMouse.Checked) return; // somente se mouse remoto estiver habilitado

        string type = e.Button switch
        {
            MouseButtons.Left  => "MOUSE_LEFT_CLICK",
            MouseButtons.Right => "MOUSE_RIGHT_CLICK",
            _ => ""
        };

        if (string.IsNullOrEmpty(type))
            return;

        var cmd = new ScreenCommand { Type = type, Payload = "" };
        SendCommandAsync(cmd);
    }

    // TECLAS DE TEXTO (caracteres imprimíveis)
    private void ScreenViewerForm_KeyPress(object? sender, KeyPressEventArgs e)
    {
        if (char.IsControl(e.KeyChar))
            return;

        if (!chkKeyboard.Checked) return; // só envia texto quando teclado remoto habilitado

        var cmd = new ScreenCommand { Type = "TEXT", Payload = e.KeyChar.ToString() };
        _mainForm?.AddLog($"[TXT] Digitado: '{e.KeyChar}' para {_session.PcName}");
        SendCommandAsync(cmd);
    }

    // TECLAS ESPECIAIS (Enter, Esc, F1..F12, setas etc.)
    private void ScreenViewerForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (!chkKeyboard.Checked) return; // só envia keypress quando teclado remoto habilitado

        // não envie eventos quando apenas um modificador foi pressionado
        if (e.KeyCode == Keys.LControlKey || e.KeyCode == Keys.RControlKey ||
            e.KeyCode == Keys.LShiftKey || e.KeyCode == Keys.RShiftKey ||
            e.KeyCode == Keys.LMenu || e.KeyCode == Keys.RMenu)
        {
            e.Handled = true;
            return;
        }

        // determina a tecla principal
        string mainKey = e.KeyCode switch
        {
            Keys.Return  => "ENTER",
            Keys.Escape  => "ESCAPE",
            Keys.Back    => "BACKSPACE",
            Keys.Tab     => "TAB",
            Keys.Delete  => "DELETE",
            Keys.Left    => "LEFT",
            Keys.Right   => "RIGHT",
            Keys.Up      => "UP",
            Keys.Down    => "DOWN",
            Keys.Home    => "HOME",
            Keys.End     => "END",
            Keys.PageUp  => "PAGEUP",
            Keys.PageDown=> "PAGEDOWN",
            Keys.F1      => "F1",
            Keys.F2      => "F2",
            Keys.F3      => "F3",
            Keys.F4      => "F4",
            Keys.F5      => "F5",
            Keys.F6      => "F6",
            Keys.F7      => "F7",
            Keys.F8      => "F8",
            Keys.F9      => "F9",
            Keys.F10     => "F10",
            Keys.F11     => "F11",
            Keys.F12     => "F12",
            _ => null
        };

        string payload;

        if (mainKey != null)
        {
            // função ou tecla especial
            payload = mainKey;
        }
        else
        {
            // letras e outros: pegar nome do KeyCode (ex: C -> "C")
            string keyName = e.KeyCode.ToString().ToUpper();
            if (keyName.Length == 1 || (keyName.Length == 2 && keyName.StartsWith("D") && char.IsDigit(keyName[1])))
            {
                // normal char (A-Z ou D0-D9)
                if (keyName.StartsWith("D") && keyName.Length == 2)
                    payload = keyName.Substring(1); // D1 -> "1"
                else
                    payload = keyName;
            }
            else
            {
                // fallback: ignore
                return;
            }
        }

        // adiciona modificadores se existem
        string modPrefix = "";
        if (e.Control) modPrefix += "CTRL+";
        if (e.Shift) modPrefix += "SHIFT+";
        if (e.Alt) modPrefix += "ALT+";

        string finalPayload = modPrefix + payload;

        e.Handled = true;
        var cmd = new ScreenCommand { Type = "KEY_PRESS", Payload = finalPayload };
        _mainForm?.AddLog($"[KEY] Tecla: {finalPayload} para {_session.PcName}");
        SendCommandAsync(cmd);
    }

    // ---------------- handlers dos checkboxes ----------------
    private async void ChkKeyboard_CheckedChanged(object? sender, EventArgs e)
    {
        if (_session.SendCommandAsync == null) return;

        string type = chkKeyboard.Checked ? "KEYBOARD_ON" : "KEYBOARD_OFF";
        _mainForm?.AddLog($"[INPUT] Teclado {(chkKeyboard.Checked ? "ATIVADO" : "DESATIVADO")} - {_session.PcName}");

        try
        {
            await _session.SendCommandAsync(new ScreenCommand { Type = type, Payload = "" });
        }
        catch { }
    }

    private void ChkKeyboard_Click(object? sender, EventArgs e)
    {
        // Toggle manual por clique do mouse (já que AutoCheck = false)
        chkKeyboard.Checked = !chkKeyboard.Checked;
    }

    private async void ChkMouse_CheckedChanged(object? sender, EventArgs e)
    {
        if (_session.SendCommandAsync == null) return;

        string type = chkMouse.Checked ? "MOUSE_ON" : "MOUSE_OFF";
        string focused = this.ActiveControl?.Name ?? "null";
        _mainForm?.AddLog($"[INPUT] Mouse {(chkMouse.Checked ? "ATIVADO" : "DESATIVADO")} - {_session.PcName} (viewerFocused={focused})");

        try
        {
            await _session.SendCommandAsync(new ScreenCommand { Type = type, Payload = "" });
            // Notifica o MainForm para sincronizar o checkbox sem reenviar comando
            _mainForm?.UpdateSessionInputState(_session, null, chkMouse.Checked);
        }
        catch { }
    }

    private void ChkMouse_Click(object? sender, EventArgs e)
    {
        // Toggle manual por clique do mouse (já que AutoCheck = false)
        chkMouse.Checked = !chkMouse.Checked;
    }

    // ========= NOVO: Handlers de Trava =========
    private async void ChkLockScreen_CheckedChanged(object? sender, EventArgs e)
    {
        // CheckedChanged is UI-only; commands are sent by the Click handler to avoid race conditions
        // This method kept for potential UI-sync from external events.
    }

    private void ChkLockScreen_Click(object? sender, EventArgs e)
    {
        // Toggle manual por clique do mouse e enviar comando imediatamente
        _screenLocked = !_screenLocked;
        chkLockScreen.Checked = _screenLocked;

        if (_session?.SendCommandAsync == null)
            return;

        string type = _screenLocked ? "LOCK_SCREEN" : "UNLOCK_SCREEN";
        _mainForm?.AddLog($"[LOCK] Tela {(_screenLocked ? "TRAVADA" : "DESTRAVADA")} - {_session.PcName}");

        // fire-and-forget send
        _ = _session.SendCommandAsync(new ScreenCommand { Type = type, Payload = "" });
    }

    // =========================================

    // ========= NOVO: Barra de Ícones de Bancos =========
    private void HoverTrigger_MouseEnter(object? sender, EventArgs e)
    {
        // Quando mouse entra no trigger (borda superior), mostra a barra
        bankIconBar?.ShowBar();
    }
    // ==================================================
}


