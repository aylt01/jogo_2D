# Cavaleiro Pixel — guia do projeto e próximos passos

Unity 6000.6 (2D, URP). **Toda a arte e todos os sons são gerados por código** (nada de imagens ou áudios externos).

## Como jogar
| Tecla | Ação |
|---|---|
| A / D ou setas | andar |
| Espaço ou W | pular (soltou antes = pulo curto) |
| J, Z ou botão esquerdo do mouse | atacar com a espada |
| X ou Shift | **dash** (liberado ao pegar o baú da fase 1) |
| Esc ou P | pausar |
| W/S + Enter ou mouse | menus |

## O que o jogo tem
- **Tela de início** (ilustração em pixel art, Jogar / Como jogar / Sair), **pausa**, **tela de morte** (tentar de novo / menu), tela de fase completa e de vitória.
- **Herói animado**: parado, correndo, pulo, queda, ataque (3 quadros + arco da espada), dano, dash e morte (3 quadros). 5 corações de vida, invencibilidade curta depois de levar dano e volta ao último lugar seguro se cair num buraco.
- **Inimigos animados**: slime (pula), morcego (voa atrás de você, morde e recua), esqueleto (persegue e ataca levantando a espada) e o **Rei Esqueleto** (chefe dourado com barra de vida).
- **Fase 1 (cena `a`)**: o cenário que você já tinha + inimigos e corações espalhados. O baú dá o dash.
- **Fase 2 (cena `a2`) — masmorra**: salão de entrada, sala de buracos com espinhos, escadaria, corredor de esqueletos, sala do chefe. Ache a **chave** e abra a **porta** para vencer.
- Efeitos: partículas, tremor de câmera, "hit stop" nos golpes, tochas com brilho, música e sons sintetizados.

## Como o código está organizado (`Assets/script/`)
| Arquivo | O que faz |
|---|---|
| `GameManager.cs` | O "diretor". **Se cria sozinho em qualquer cena**. Controla os estados (início, jogando, pausa, morte, fase completa, vitória), desenha menus e HUD, prepara a fase 1 e chama o construtor da masmorra |
| `player.cs` | Herói: movimento, pulo, ataque, vida, dash, morte, animação |
| `Inimigo.cs` | Slime, esqueleto e chefe (patrulha, perseguição, golpe) |
| `InimigoSeguri.cs` | Morcego (era o quadrado vermelho da fase 1) |
| `DungeonBuilder.cs` | Monta a fase 2 a partir de um **mapa em texto** (as letras estão explicadas no topo do arquivo) |
| `PixelArt.cs` | Desenha todos os sprites por código (herói, inimigos, tiles, porta, ícones, tela inicial) |
| `Som.cs` | Gera os efeitos sonoros e a música |
| `CameraSegue.cs` | Câmera com suavidade, olhar à frente e tremor |
| `Coletavel.cs`, `PortaMasmorra.cs`, `Tocha.cs`, `Efeitos.cs` | Chave/coração, porta, tochas, partículas |
| `Entrada.cs` | Todas as teclas num lugar só |
| `Portao.cs`, `habilidade.cs` | Portal da fase 1 e baú do dash |

### Como mexer
- **Dificuldade**: vida, velocidade e dano dos inimigos ficam em `Inimigo.cs` (`Configurar`); vida do herói em `player.cs` (`vidaMax`).
- **Sensação do pulo**: `jumpSpeed`, `gravidadeSubindo`, `gravidadeCaindo`, `speed` no `player.cs`. O mapa da masmorra foi feito para o pulo atual (altura ~2,5 blocos, vão de até ~3).
- **Mudar a masmorra**: edite as letras do `Mapa` no `DungeonBuilder.cs` (a primeira linha é a de cima). `#` parede, `^` espinhos, `P` início, `s` slime, `S` esqueleto, `b` morcego, `M` chefe, `k` chave, `h` coração, `D` porta, `T` tocha.
- **Mudar a arte**: os desenhos estão em `PixelArt.cs`. Os personagens são feitos com retângulos/linhas; dá para trocar cores no topo da classe.
- **Nova fase**: crie uma cena, coloque-a na Build Settings (File > Build Profiles) depois da `a2`; o portal/porta leva para a próxima da lista.

## O que foi verificado (testes automáticos em modo batch)
- Projeto compila sem erros.
- Fase 1: abre no menu, começa, ataque mata o slime, dano + invencibilidade, pausa, morte → tela de morte → tentar de novo.
- Fase 2: um "bot" jogou a masmorra inteira (escadaria, corredor, chave, sala do chefe) e chegou na porta, que terminou a fase.
- A arte foi renderizada em PNG para conferir o visual.
- **Não foi possível** testar a janela do jogo com placa de vídeo (o Unity em modo batch travou o driver), então os **menus e o HUD (OnGUI) ainda não foram vistos na tela**. Dê uma olhada ao dar Play.

## Próximos passos sugeridos
1. **Jogue e ajuste**: tamanho das fontes dos menus, dificuldade do chefe, onde ficam os corações.
2. **Arte da fase 1**: o cenário da fase 1 ainda usa as imagens antigas (foto/ilustração). Dá para trocar por pixel art como a masmorra (tiles e fundo em `PixelArt.cs`).
3. **Mais conteúdo**: fase 3, mais tipos de inimigo (arqueiro, fantasma), moedas/itens, pulo duplo como segunda habilidade.
4. **Polimento**: tela de opções (volume), salvar o progresso, tela de créditos, gerar uma **build** (File > Build Profiles) para entregar à escola.
5. **Trocar `OnGUI` por Canvas (uGUI)** se quiser telas mais elaboradas; e o `Input` antigo pelo Input System para aceitar controle.
6. **Git**: faça commit destas mudanças; antes de `Revert`, use branches (a história do projeto teve um conflito de merge commitado por engano).
7. **Organização**: a pasta `Assets/fotos` tem imagens grandes que já não são usadas pelo jogo novo; vale limpar. A cena `Scenes/SampleScene` não está mais na Build Settings.

## Histórico das correções desta rodada
- `a.unity` estava quebrada no git (marcas de conflito `<<<<<<<`); restaurada de um commit bom.
- `habilidade.cs` (classe com nome diferente do arquivo), `Portao.cs`, `InimigoSeguri.cs` e o pulo do `player.cs` corrigidos; componentes fantasma (`playermove`, `PlayerDash`) removidos da cena.
