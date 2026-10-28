# Guide: Hent de nyeste features og lav prefabs

Se også `Fjendetyper-boss-score.md` for alle tal (hund, sniper, bombekaster, skjold, boss m.m.).

## 1. Hent de nyeste features

Alt ligger allerede på `main` (commit "Add student toolkit" + "Update Unity editor to 6000.6.7f1").

1. **Luk Unity** (eller lad den stå, den genindlæser bagefter).
2. Hent ændringerne:
   ```bash
   git checkout main
   git pull origin main
   ```
   Har du lokale ændringer, så commit eller stash dem først (`git stash`).
3. Åbn projektet i **Unity 6000.6.7f1** (Unity Hub -> Install Editor, hvis du ikke har den). Pakkerne er også opdateret, så første åbning tager et par minutter.
4. Vent til scripts er kompileret. Der skal ikke stå røde fejl i Console.
5. Står der noget om Input System / "Active Input Handling", så vælg `Input System Package (New)` eller `Both` under Project Settings -> Player.

## 2. Nye scripts (Assets/Scripts)

| Script | Bruges til |
|---|---|
| `PlayerController`, `WeaponHolder`, `PlayerInventory` | Ny spiller: bevægelse, dash, våbenskift, ammo, nøgler |
| `WeaponData` | ScriptableObject: **Create -> Top Down -> Weapon**. Ét asset pr. våben (pistol, haglgevær, kniv, granat...) |
| `Health` | Liv for ALT (spiller, fjender, tønder, boss). Point ved død, drop, events |
| `EnemyAI` | Fjende: patrulje, syn, hørelse, sniper-laser, retræt, alarm |
| `BossAI` | Boss med faser og angreb |
| `Shield`, `Explosion`, `Grenade`, `Noise` | Skjold, eksplosion, granat, lyd |
| `Pickup` | Health, ammo, våben, nøgle, score-samleobjekt |
| `Door`, `Switch` | Døre og knapper/trykplader (kobles uden kode via UnityEvents) |
| `ScoreManager`, `ScorePopup` | Score, combo, high score |

De gamle scripts (`Enemy`, `EnemyGun`, `PlayerHealth`, `PlayerMovement`, `GunAim`, `Gun`) virker stadig, men de nye prefabs bruger dem ikke.

## 3. Opsætning én gang i projektet

- **Tags**: `Player` og `Enemy` skal findes. Husk at sætte **Player-prefabens rod til tag `Player`** (den står som Untagged nu).
- **Layer**: lav en layer `Walls`, sæt vægge/tilemap på den, og brug den som `Obstacle Layers` på alle fjender.
- **Collision matrix** (Project Settings -> Physics 2D): kuglerne skal kunne ramme Walls, Player og Enemy.

## 4. Opdater Player-prefaben

1. Åbn `Assets/Prefabs/Player.prefab`. Fjern `PlayerMovement`, `GunAim` og `PlayerHealth`.
2. Tilføj `PlayerController` (`Health` og `PlayerInventory` følger automatisk) og `WeaponHolder`.
3. `Health`: `Max Health` 3, `Invincible Time` 1, evt. `Restart Scene On Death`.
4. `WeaponHolder`: læg dine WeaponData-assets i `Start Weapons`. Træk et child-objekt ind i `Weapon Pivot` (og et i `Muzzle`).
5. Tag = `Player`.

## 5. Lav fjende-prefabs (variants)

1. Lav en basis: `Assets/Prefabs/Enemy.prefab` -> fjern `Enemy` og `EnemyGun` -> tilføj `EnemyAI` (+ `Health` kommer med). Tag `Enemy`, Collider2D, `Obstacle Layers = Walls`.
2. Træk den ud i Scene, og vælg **Prefab -> Save as Original** (kald den `Enemy_Base`). Slet fra scenen.
3. For **hver type**: højreklik `Enemy_Base` -> **Create -> Prefab Variant**. Giv den navn og sæt tallene fra `Fjendetyper-boss-score.md`:

| Variant | Våben (WeaponData) | Nøgletal |
|---|---|---|
| `Enemy_Dog` | `Is Melee`, Range 1, Interval 0.6 | Chase Speed 6.5, Health 1, Score 100 |
| `Enemy_Shooter` | Pistol/riffel | Standard, Score 150 |
| `Enemy_Sniper` | Damage 3, Spread 0, Reload 2 | View Angle 40, Charge Time 1.2, + `LineRenderer` i `Laser`, Score 300 |
| `Enemy_Bomber` | Bullet Prefab = Grenade-prefab | Retreat Distance 3, Stop Distance 5, Score 250 |
| `Enemy_Shield` | Pistol | + `Shield`, Health 4, Chase Speed 2.5, Score 200 |
| `Boss` | to-tre `WeaponData` | `BossAI`, Health 30-60, `Health Bar`, Score 5000 |

Hver type skal bruge **sit eget WeaponData-asset** (Create -> Top Down -> Weapon).

## 6. Andre prefabs

- **Grenade**: sprite + `Rigidbody2D` (Gravity 0) + `Grenade` + `Explosion` (Radius 2.5).
- **Eksplosiv tønde**: sprite + Collider2D + `Health` (Max 1) + `Explosion`, og `Explosion.Explode()` i `Health -> On Death`.
- **Pickup**: sprite + Collider2D + `Pickup`. Lav en variant pr. `Type` (Health, Ammo, Weapon, Key, Score).
- **Door** og **Switch**: læg på sprite med Collider2D. Træk døren ind i `Switch -> On Activated` og vælg `Door.Open`.
- **ScoreManager**: tomt GameObject i banen + TextMeshPro-tekster i Canvas.

## 7. Test-tjekliste

- [ ] Ingen røde fejl i Console
- [ ] Spilleren kan gå, rulle (Space) og skyde
- [ ] Fjenden patruljerer, ser dig og jager dig (se synskeglen i Scene-view)
- [ ] Skud får fjenden til at lytte (Noise Radius)
- [ ] Død giver point og combo
- [ ] Skjold blokerer forfra, men ikke bagfra
- [ ] Boss skifter fase ved `Starts Below Health`

## Typiske fejl

- **Fjenden ser igennem vægge** -> `Obstacle Layers` ikke sat til Walls.
- **Intet sker ved skud** -> Tag `Enemy`/`Player` mangler, eller kuglen har ingen `Bullet Prefab` i WeaponData.
- **Ingen point** -> der mangler en `ScoreManager` i scenen, eller `Score On Death` er 0.
- **Prefab-variant ændrer sig ikke** -> du har rettet i scenen i stedet for i prefaben (Overrides -> Apply).

## Færdige prefabs (allerede lavet)

I `Assets/Prefabs/`: `Enemy_Shooter`, `Enemy_Dog`, `Enemy_Sniper` (med laser), `Enemy_Bomber`, `Enemy_Shield`, `Boss` (2 faser), `Grenade` og `ExplosiveBarrel`.
I `Assets/Prefabs/Weapons/`: WeaponData til alle fjender og bossen samt `Player_Pistol`, `Player_Shotgun` og `Player_Knife` (træk dem ind i `WeaponHolder -> Start Weapons`).
Tallene følger `Fjendetyper-boss-score.md`. Prefabs'ene er firkanter i forskellige farver, så skift sprite selv. Brug fx `Enemy_Shooter` som udgangspunkt for nye **Prefab Variants**.
Tag `Enemy` og `Obstacle Layers = Walls` (layer 6) er allerede sat. Du skal stadig have en `ScoreManager` i scenen, hvis du vil have point.

## Lyd med tilfældig pitch (nyt)

Alle lyd-felter er nu et `SoundEffect` i stedet for en rå lydfil: `clip` (lyden), `volume`, `Min Pitch` og `Max Pitch`.
Hver gang lyden spilles, vælges en tilfældig pitch mellem min og max (våben: 0.9-1.1), så skud ikke lyder ens. Sæt Min = Max for ingen variation.
Lyden spiller på sit eget midlertidige objekt, så den høres færdig, selvom fjenden, kuglen eller pickuppen er væk med det samme.

Hvor kan man sætte lyd på:
- `WeaponData -> Attack Sound`: skud og slag.
- `Health -> Hurt Sound` og `Death Sound`: når noget rammes eller dør (spiller, fjender, boss, tønder).
- `Pickup -> Pickup Sound`, `Door -> Open/Close/Locked Sound`, `Switch -> Switch Sound`, `Explosion -> Sound`, `Shield -> Block Sound`.

## Pickup- og dør-prefabs

`Pickup_Health`, `Pickup_Ammo`, `Pickup_Coin` (250), `Pickup_Gold` (1000), `Pickup_Key_Red`, `Pickup_Key_Blue`, `Pickup_Weapon_Shotgun` og `Pickup_Weapon_Knife`.
`Door_Interact` (tryk E), `Door_Automatic`, `Door_Key_Red`, `Door_Key_Blue` (kræver nøgle med samme id) og `Door_Switch` (åbnes kun af en `Switch` eller et event).
Dørene ligger på layer `Walls`, så fjender ikke ser igennem dem, og de glider 2 enheder til siden. Skal de glide op/ned, så ret `Slide Offset` til fx (0, 2).
