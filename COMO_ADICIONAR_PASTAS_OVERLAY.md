# Como Adicionar Novas Pastas de Overlay

## 📋 Visão Geral

Este guia explica como adicionar **novas pastas** com overlays customizados ao projeto ClienteScreen. Cada pasta pode conter seus próprios arquivos BMP para diferentes cenários.

---

## 🎯 Estrutura Atual

```
ClienteScreen/
├── overlay/
│   ├── 00.bmp          ← Imagem padrão para LOCK_SCREEN
│   ├── README.txt
│   └── COMO_ADICIONAR_OVERLAYS.md
└── ClienteScreen.csproj
```

---

## 🚀 Como Adicionar Nova Pasta Overlay

### **Passo 1: Criar a Pasta no Projeto**

Crie uma nova pasta dentro de `ClienteScreen/` com o nome desejado:

```
ClienteScreen/
├── overlay/           ← Existente
├── planta/            ← NOVA PASTA
│   └── 00.bmp
├── mensagem/          ← NOVA PASTA
│   └── 00.bmp
└── aviso/             ← NOVA PASTA
    └── 00.bmp
```

**Exemplo de nomes de pastas:**
- `planta` - Para overlay de rosa do deserto
- `mensagem` - Para mensagens do sistema
- `aviso` - Para avisos importantes
- `manutencao` - Para tela de manutenção
- `processando` - Para indicador de processamento

---

### **Passo 2: Adicionar Imagem BMP**

Adicione o arquivo `00.bmp` dentro da nova pasta:

```
planta/
└── 00.bmp  ← Imagem da rosa do deserto
```

**Especificações do arquivo:**
- Nome: `00.bmp` (padrão recomendado)
- Formato: BMP 24-bit
- Resolução recomendada: 1920x1080 ou similar
- A imagem será redimensionada automaticamente mantendo proporções

---

### **Passo 3: Atualizar ClienteScreen.csproj**

Abra `ClienteScreen/ClienteScreen.csproj` e adicione a nova pasta na seção de cópia:

```xml
<!-- Copiar pastas de overlay para o diretório de saída -->
<ItemGroup>
  <None Include="overlay\**\*" CopyToOutputDirectory="PreserveNewest" />

  <!-- ADICIONE AQUI AS NOVAS PASTAS: -->
  <None Include="planta\**\*" CopyToOutputDirectory="PreserveNewest" />
  <None Include="mensagem\**\*" CopyToOutputDirectory="PreserveNewest" />
  <None Include="aviso\**\*" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

Isto garante que a pasta será copiada automaticamente durante a compilação.

---

### **Passo 4: Implementar Comando no Cliente**

Abra `ClienteScreen/Program.cs` e adicione o comando na seção de comandos:

#### **Opção A: Comando Específico (Recomendado)**

```csharp
case "LOCK_SCREEN_PLANTA":
    if (lockOverlay != null)
    {
        screenLocked = true;
        string imagePath = GetOverlayImage("planta", "00.bmp");
        // Ou simplesmente: string imagePath = GetOverlayImage("planta");

        if (!string.IsNullOrEmpty(imagePath))
        {
            lockOverlay.ShowCustomImage(imagePath);
            Console.WriteLine($"  >> [EXEC] Tela TRAVADA (planta/00.bmp)");
        }
        else
        {
            lockOverlay.ShowLockText();
            Console.WriteLine($"  >> [EXEC] Tela TRAVADA (fallback texto)");
        }
    }
    break;

case "LOCK_SCREEN_MENSAGEM":
    if (lockOverlay != null)
    {
        screenLocked = true;
        string imagePath = GetOverlayImage("mensagem");

        if (!string.IsNullOrEmpty(imagePath))
        {
            lockOverlay.ShowCustomImage(imagePath);
            Console.WriteLine($"  >> [EXEC] Tela TRAVADA (mensagem/00.bmp)");
        }
        else
        {
            lockOverlay.ShowLockText();
            Console.WriteLine($"  >> [EXEC] Fallback texto TRAVA");
        }
    }
    break;
```

#### **Opção B: Comando Dinâmico (Mais Flexível)**

```csharp
case "SHOW_CUSTOM_OVERLAY":
    if (lockOverlay != null && !string.IsNullOrEmpty(payload))
    {
        screenLocked = true;

        // Payload pode ser "planta" ou "planta/00.bmp"
        string[] parts = payload.Split('/');
        string imagePath = null;

        if (parts.Length == 2)
            imagePath = GetOverlayImage(parts[0], parts[1]); // pasta/arquivo
        else
            imagePath = GetOverlayImage(parts[0]); // pasta (usa 00.bmp)

        if (!string.IsNullOrEmpty(imagePath))
        {
            lockOverlay.ShowCustomImage(imagePath);
            Console.WriteLine($"  >> [EXEC] Overlay customizado: {payload}");
        }
        else
        {
            lockOverlay.ShowLockText();
            Console.WriteLine($"  >> [EXEC] Fallback texto");
        }
    }
    break;
```

---

### **Passo 5: Implementar UI no Servidor (Opcional)**

Abra `ServidorScreenPanel/ScreenViewerForm.cs` e adicione controle para selecionar overlay:

```csharp
// No construtor ou InitializeComponent
private readonly ComboBox cmbOverlayType = new ComboBox();

// Configuração
cmbOverlayType.Items.AddRange(new object[] {
    "Trava Padrão",
    "Rosa do Deserto",
    "Mensagem Sistema",
    "Aviso Importante"
});
cmbOverlayType.SelectedIndex = 0;
cmbOverlayType.DropDownStyle = ComboBoxStyle.DropDownList;
cmbOverlayType.Left = chkPeekBehind.Right + 20;
cmbOverlayType.Top = 5;
cmbOverlayType.Width = 150;
inputPanel.Controls.Add(cmbOverlayType);

// Ao clicar em "Trava"
private void ChkLockScreen_Click(object? sender, EventArgs e)
{
    _screenLocked = !_screenLocked;
    chkLockScreen.Checked = _screenLocked;

    if (_screenLocked)
    {
        string commandType = "";

        switch (cmbOverlayType.SelectedIndex)
        {
            case 0: commandType = "LOCK_SCREEN"; break;        // overlay/00.bmp
            case 1: commandType = "LOCK_SCREEN_PLANTA"; break; // planta/00.bmp
            case 2: commandType = "LOCK_SCREEN_MENSAGEM"; break; // mensagem/00.bmp
            case 3: commandType = "LOCK_SCREEN_AVISO"; break;  // aviso/00.bmp
        }

        SendCommandAsync(new ScreenCommand {
            Type = commandType,
            Payload = ""
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

## 📊 Exemplo Completo - Adicionar "Rosa do Deserto"

### **1. Estrutura de Arquivos**

```
ClienteScreen/
├── overlay/
│   └── 00.bmp          ← Trava padrão
└── planta/             ← NOVA PASTA
    └── 00.bmp          ← Imagem rosa do deserto
```

### **2. ClienteScreen.csproj**

```xml
<ItemGroup>
  <None Include="overlay\**\*" CopyToOutputDirectory="PreserveNewest" />
  <None Include="planta\**\*" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

### **3. Program.cs**

```csharp
case "LOCK_SCREEN_PLANTA":
    if (lockOverlay != null)
    {
        screenLocked = true;
        string imagePath = GetOverlayImage("planta");

        if (!string.IsNullOrEmpty(imagePath))
        {
            lockOverlay.ShowCustomImage(imagePath);
            Console.WriteLine($"  >> [EXEC] Tela TRAVADA (rosa do deserto)");
        }
        else
        {
            lockOverlay.ShowLockText();
        }
    }
    break;
```

### **4. Servidor**

```csharp
// Enviar comando
SendCommandAsync(new ScreenCommand {
    Type = "LOCK_SCREEN_PLANTA",
    Payload = ""
});
```

### **5. Logs Esperados**

```
Comando do servidor: LOCK_SCREEN_PLANTA |
  >> [OVERLAY] Buscando: planta/00.bmp
  >> [OK] Encontrada: planta/00.bmp (45 KB)
  >> [EXEC] Tela TRAVADA (rosa do deserto)
[OVERLAY] Modo: CUSTOM_IMAGE - C:\...\planta\00.bmp
```

---

## 🎨 Exemplos de Cenários de Uso

### **Cenário 1: Diferentes Plantas**

```
ClienteScreen/
├── rosa_deserto/00.bmp
├── cacto/00.bmp
├── suculenta/00.bmp
└── orquidea/00.bmp
```

**Comandos:**
- `LOCK_SCREEN_ROSA_DESERTO`
- `LOCK_SCREEN_CACTO`
- `LOCK_SCREEN_SUCULENTA`
- `LOCK_SCREEN_ORQUIDEA`

---

### **Cenário 2: Estados do Sistema**

```
ClienteScreen/
├── processando/00.bmp    → "Processando dados..."
├── manutencao/00.bmp     → "Sistema em manutenção"
├── backup/00.bmp         → "Realizando backup..."
└── atualizando/00.bmp    → "Atualizando sistema..."
```

**Comandos:**
- `SHOW_ESTADO_PROCESSANDO`
- `SHOW_ESTADO_MANUTENCAO`
- `SHOW_ESTADO_BACKUP`
- `SHOW_ESTADO_ATUALIZANDO`

---

### **Cenário 3: Mensagens para Usuários**

```
ClienteScreen/
├── aguarde/00.bmp
├── bloqueado/00.bmp
├── acesso_negado/00.bmp
└── sessao_expirada/00.bmp
```

---

## 🔍 Debug e Troubleshooting

### **Pasta não encontrada**

```
  >> [OVERLAY] Buscando: planta/00.bmp
  >> [AVISO] Pasta 'planta' não encontrada
```

**Solução:**
1. Verifique se a pasta existe em `ClienteScreen/planta/`
2. Verifique se adicionou no `.csproj`
3. Recompile o projeto

---

### **Arquivo não encontrado**

```
  >> [OVERLAY] Buscando: planta/00.bmp
  >> [AVISO] Arquivo '00.bmp' não encontrado em 'planta'
  >> [INFO] Arquivos BMP disponíveis em 'planta':
  >>   - 01.bmp
  >>   - 02.bmp
```

**Solução:**
1. Adicione o arquivo `00.bmp` na pasta
2. Ou modifique o comando para usar arquivo disponível:
   ```csharp
   string imagePath = GetOverlayImage("planta", "01.bmp");
   ```

---

### **Pasta não copiada para bin/**

**Solução:**
1. Verifique `.csproj`:
   ```xml
   <None Include="planta\**\*" CopyToOutputDirectory="PreserveNewest" />
   ```
2. Recompile completamente (Clean + Build)
3. Verifique `bin/Release/.../planta/00.bmp` existe

---

## ✅ Checklist Completo

- [ ] **1.** Criar pasta em `ClienteScreen/[nome]/`
- [ ] **2.** Adicionar `00.bmp` na pasta
- [ ] **3.** Atualizar `ClienteScreen.csproj` (adicionar `<None Include=...`)
- [ ] **4.** Implementar comando em `Program.cs`
- [ ] **5.** (Opcional) Adicionar UI no servidor
- [ ] **6.** Recompilar projeto (`publicar.bat`)
- [ ] **7.** Verificar pasta copiada em `bin/Release/.../[nome]/`
- [ ] **8.** Testar comando do servidor
- [ ] **9.** Verificar logs do cliente
- [ ] **10.** Confirmar imagem aparece no cliente

---

## 📝 Template de Código

### **Program.cs - Adicionar Comando**

```csharp
case "LOCK_SCREEN_[NOME]":
    if (lockOverlay != null)
    {
        screenLocked = true;
        string imagePath = GetOverlayImage("[pasta]");

        if (!string.IsNullOrEmpty(imagePath))
        {
            lockOverlay.ShowCustomImage(imagePath);
            Console.WriteLine($"  >> [EXEC] Tela TRAVADA ([descrição])");
        }
        else
        {
            lockOverlay.ShowLockText();
            Console.WriteLine($"  >> [EXEC] Fallback texto TRAVA");
        }
    }
    break;
```

### **.csproj - Adicionar Pasta**

```xml
<None Include="[nome_pasta]\**\*" CopyToOutputDirectory="PreserveNewest" />
```

### **Servidor - Enviar Comando**

```csharp
SendCommandAsync(new ScreenCommand {
    Type = "LOCK_SCREEN_[NOME]",
    Payload = ""
});
```

---

## 💡 Dicas Importantes

✅ **Nomes de Pasta:** Use nomes descritivos sem espaços (ex: `rosa_deserto`, `mensagem_sistema`)

✅ **Arquivo Padrão:** Sempre use `00.bmp` como arquivo padrão em cada pasta

✅ **Múltiplos Arquivos:** Cada pasta pode ter múltiplos BMPs (00.bmp, 01.bmp, 02.bmp)

✅ **Organização:** Agrupe overlays relacionados na mesma pasta

✅ **Documentação:** Adicione README.txt em cada pasta explicando seu propósito

✅ **Versionamento:** As pastas de overlay podem ser versionadas no git

---

## 🎯 Resumo Rápido

```bash
# 1. Criar pasta
ClienteScreen/nova_pasta/00.bmp

# 2. Adicionar no .csproj
<None Include="nova_pasta\**\*" CopyToOutputDirectory="PreserveNewest" />

# 3. Adicionar comando no Program.cs
case "LOCK_SCREEN_NOVA":
    imagePath = GetOverlayImage("nova_pasta");
    break;

# 4. Recompilar
publicar.bat

# 5. Testar
SendCommandAsync(new ScreenCommand { Type = "LOCK_SCREEN_NOVA", Payload = "" });
```

---

**Desenvolvido em:** 2025-12-06
**Versão:** 3.0 - Sistema de Múltiplas Pastas Overlay
