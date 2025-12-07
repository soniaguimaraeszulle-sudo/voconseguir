using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Net.Client;
using System.Net.Http;
using Grpc.Core;
using ExemploGrpc;
// substituído uso de WindowsInput por LocalInput (mais compatível)

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            await RunClient(args);
        }
        catch (Exception ex)
        {
            // Registra o erro no mesmo diretório do executável
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            string logPath = System.IO.Path.Combine(exePath, "ClienteScreen_error.log");
            string errorMsg = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - ERRO: {ex.Message}\n{ex.StackTrace}";
            
            try
            {
                System.IO.File.AppendAllText(logPath, errorMsg + "\n");
                Console.WriteLine($"\n[ERRO REGISTRADO EM: {logPath}]");
            }
            catch { }
            
            Console.WriteLine($"\nErro fatal: {ex.Message}");
            Console.WriteLine("Pressione qualquer tecla para sair...");
            Console.ReadKey();
        }
    }

    static async Task RunClient(string[] args)
    {
        Console.WriteLine("Cliente de transmissão de tela iniciando...");

        // permite sobrescrever o endereço do servidor via argumento de linha de comando
        // uso: ClienteScreen.exe http://SERVIDOR:5000  ou  ClienteScreen.exe 192.168.1.100:5000
        string serverAddress;
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            serverAddress = args[0];
            // se o usuário passou apenas "192.168.1.100:5000", adiciona o esquema http://
            if (!serverAddress.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !serverAddress.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                serverAddress = "http://" + serverAddress;
            }
        }
        else
        {
            // padrão: conexão ao servidor em VPS via DNS
            serverAddress = "http://api.pinnalcesteel.store:5000";
        }

        Console.WriteLine($"Conectando ao servidor: {serverAddress}");

        // Tenta conectar ao servidor
        GrpcChannel channel = null;
        try
        {
            channel = GrpcChannel.ForAddress(serverAddress);
            var client = new ScreenService.ScreenServiceClient(channel);
            
            // Apenas cria o canal, não testa conexão ainda
            Console.WriteLine("Canal gRPC criado com sucesso.");
            
            await RunScreenStreaming(serverAddress, args);
        }
        catch (Exception ex) when (ex.InnerException is System.Net.Sockets.SocketException || 
                                   ex.Message.Contains("Unavailable") || 
                                   ex.Message.Contains("recusou"))
        {
            Console.WriteLine($"\n[AVISO] Não conseguiu conectar em {serverAddress}");
            Console.WriteLine($"Erro: {ex.Message}\n");
            
            // Tenta fallback para localhost
            string fallbackAddress = "http://localhost:5000";
            Console.WriteLine($"Tentando fallback em {fallbackAddress}...");
            
            try
            {
                channel = GrpcChannel.ForAddress(fallbackAddress);
                var client = new ScreenService.ScreenServiceClient(channel);
                
                Console.WriteLine("Conexão com fallback bem-sucedida!");
                await RunScreenStreaming(fallbackAddress, args);
            }
            catch (Exception fallbackEx)
            {
                Console.WriteLine($"\n[ERRO] Fallback também falhou: {fallbackEx.Message}");
                throw;
            }
        }
        finally
        {
            channel?.Dispose();
        }
    }

    static async Task RunScreenStreaming(string serverAddress, string[] args)
    {
        using var channel = GrpcChannel.ForAddress(serverAddress);
        var client = new ScreenService.ScreenServiceClient(channel);

        using var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (s, e) =>
        {
            Console.WriteLine("Cancelando...");
            cts.Cancel();
            e.Cancel = true;
        };

        var info = ClientInfo.Collect();

        Console.WriteLine($"PC: {info.PcName}");
        Console.WriteLine($"IP: {info.Ip}");
        Console.WriteLine($"MAC: {info.Mac}");
        Console.WriteLine($"Antivírus: {info.Antivirus}");
        Console.WriteLine($"País: {info.Country}");

        using var call = client.StreamScreen(cancellationToken: cts.Token);

        int currentMonitor = 0;

        var input = new InputInjector(); // Usando a biblioteca Hook
        bool keyboardEnabled = false;
        bool mouseEnabled = false;
        
        // ========== NOVO: Overlay de trava ==========
        ScreenLockOverlay lockOverlay = null;
        bool screenLocked = false;
        try
        {
            lockOverlay = new ScreenLockOverlay();
            Console.WriteLine("[LOCK] Overlay de trava inicializado");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Não foi possível criar overlay de trava: {ex.Message}");
        }
        // ==========================================

        // resolução atual da tela capturada
        int lastWidth = 1920;
        int lastHeight = 1080;

        // Variáveis para ping
        long lastPingTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        const int pingInterval = 2000; // 2 segundos

        // ---------------- ENVIO DE FRAMES ----------------
        var sendTask = Task.Run(async () =>
        {
            const int targetFps = 10; // Aumentado de 5 para 10 FPS
            var frameInterval = TimeSpan.FromMilliseconds(1000.0 / targetFps);

            Console.WriteLine($"Enviando tela ({targetFps} FPS). Ctrl+C para parar.");

            byte[]? lastJpeg = null; // Para congelar frame quando travado

            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    byte[] jpeg;
                    int width, height;

                    // Quando travado: enviar frame congelado (último frame antes da trava)
                    // Cliente vê: overlay vermelho
                    // Servidor vê: desktop congelado (sem overlay)
                    if (screenLocked && lastJpeg != null && lastJpeg.Length > 0)
                    {
                        // Enviar frame congelado
                        jpeg = lastJpeg;
                        width = lastWidth;
                        height = lastHeight;
                    }
                    else
                    {
                        // Capturar novo frame
                        using var capturer = new GdiScreenCapturer(currentMonitor);
                        jpeg = capturer.CaptureFrameJpeg(out width, out height);

                        if (jpeg.Length == 0)
                        {
                            await Task.Delay(frameInterval, cts.Token);
                            continue;
                        }

                        // Guardar frame para usar quando travar
                        lastJpeg = jpeg;
                        lastWidth = width;
                        lastHeight = height;
                    }

                    lastWidth = width;
                    lastHeight = height;

                    var frame = new ScreenFrame
                    {
                        PcName = info.PcName,
                        ImageData = Google.Protobuf.ByteString.CopyFrom(jpeg),
                        Width = width,
                        Height = height,
                        MonitorIndex = currentMonitor,
                        MonitorsCount = System.Windows.Forms.Screen.AllScreens.Length,
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        Ip = info.Ip,
                        Mac = info.Mac,
                        Antivirus = info.Antivirus,
                        Country = info.Country
                    };

                    await call.RequestStream.WriteAsync(frame);

                    // Enviar ping a cada 2 segundos
                    long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    if (currentTime - lastPingTime >= pingInterval)
                    {
                        long latency = currentTime - frame.Timestamp;
                        Console.WriteLine($"[PING] {latency}ms");
                        lastPingTime = currentTime;
                    }

                    await Task.Delay(frameInterval, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Erro ao capturar/enviar frame: {ex.Message}");
                    await Task.Delay(frameInterval, cts.Token);
                }
            }

            await call.RequestStream.CompleteAsync();
        }, cts.Token);

        // ---------------- RECEBIMENTO DE COMANDOS ----------------
        var receiveTask = Task.Run(async () =>
        {
            try
            {
                while (await call.ResponseStream.MoveNext(cts.Token))
                {
                    var cmd = call.ResponseStream.Current;
                    // Tenta desserializar timestamp prefixado (enviado pelo servidor)
                    long sentTs = 0;
                    string payload = cmd.Payload ?? "";
                    if (payload.Contains('|'))
                    {
                        var parts = payload.Split(new[] { '|' }, 2);
                        if (long.TryParse(parts[0], out var pTs))
                        {
                            sentTs = pTs;
                            payload = parts.Length > 1 ? parts[1] : "";
                        }
                    }

                    var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    if (sentTs > 0)
                    {
                        Console.WriteLine($"Comando do servidor: {cmd.Type} | {payload} (sent {nowMs - sentTs}ms ago)");
                    }
                    else
                    {
                        Console.WriteLine($"Comando do servidor: {cmd.Type} | {payload}");
                    }

                    switch (cmd.Type)
                    {
                        case "KEYBOARD_ON":
                            // Ignorar se tela estiver travada
                            if (screenLocked)
                            {
                                Console.WriteLine(">> [BLOQUEADO] Teclado não pode ser ativado (tela travada)");
                                break;
                            }
                            keyboardEnabled = true;
                            Console.WriteLine(">> Teclado remoto ATIVADO");
                            break;

                        case "KEYBOARD_OFF":
                            keyboardEnabled = false;
                            Console.WriteLine(">> Teclado remoto DESATIVADO");
                            break;

                        case "MOUSE_ON":
                            // Ignorar se tela estiver travada
                            if (screenLocked)
                            {
                                Console.WriteLine(">> [BLOQUEADO] Mouse não pode ser ativado (tela travada)");
                                break;
                            }
                            mouseEnabled = true;
                            Console.WriteLine(">> Mouse remoto ATIVADO");
                            break;

                        case "MOUSE_OFF":
                            mouseEnabled = false;
                            Console.WriteLine(">> Mouse remoto DESATIVADO");
                            break;

                        case "TEXT":
                            // Bloquear se tela travada
                            if (screenLocked || !keyboardEnabled) break;
                            if (!string.IsNullOrEmpty(payload))
                            {
                                InputInjector.TextEntry(payload);
                                Console.WriteLine($"  >> [EXEC] Digitando: '{payload}'");
                            }
                            break;

                        case "KEY_PRESS":
                            // Bloquear se tela travada
                            if (screenLocked || !keyboardEnabled) break;
                            if (!string.IsNullOrEmpty(payload))
                            {
                                InputInjector.KeyPress(payload);
                                Console.WriteLine($"  >> [EXEC] Tecla: {payload}");
                            }
                            break;

                            case "SET_MONITOR":
                                if (!string.IsNullOrEmpty(cmd.Payload) && int.TryParse(cmd.Payload, out var mi))
                                {
                                    currentMonitor = Math.Max(0, mi);
                                    Console.WriteLine($"Monitor remoto selecionado: {currentMonitor}");
                                }
                                break;

                        case "MOUSE_LEFT_CLICK":
                            // Bloquear se tela travada
                            if (screenLocked || !mouseEnabled) break;
                            InputInjector.LeftClick();
                            Console.WriteLine($"  >> [EXEC] Clique esquerdo");
                            break;

                        case "MOUSE_RIGHT_CLICK":
                            // Bloquear se tela travada
                            if (screenLocked || !mouseEnabled) break;
                            InputInjector.RightClick();
                            Console.WriteLine($"  >> [EXEC] Clique direito");
                            break;

                        case "MOUSE_MOVE":
                            // Bloquear se tela travada
                            if (screenLocked || !mouseEnabled) break;
                            if (!string.IsNullOrEmpty(payload))
                            {
                                var parts = payload.Split(';');
                                if (parts.Length == 2 &&
                                    int.TryParse(parts[0], out int x) &&
                                    int.TryParse(parts[1], out int y))
                                {
                                    // posiciona o cursor na coordenada enviada pelo servidor
                                    InputInjector.MoveMouseTo(x, y);
                                    Console.WriteLine($"  >> [EXEC] Mouse: {x},{y}");
                                }
                            }
                            break;

                        case "LOCK_SCREEN":
                            // Ativa a trava do cliente
                            if (lockOverlay != null)
                            {
                                screenLocked = true;
                                lockOverlay.SetLocked(true);

                                // DESABILITAR controle remoto quando travar
                                // (servidor vê tela congelada, não pode controlar às cegas)
                                if (keyboardEnabled)
                                {
                                    keyboardEnabled = false;
                                    Console.WriteLine("  >> [LOCK] Teclado remoto DESABILITADO (trava ativa)");
                                }
                                if (mouseEnabled)
                                {
                                    mouseEnabled = false;
                                    Console.WriteLine("  >> [LOCK] Mouse remoto DESABILITADO (trava ativa)");
                                }

                                Console.WriteLine("  >> [EXEC] Tela TRAVADA (controle remoto desabilitado)");
                            }
                            break;

                        case "UNLOCK_SCREEN":
                            // Desativa a trava do cliente
                            if (lockOverlay != null)
                            {
                                screenLocked = false;
                                lockOverlay.SetLocked(false);
                                Console.WriteLine("  >> [EXEC] Tela DESTRAVADA");
                            }
                            break;

                        // PEEK_BEHIND removed: Trava agora já libera o servidor automaticamente.

                        case "STOP":
                            Console.WriteLine("Servidor solicitou parada do streaming.");
                            cts.Cancel();
                            return;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }, cts.Token);

        await Task.WhenAll(sendTask, receiveTask);

        // Limpar overlay
        try
        {
            lockOverlay?.SetLocked(false);
            lockOverlay?.Close();
        }
        catch { }

        Console.WriteLine("Cliente finalizado.");
    }
}
