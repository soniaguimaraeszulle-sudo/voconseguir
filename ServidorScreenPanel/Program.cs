using System;
using System.Windows.Forms;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal static class Program
{
    // Porta usada pelo servidor gRPC (mostrada no painel)
    public const int ServerPort = 5000;

    [STAThread]
    static void Main()
    {
        try
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            string errorLog = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - ERRO: {ex.Message}\n{ex.StackTrace}";
            string logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ServidorScreenPanel_error.log");
            
            try
            {
                System.IO.File.AppendAllText(logPath, errorLog + "\n");
            }
            catch { }
            
            MessageBox.Show($"Erro ao iniciar servidor:\n\n{ex.Message}\n\nArquivo de log: {logPath}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // Mantemos a lógica do host aqui, mas quem decide ligar/desligar é o MainForm
    public static IHostBuilder CreateHostBuilder() =>
        Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel(options =>
                {
                    // Escuta em qualquer interface na porta definida usando HTTP/2
                    options.ListenAnyIP(ServerPort, o =>
                    {
                        o.Protocols = HttpProtocols.Http2;
                    });
                });

                webBuilder.ConfigureServices(services =>
                {
                    services.AddGrpc();
                });

                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGrpcService<ScreenServiceImpl>();
                    });
                });
            });
}
