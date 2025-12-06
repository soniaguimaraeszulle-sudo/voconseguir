═══════════════════════════════════════════════════════════════════
  PASTA OVERLAY - Imagens para Tela de Trava
═══════════════════════════════════════════════════════════════════

📁 PROPÓSITO:
   Esta pasta contém imagens BMP que serão exibidas quando o servidor
   ativar a trava do cliente (comando LOCK_SCREEN).

📄 ARQUIVO PRINCIPAL:
   00.bmp  ← Esta é a imagem que será carregada por padrão

📐 ESPECIFICAÇÕES:
   Nome do arquivo: 00.bmp (OBRIGATÓRIO)
   Formato: BMP (Bitmap)
   Recomendação de resolução: 1920x1080 ou similar
   A imagem será redimensionada automaticamente mantendo proporções

✅ FUNCIONAMENTO ATUAL:
   Quando o servidor clicar em "Trava":
   1. Cliente busca especificamente por "00.bmp"
   2. Carrega e exibe em tela cheia com fundo preto
   3. Se não encontrar, mostra texto "TRAVA" (fallback)

🔄 ATUALIZAÇÃO DA BUILD:
   Esta pasta é copiada automaticamente para o diretório de saída
   durante a compilação. Todos os arquivos .bmp serão incluídos.

📝 ESTRUTURA ATUAL:
   ClienteScreen/
   └── overlay/
       ├── 00.bmp              ← IMAGEM PADRÃO
       ├── README.txt          ← Este arquivo
       └── COMO_ADICIONAR_OVERLAYS.md  ← Guia para múltiplos overlays

🎯 FUTURAS IMPLEMENTAÇÕES:
   Para usar múltiplas imagens overlay (01.bmp, 02.bmp, etc.),
   consulte o arquivo: COMO_ADICIONAR_OVERLAYS.md

═══════════════════════════════════════════════════════════════════
