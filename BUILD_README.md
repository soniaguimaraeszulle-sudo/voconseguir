# 🔨 Scripts de Compilação - Screen Panel

Este diretório contém scripts batch para facilitar a compilação e publicação do projeto Screen Panel.

## 📋 Pré-requisitos

- **Windows** (OS requerido)
- **.NET 8.0 SDK** instalado ([Download aqui](https://dotnet.microsoft.com/download/dotnet/8.0))

Para verificar se o .NET SDK está instalado, execute no PowerShell ou CMD:
```bash
dotnet --version
```

## 🚀 Scripts Disponíveis

### 1️⃣ `compilar.bat` - Compilação Padrão

Compila todos os componentes do projeto em modo **Release**.

**Quando usar:**
- Durante o desenvolvimento
- Para testes rápidos
- Requer .NET 8.0 instalado na máquina de destino

**Como usar:**
```bash
# Duplo clique no arquivo ou execute via CMD:
compilar.bat
```

**Saída:**
- `Hook\bin\Release\net8.0-windows\Hook.dll`
- `ClienteScreen\bin\Release\net8.0-windows\ClienteScreen.exe`
- `ServidorScreenPanel\bin\Release\net8.0-windows\ServidorScreenPanel.exe`

---

### 2️⃣ `publicar.bat` - Publicação Standalone

Cria executáveis **standalone** (independentes) que não requerem .NET instalado na máquina de destino.

**Quando usar:**
- Para distribuir para usuários finais
- Para criar pacotes de instalação
- Quando o destino não tem .NET 8.0 instalado

**Como usar:**
```bash
# Duplo clique no arquivo ou execute via CMD:
publicar.bat
```

**Saída:**
- `Publicado\Cliente\ClienteScreen.exe` (standalone)
- `Publicado\Servidor\ServidorScreenPanel.exe` (standalone)

> ⚠️ **Nota:** Os executáveis standalone são maiores (~60-80 MB cada) pois incluem o runtime .NET.

---

### 3️⃣ `limpar.bat` - Limpeza de Binários

Remove todos os arquivos compilados e pastas geradas (bin/, obj/, Publicado/).

**Quando usar:**
- Antes de recompilar do zero
- Para economizar espaço em disco
- Antes de fazer commit (opcionalmente)

**Como usar:**
```bash
# Duplo clique no arquivo ou execute via CMD:
limpar.bat
```

> ⚠️ **Atenção:** Este script requer confirmação antes de executar.

---

## 📂 Estrutura do Projeto

```
voconseguir/
├── Hook/                      # Biblioteca de injeção de entrada
│   └── Hook.csproj
├── ClienteScreen/             # Aplicação cliente (transmite tela)
│   └── ClienteScreen.csproj
├── ServidorScreenPanel/       # Aplicação servidor (recebe tela)
│   └── ServidorScreenPanel.csproj
├── compilar.bat              # Script de compilação
├── publicar.bat              # Script de publicação standalone
├── limpar.bat                # Script de limpeza
└── BUILD_README.md           # Este arquivo
```

## 🔧 Ordem de Compilação

Os scripts respeitam automaticamente a ordem de dependências:

1. **Hook** (biblioteca base)
2. **ClienteScreen** (depende do Hook)
3. **ServidorScreenPanel** (depende do Hook)

## ❓ Solução de Problemas

### Erro: ".NET SDK não encontrado"
- Instale o .NET 8.0 SDK: https://dotnet.microsoft.com/download/dotnet/8.0

### Erro: "Falha ao compilar"
1. Execute `limpar.bat` primeiro
2. Execute `compilar.bat` novamente
3. Verifique os logs de erro exibidos no console

### Executável não abre
- **Versão compilada:** Instale .NET 8.0 Runtime na máquina
- **Versão publicada:** Use o executável standalone da pasta `Publicado/`

## 📝 Notas Adicionais

- Todos os scripts geram saída em modo **Release** (otimizado)
- Para modo **Debug**, edite os scripts e altere `-c Release` para `-c Debug`
- Os scripts pausam ao final para visualizar mensagens
- Use Ctrl+C para cancelar a compilação em andamento

## 🆘 Suporte

Para problemas relacionados à compilação, verifique:
- Versão do .NET SDK: `dotnet --version`
- Logs de erro no console
- Arquivo `ClienteScreen_error.log` (se o cliente crashar)
- Arquivo `ServidorScreenPanel_error.log` (se o servidor crashar)
