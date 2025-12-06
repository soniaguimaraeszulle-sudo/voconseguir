═══════════════════════════════════════════════════════════════════
  PASTA OVERLAY - Imagens para Tela de Trava
═══════════════════════════════════════════════════════════════════

📁 PROPÓSITO:
   Esta pasta contém a imagem BMP que será exibida quando o servidor
   ativar a trava do cliente (comando LOCK_SCREEN).

📄 COMO USAR:
   1. Adicione sua imagem BMP nesta pasta
   2. Formato suportado: .bmp (Bitmap)
   3. O sistema carregará automaticamente a primeira imagem .bmp encontrada

📐 RECOMENDAÇÕES:
   - Use imagens de boa qualidade
   - A imagem será redimensionada automaticamente para caber na tela
   - Aspect ratio será preservado (não distorce)
   - Prefira imagens com resolução adequada (ex: 1920x1080)

✅ FUNCIONAMENTO:
   Quando o servidor clicar em "Trava":
   - Cliente busca arquivos .bmp nesta pasta
   - Carrega o primeiro .bmp encontrado
   - Exibe em tela cheia com fundo preto
   - Se não houver imagem, mostra texto "TRAVA" (fallback)

🔄 ATUALIZAÇÃO DA BUILD:
   Esta pasta é copiada automaticamente para o diretório de saída
   durante a compilação. Qualquer arquivo .bmp aqui será incluído
   no executável final.

📝 EXEMPLO:
   ClienteScreen/
   └── overlay/
       └── sua_imagem.bmp  ← Adicione sua imagem aqui

═══════════════════════════════════════════════════════════════════
