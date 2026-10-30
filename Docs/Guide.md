# Guide: Top-down shooter

Alt ligger i `Assets/Prefabs`. Træk prefabs ud i scenen og skift sprites. Der skal ikke skrives kode.

Den store avancerede version (boss, combo-score, granater osv.) ligger på git-branchen `avanceret-version`, hvis man får brug for den senere.

## Hvad er der?

| Prefab | Hvad gør den |
|---|---|
| `Player` | Gå med WASD, rul med Space, sigt med musen, skyd med venstre musetast, R = reload. Har pistol (`Hand`) og `Shotgun`. Skift med 1 / 2 eller musehjulet |
| `Enemy` | Din egen fjende (skytte) |
| `Enemy_Shooter` | Går efter dig og skyder |
| `Enemy_Dog` | Hurtig, løber ind i dig. Skader ved berøring |
| `Enemy_Sniper` | Står stille, ser langt, skyder hårdt og præcist |
| `Pickup_Health` | Giver liv |
| `Pickup_Ammo` | Giver ammo til det våben man holder |
| `Pickup_Shotgun` | Giver haglgeværet |
| `Key` + `Door` | Døren åbner når man har taget nøglen |

## Skift grafik

- **Spiller og fjender:** vælg objektet og træk sprites ind i `Sprite Up / Down / Left / Right` (på `Player Movement` eller `Enemy`). Tomme felter = billedet skifter ikke.
- **Våben:** vælg `Gun` under `Hand` (eller `Shotgun`) og skift sprite på Sprite Renderer.
- **Pickups, nøgle og dør:** skift sprite på Sprite Renderer.

## Våben

Våbnene er children af `Player`: `Hand` (pistol) og `Shotgun`. Indstillingerne står i `Gun`-scriptet:
- `Damage`, `Bullet Speed`, `Shoot Interval` (tid mellem skud)
- `Magazine Size`, `Total Ammo`, `Reload Time`
- `Bullets Per Shot` og `Spread`: haglgevær = fx 5 kugler og 15 graders spredning
- `Shoot Sound`: lyd. `Min Pitch` / `Max Pitch` gør at lyden er lidt forskellig hver gang

**Man starter kun med det første våben.** De andre skal samles op med en pickup.
Vil du teste alle våben med det samme: slå `Start With All Weapons` til på `Weapon Switcher` (på Player).

### Lav et nyt våben (fx Uzi)
1. Åbn `Player`-prefaben. Højreklik på `Shotgun` -> **Duplicate**. Omdøb til `Uzi`.
2. Ret tallene på `Gun` (fx `Shoot Interval` 0.05, `Bullets Per Shot` 1, `Spread` 5) og skift sprite.
3. Vælg `Player` og træk `Uzi` ind i listen `Weapons` på `Weapon Switcher`. Den er nu våben nr. 3.
4. Lav en pickup: duplikér `Pickup_Shotgun`, omdøb til `Pickup_Uzi` og skriv `Uzi` i `Weapon Name`. **Navnet skal være præcis det samme** som objektet under Player.

## Fjender

Indstillinger på `Enemy`:
- `Max Health`: liv
- `Detection Range`: hvor langt den kan se (gul cirkel i Scene-vinduet)
- `Chase Speed`: 0 = står stille. `Stop Distance`: hvor tæt den går på
- `Obstacle Layers`: sæt til `Walls`, så den ikke kan se igennem vægge

Indstillinger på fjendens `Gun` (`Enemy Gun`): skade, fart, spredning, hvor hurtigt den skyder og reagerer.

Alle fjender skader spilleren, når de rører ham (det står på spillerens `Player Health`: `Damage Tag = Enemy`).
Lav nye fjendetyper: duplikér en fjende-prefab og ret tallene og sprites.

## Pickups

Pickups skal have en Collider2D med **Is Trigger** slået til (det har prefabs allerede).
- `Pickup_Health`: `Heal Amount` = hvor meget liv
- `Pickup_Ammo`: `Ammo Amount` = hvor mange skud
- `Pickup_Shotgun`: `Weapon Name` = navnet på våbnet under Player
- Alle har `Pickup Sound`

## Nøgle og dør

1. Træk en `Key` og en `Door` ud i scenen.
2. Vælg **døren i Hierarchy** og træk **nøglen fra Hierarchy** ned i feltet `Key`.
3. Spilleren går ind i døren: uden nøgle står der "Døren er låst" i Console. Med nøgle forsvinder døren.

Er `Key` tom, åbner døren bare når man går ind i den.
Lav flere: hver dør får sin egen nøgle trukket ind.

## UI

På `Player`:
- `Player Health` -> `Health Bar`: et Image med `Image Type = Filled`, `Fill Method = Horizontal`
- `Weapon Switcher` -> `Ammo Text`: TextMeshPro-tekst til ammo (bruges af alle våben)
- `Weapon Switcher` -> `Weapon Name Text`: viser navnet på våbnet

## Hvis noget ikke virker

- **Kan ikke samle op:** spilleren skal have tag `Player`. Pickuppen skal have Is Trigger slået til.
- **Fjenden gør ingenting:** spilleren skal have tag `Player`. Er der en væg imellem?
- **Fjenden ser igennem vægge:** `Obstacle Layers` = `Walls`, og væggene skal ligge på laget `Walls`.
- **Våben-pickup virker ikke:** står der en gul advarsel i Console, er `Weapon Name` stavet forkert.
- **Røde fejl i Console:** så virker ingen scripts. Ret den første fejl først.
