# Kael — Sombras de Além · Contexto do jogo

Plataforma 2D de ação em pixel art (Unity 6, URP 2D), criado por **Gustavo Agudo**, **Guilherme Rezende** e **João Calegari**. Este documento resume a história, as fases, os personagens e as regras visuais, para orientar quem cria arte, interface ou conteúdo novo.

---

## 1. Premissa
Durante séculos, o **Selo das Duas Luas** manteve o Reino das Sombras separado do mundo humano. Numa noite de eclipse, o selo se partiu e as sombras corromperam florestas, ruínas e cidades.

**Kael** desperta sozinho sob os escombros do **Templo da Lua**, sem lembrar do ataque que destruiu sua ordem. Sua única pista é **Lyria**, um pequeno espírito azul que parece conhecer o passado dele. Juntos, buscam os **Fragmentos da Lua** para restaurar o selo. No caminho, descobrem que o selo não foi quebrado por um monstro, e sim por alguém de dentro da própria ordem dos guardiões.

**Temas:** memória, identidade, responsabilidade pelas próprias escolhas, equilíbrio entre luz e sombra.
**Tom:** dark fantasy acessível. Melancólico, mas bonito, e sem gore.

## 2. Personagens
| Personagem | Papel | Visual |
|---|---|---|
| **Kael** | protagonista, jovem guardião do Templo da Lua | cabelo branco-lilás, cachecol/manto **vermelho vivo**, roupa azul-escura, espada "Essência Lunar" com brilho **ciano** |
| **Lyria** | espírito guia (ainda não está no jogo) | pequena luz azul, forma de espírito |
| **Rastejante** | inimigo terrestre comum; patrulha plataformas | criatura corrompida com a cabeça larga e achatada (dá para pisar), olhos magenta |
| **Mariposa Umbral** | inimigo aéreo comum; voa entre pontos | mariposa de sombra com núcleo brilhante |
| **Guardião da Floresta Corrompido** | chefe da Floresta Sombria | grande cervo de raízes, com um fragmento de lua preso no peito |
| **Capitão Espectral** | chefe das Ruínas da Aurora | cavaleiro fantasma de armadura partida e lança de luz roxa |
| **O Guardião Caído** | chefe final; o traidor do selo | espelho sombrio do Kael: mesma silhueta de manto, tons violeta e prata, olhos magenta |

## 3. Fases
O que liga as fases visualmente é **a lua no céu, que vai sendo eclipsada a cada região**, mais as **runas lunares ciano** presentes em todas as ruínas.

| Ordem | Cena Unity | Região | Clima | Paleta de acento | Lua | Chefe |
|---|---|---|---|---|---|---|
| — | `__Main` | **Templo da Lua** | ruínas do despertar, pilares, estátuas de guardiões | branco lunar + azul | cheia, enorme | — |
| 1 | `__Level01` | **Floresta Sombria** | árvores retorcidas, névoa violeta, cogumelos ciano | verde-musgo escuro + ciano | minguante | Guardião da Floresta |
| 2 | `__Level02` | **Ruínas da Aurora** | aquedutos suspensos, pedras flutuantes, cristais; plataformas que caem sobre espinhos | âmbar/rosa da aurora vs. noite | coberta pela aurora | Capitão Espectral |
| 3 | `__Level03` | **Trono das Sombras** (final) | arquitetura gótica partida, correntes, trono distante | violeta profundo + anel branco | **eclipse total** | O Guardião Caído |
| futura | — | **Cidade do Eclipse** | telhados, torres de sino, lanternas apagadas | cinza-azulado + laranja fraco | quase total | (Capitão Espectral) |

Cada fase tem um percurso de plataforma (ilhas de chão separadas por abismos, inimigos da região, armadilhas, gemas e checkpoints) e termina numa **arena de chefe**. A porta de saída só aparece depois que o chefe é derrotado. O jogo começa na **tela inicial** (`__Title`); ao derrotar o chefe final, os **créditos** sobem e o jogo volta ao menu.

## 4. Jogabilidade
- **Movimento:** andar e correr (A/D ou ←/→), pular (Espaço, W ou ↑) e deslizar na parede.
- **Combate:** golpe de espada com **J** ou clique esquerdo. No ar, o golpe vira um ataque aéreo. Pular na cabeça dos inimigos também os derrota.
- **Parry:** se o Kael **começa um golpe** (até 0,3 s antes) no momento em que um inimigo ou chefe iria acertá-lo (encostar ou golpear) e o atacante está perto (até 3 unidades), o dano é bloqueado. O Kael brilha em ciano e fica invulnerável por 0,6 s para esquivar e se reposicionar. Só vale contra inimigos; serras, espinhos e abismos não podem ser aparados. Num **chefe**, o parry interrompe o golpe e o deixa atordoado por ~1 s (sem dano de contato), e projéteis aparados são desviados.
- **Esquiva:** **K**, **Shift** ou botão direito. Um rolamento rápido (~3 unidades) na direção segurada, durante o qual o Kael fica invulnerável e atravessa inimigos. Uma vez por pulo no ar, com pequena recarga.
- **Vida:** a partida começa com **3 corações**, que passam de uma fase para a outra. Ao levar dano, o Kael pisca e fica invulnerável por 1 s. O HUD mostra os corações cheios e os espaços vazios.
- **Chefes recompensam:** derrotar um chefe **restaura toda a vida** e dá **+1 coração máximo** pelo resto da partida (3 → 4 → 5).
- **Checkpoints:** pedras rúnicas ao longo das fases (Floresta: 5; Ruínas: 3; Trono: 2, com fragmentos de lua), sempre um na entrada de cada arena. Ao tocar, ela acende em ciano, vira o ponto de retorno e devolve 1 coração (só na primeira vez).
- **Quedas:** cair do mapa **custa 1 coração** e leva de volta ao último checkpoint (ou ao início da fase), com 1,5 s de invulnerabilidade.
- **Fim da partida:** ao perder o último coração, aparece o game over; "tentar de novo" recomeça a partida desde a fase 1, com 3 corações.
- **Pontuação:** moedas, gemas e inimigos derrotados.
- **Perigos:** serras, espinhos, bolas de espinho, plataformas que caem (voltam ao lugar ~3 s depois) e abismos.
- **Chefes:** cada golpe tem um aviso visual antes e uma pausa depois, que é a hora de revidar. A barra de vida do chefe aparece no topo da tela.
- **Pausa:** ESC abre o painel de opções (volume de som e de música).
- **Tela cheia:** o jogo gerado (build) abre em tela cheia; a tecla **F** alterna entre tela cheia e janela.
- **Game over:** ao perder todos os corações, aparece a tela de reinício (tentar de novo / menu).

## 5. Direção de arte
- **Pixel art** sem anti-aliasing, com cores chapadas e dithering manual. Contorno de 1 px azul-noite (nunca #000000) em personagens.
- **Luz** vindo de cima-esquerda.
- **Paleta-mãe:** azul-noite `#141a33`, violeta escuro `#2b1f45`, branco lunar `#e8e6ff`, vermelho do Kael `#d8343f` (exclusivo dele), ciano lunar `#5fe3f0` (espada, runas e luz boa).
- **Corrupção / inimigos:** violeta e magenta `#d63fa0`, verde-ácido `#a6e33a` nos olhos e núcleos. Inimigos **nunca** usam o vermelho do Kael nem o ciano.
- **Densidade de pixel:** cenário e tiles com 32 px por unidade. Personagens com a mesma densidade do Kael (≈ 40 px por unidade; o Kael tem ~65 px de altura).

## 6. Dados técnicos úteis
- **Câmera:** ortográfica, mostra ~30 × 17 unidades. Interface feita para **1920 × 1080** (Canvas com "Scale With Screen Size").
- **Fontes no projeto:** Pixelify Sans (OFL, com acentos; usada na tela inicial) e Byeoljari (sem acentos; ainda usada no HUD e nos painéis das fases). Os textos da interface são feitos com TMP, não desenhados dentro das imagens.
- **Pastas de arte:** `Assets/GOTY/Kael` (protagonista), `Assets/GOTY/Art` (fundos, tiles, inimigos), `Assets/GOTY/Resources/Bosses` (chefes e efeitos).
- **Áudio:** `Assets/GOTY/Resources/Audio/Music` e `/SFX`, tocados pelo `GameAudio` (criado sozinho em qualquer cena). Cada fase tem música de exploração + camada de combate sincronizada (sobe com inimigos a menos de ~9 unidades), ambiente próprio, música de chefe na arena e stingers (chefe derrotado, fase concluída, vitória final, game over). Efeitos são chamados por nome (`GameAudio.Sfx("SFX_Kael_Pulo")`) e as variações `_01`, `_02`... são sorteadas. Volumes vêm dos sliders (PlayerPrefs `MusicVolume`/`SoundVolume`).
- **Direção musical:** dark fantasy sacra e melancólica, com composição própria: harmonia frígia, violão de nylon, coro masculino, órgão, cordas graves, trompas, sinos e percussão pesada. O tema do Kael reaparece em todas as regiões; os chefes são mais rápidos e tensos (6/8, marcha 4/4 e 7/8 no chefe final). Evitar tons puros agudos e drones graves constantes. Gerador em `GOTY/Kael_Musica_v2/_scripts`; versão anterior em `GOTY/Kael_Audio_antigo`.
- **Ainda com arte provisória** (de packs antigos): moedas, gemas, corações, serras, espinhos, bolas de espinho, plataformas de madeira, porta e a interface inteira.
