# ⚠️ IMPORTANTE: Executar como Administrador

## Por que precisa de privilégios de Administrador?

O **ClienteScreen.exe** requer privilégios de administrador para que a funcionalidade de **trava de tela** funcione corretamente.

### Recursos que dependem de Admin:

1. **BlockInput()** - API do Windows que bloqueia entrada de teclado/mouse
2. **Hooks de baixo nível** - InputBlocker usa hooks globais (WH_KEYBOARD_LL, WH_MOUSE_LL)
3. **Controle total da interface** - Necessário para prevenir bypass da trava

---

## 🚀 Como Executar como Administrador

### Método 1: Clique direito (Recomendado)

1. Localize o arquivo `ClienteScreen.exe`
2. **Clique direito** no executável
3. Selecione **"Executar como administrador"**
4. Clique em **"Sim"** no prompt do UAC (Controle de Conta de Usuário)

### Método 2: Propriedades (Permanente)

1. Localize o arquivo `ClienteScreen.exe`
2. **Clique direito** → **"Propriedades"**
3. Aba **"Compatibilidade"**
4. Marque ☑ **"Executar este programa como administrador"**
5. Clique **"Aplicar"** → **"OK"**

Agora o programa **sempre** executará como admin ao dar duplo clique.

### Método 3: Prompt de Comando (CMD)

```cmd
# Abra o CMD como administrador (Clique direito → "Executar como administrador")
cd C:\caminho\para\o\executavel
ClienteScreen.exe
```

### Método 4: PowerShell

```powershell
# Abra o PowerShell como administrador
Start-Process "C:\caminho\para\ClienteScreen.exe" -Verb RunAs
```

---

## 🔍 Como Verificar se Está Rodando como Admin

Quando o cliente inicia, ele exibe no console:

```
[LOCK] Executando como Administrador: True  ✅ (CORRETO)
```

Se aparecer `False`, você verá avisos:

```
[LOCK] Executando como Administrador: False  ❌ (INCORRETO)
[LOCK] AVISO: Cliente não está executando como administrador!
[LOCK] AVISO: BlockInput() e hooks podem não funcionar corretamente!
[LOCK] AVISO: Execute o ClienteScreen.exe como administrador para bloqueio efetivo!
```

---

## ⚙️ Manifesto Incorporado

A partir desta versão, o executável possui um **manifesto incorporado** (`app.manifest`) que solicita automaticamente privilégios de administrador ao executar.

### O que isso significa?

Ao dar **duplo clique** em `ClienteScreen.exe`, o Windows **automaticamente** mostrará o prompt do UAC solicitando permissão de administrador. Basta clicar em **"Sim"**.

---

## 🐛 Solução de Problemas

### Problema: Overlay aparece mas não bloqueia entrada

**Causa:** Cliente não está executando como administrador.

**Solução:**
1. Feche o cliente
2. Execute novamente como administrador (Método 1 ou 2 acima)
3. Verifique no log: `[LOCK] Executando como Administrador: True`

### Problema: UAC não aparece automaticamente

**Causa:** Manifesto não foi incorporado corretamente na compilação.

**Solução:**
1. Recompile o projeto: `compilar.bat`
2. Ou execute manualmente como admin (Método 1 acima)

### Problema: "BlockInput() retornou: False"

**Causa:** Permissões insuficientes ou outra aplicação está bloqueando.

**Solução:**
1. Certifique-se de estar executando como admin
2. Feche outros programas de controle remoto (TeamViewer, AnyDesk, etc.)
3. Verifique antivírus não está bloqueando

---

## 📝 Notas Técnicas

### Por que BlockInput() precisa de Admin?

Por segurança, o Windows restringe a API `BlockInput()` para impedir que aplicativos maliciosos travem o sistema. Apenas processos com privilégios elevados podem usar essa função.

### Alternativas ao BlockInput()

O código usa **duas estratégias** de bloqueio:

1. **BlockInput()** - Bloqueia entrada globalmente (requer admin)
2. **InputBlocker** - Hooks de baixo nível que consomem eventos (requer admin)
3. **Overlay TopMost** - Janela sempre na frente que captura eventos

Com privilégios de admin, todas as 3 estratégias funcionam, garantindo bloqueio efetivo.

---

## ✅ Checklist de Execução

Antes de conectar ao servidor:

- [ ] ClienteScreen.exe executado como administrador
- [ ] Log mostra: `[LOCK] Executando como Administrador: True`
- [ ] UAC foi aceito (clicou "Sim")
- [ ] Firewall permite conexão (se aplicável)
- [ ] Antivírus não está bloqueando o executável

---

## 🆘 Suporte

Se mesmo executando como admin a trava não funciona:

1. Envie o **log completo** do console
2. Informe a **versão do Windows** (Win 10/11)
3. Liste **antivírus** e **software de segurança** instalados
4. Verifique se outros programas de controle remoto estão rodando

---

**Última atualização:** 2025-12-05
