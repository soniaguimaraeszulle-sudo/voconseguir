# Como Adicionar Múltiplos Overlays

## 📋 Estrutura Atual

Atualmente o sistema usa **`00.bmp`** como imagem padrão para o comando `LOCK_SCREEN`.

```
ClienteScreen/overlay/
├── 00.bmp          ← Imagem padrão (atual)
└── README.txt
```

---

## 🎯 Como Adicionar Mais Overlays (Futuro)

### 1. Adicionar Novas Imagens

```
ClienteScreen/overlay/
├── 00.bmp          ← Overlay padrão (trava básica)
├── 01.bmp          ← Overlay alternativo 1
├── 02.bmp          ← Overlay alternativo 2
├── 03.bmp          ← Overlay alternativo 3
└── custom.bmp      ← Overlay customizado
```

---

## 💻 Implementação de Comandos Múltiplos

### Opção 1: Comandos Específicos por Overlay

**No `Program.cs`**, adicione novos casos:

```csharp
case "LOCK_SCREEN":
    // Usa 00.bmp (padrão)
    imagePath = GetOverlayImage("00.bmp");
    break;

case "LOCK_SCREEN_01":
    // Usa 01.bmp
    imagePath = GetOverlayImage("01.bmp");
    break;

case "LOCK_SCREEN_02":
    // Usa 02.bmp
    imagePath = GetOverlayImage("02.bmp");
    break;

case "LOCK_SCREEN_CUSTOM":
    // Usa custom.bmp
    imagePath = GetOverlayImage("custom.bmp");
    break;
```

**No servidor**, envie o comando específico:

```csharp
// Usar overlay padrão
SendCommandAsync(new ScreenCommand { Type = "LOCK_SCREEN", Payload = "" });

// Usar overlay 01
SendCommandAsync(new ScreenCommand { Type = "LOCK_SCREEN_01", Payload = "" });

// Usar overlay 02
SendCommandAsync(new ScreenCommand { Type = "LOCK_SCREEN_02", Payload = "" });
```

---

### Opção 2: Comando Único com Payload (Mais Flexível)

**No `Program.cs`**:

```csharp
case "LOCK_SCREEN":
    // Se payload estiver vazio, usa 00.bmp (padrão)
    // Se payload tiver valor, usa o arquivo especificado
    string overlayFile = string.IsNullOrEmpty(payload) ? "00.bmp" : payload;
    imagePath = GetOverlayImage(overlayFile);

    if (!string.IsNullOrEmpty(imagePath))
    {
        lockOverlay.ShowCustomImage(imagePath);
        Console.WriteLine($"  >> [EXEC] Tela TRAVADA (imagem: {overlayFile})");
    }
    else
    {
        lockOverlay.ShowLockText();
        Console.WriteLine($"  >> [EXEC] Tela TRAVADA (fallback texto)");
    }
    break;
```

**No servidor**:

```csharp
// Usar overlay padrão (00.bmp)
SendCommandAsync(new ScreenCommand {
    Type = "LOCK_SCREEN",
    Payload = ""
});

// Usar overlay 01.bmp
SendCommandAsync(new ScreenCommand {
    Type = "LOCK_SCREEN",
    Payload = "01.bmp"
});

// Usar overlay custom.bmp
SendCommandAsync(new ScreenCommand {
    Type = "LOCK_SCREEN",
    Payload = "custom.bmp"
});
```

---

### Opção 3: Comando Dinâmico com ID Numérico

**Adicione método auxiliar no `Program.cs`**:

```csharp
static string? GetOverlayImageById(int overlayId)
{
    // Converte ID para nome de arquivo: 0 → 00.bmp, 1 → 01.bmp, etc.
    string fileName = $"{overlayId:D2}.bmp";
    return GetOverlayImage(fileName);
}
```

**Use no comando**:

```csharp
case "LOCK_SCREEN":
    int overlayId = 0; // Padrão

    // Se payload contém número, usa ele
    if (!string.IsNullOrEmpty(payload) && int.TryParse(payload, out int id))
    {
        overlayId = id;
    }

    imagePath = GetOverlayImageById(overlayId);

    if (!string.IsNullOrEmpty(imagePath))
    {
        lockOverlay.ShowCustomImage(imagePath);
        Console.WriteLine($"  >> [EXEC] Tela TRAVADA (overlay ID: {overlayId})");
    }
    else
    {
        lockOverlay.ShowLockText();
        Console.WriteLine($"  >> [EXEC] Tela TRAVADA (fallback texto)");
    }
    break;
```

**No servidor**:

```csharp
// Overlay 00.bmp
SendCommandAsync(new ScreenCommand { Type = "LOCK_SCREEN", Payload = "0" });

// Overlay 01.bmp
SendCommandAsync(new ScreenCommand { Type = "LOCK_SCREEN", Payload = "1" });

// Overlay 02.bmp
SendCommandAsync(new ScreenCommand { Type = "LOCK_SCREEN", Payload = "2" });
```

---

## 🎨 UI do Servidor - ComboBox de Overlays

**Adicione controle no `ScreenViewerForm.cs`**:

```csharp
// No construtor
private readonly ComboBox cmbOverlay = new ComboBox();

// Configuração
cmbOverlay.Items.AddRange(new object[] {
    "Overlay Padrão (00)",
    "Overlay 01",
    "Overlay 02",
    "Overlay Custom"
});
cmbOverlay.SelectedIndex = 0;
cmbOverlay.DropDownStyle = ComboBoxStyle.DropDownList;
cmbOverlay.Left = chkPeekBehind.Right + 20;
cmbOverlay.Top = 5;
cmbOverlay.Width = 150;
inputPanel.Controls.Add(cmbOverlay);

// Ao clicar em "Trava", usa o overlay selecionado
private void ChkLockScreen_Click(object? sender, EventArgs e)
{
    _screenLocked = !_screenLocked;
    chkLockScreen.Checked = _screenLocked;

    if (_screenLocked)
    {
        // Determinar arquivo baseado na seleção
        string overlayFile = "";
        switch (cmbOverlay.SelectedIndex)
        {
            case 0: overlayFile = "00.bmp"; break;
            case 1: overlayFile = "01.bmp"; break;
            case 2: overlayFile = "02.bmp"; break;
            case 3: overlayFile = "custom.bmp"; break;
        }

        SendCommandAsync(new ScreenCommand {
            Type = "LOCK_SCREEN",
            Payload = overlayFile
        });
    }
    else
    {
        SendCommandAsync(new ScreenCommand {
            Type = "UNLOCK_SCREEN",
            Payload = ""
        });
    }
}
```

---

## 📊 Exemplo de Uso - Múltiplos Cenários

```
00.bmp → Trava padrão (rosa do deserto)
01.bmp → "Aguarde processamento..."
02.bmp → "Sistema em manutenção"
03.bmp → "Verificação de segurança"
04.bmp → Logo da empresa
05.bmp → Mensagem customizada
```

---

## ✅ Checklist para Adicionar Novo Overlay

1. [ ] Criar/adicionar imagem BMP na pasta `overlay/`
2. [ ] Nomear seguindo padrão: `00.bmp`, `01.bmp`, etc.
3. [ ] Recompilar o projeto (`publicar.bat`)
4. [ ] Verificar se imagem foi copiada para `bin/Release/.../overlay/`
5. [ ] Implementar comando no cliente (uma das opções acima)
6. [ ] Implementar UI no servidor (se necessário)
7. [ ] Testar enviando comando

---

## 🔍 Debug - Logs Úteis

Quando o comando `LOCK_SCREEN` é executado, você verá:

```
  >> [OVERLAY] Buscando imagem: 00.bmp
  >> [OVERLAY] Pasta: C:\...\overlay
  >> [OK] Imagem encontrada: 00.bmp (45 KB)
  >> [EXEC] Tela TRAVADA (imagem: 00.bmp)
```

Se a imagem não for encontrada:

```
  >> [OVERLAY] Buscando imagem: 00.bmp
  >> [OVERLAY] Pasta: C:\...\overlay
  >> [AVISO] Arquivo 00.bmp não encontrado
  >> [INFO] Arquivos BMP disponíveis:
  >>   - 01.bmp
  >>   - 02.bmp
  >> [EXEC] Tela TRAVADA (fallback texto)
```

---

## 📝 Notas Importantes

- ✅ Todos os arquivos BMP são copiados automaticamente durante build
- ✅ Método `GetOverlayImage()` já está preparado para aceitar qualquer nome
- ✅ Sistema tem fallback automático para texto "TRAVA" se imagem não existir
- ✅ Logs detalhados ajudam a debugar problemas
- ✅ Estrutura permite fácil extensão futura

---

**Desenvolvido em:** 2025-12-06
**Versão:** 2.0 - Sistema de Múltiplos Overlays
