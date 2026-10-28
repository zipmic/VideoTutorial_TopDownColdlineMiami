# Fjendetyper, boss, score og samleobjekter

Alle tal er forslag til at starte med - eleverne skal skrue på dem.
Alle fjender har tag `Enemy`, en Collider2D, og `Obstacle Layers = Walls` på EnemyAI.

## Score
1. Lav et tomt GameObject `ScoreManager` og læg `ScoreManager` på.
2. Lav UI-tekster (TextMeshPro) til score, combo og evt. high score, og træk dem ind. Træk et Image (Filled) ind i `Combo Timer Bar`.
3. Giv hver fjende point: `Health` -> `Score On Death` (fx hund 100, skytte 150, sniper 300, boss 5000).
4. Valgfrit: lav en `ScorePopup`-prefab (3D Object -> Text - TextMeshPro + `ScorePopup`) og træk den ind i `Popup Prefab`.

Sådan virker comboen:
- Hvert drab inden `Combo Time` sekunder øger combo, og multiplikatoren stiger med `Multiplier Step`.
- Dræber du næsten med det samme efter forrige drab, får du op til `Speed Bonus` ekstra på multiplikatoren.
- Bliver spilleren ramt, nulstilles comboen (kan slås fra).

## Samleobjekter (energidrik, drugs, guld...)
Brug samme `Pickup`-prefab som til health og ammo - men vælg `Type = Score`.
- Skift **sprite** på SpriteRenderer og **lyd** i `Pickup Sound`. Skift `Amount` til hvor mange point den giver.
- Lav gerne flere prefabs (fx "Energidrik" 250 point, "Guldbarre" 1000) og gem dem godt i banen.
- `ScoreManager` tæller dem automatisk og kan vise "Fundet 2 / 5" (`Collectibles Text`).

## Fjendetyper
Lav hver som en **prefab-variant** af Enemy.

### Hund (hurtig nærkamp)
- Health: `Max Health` 1, `Score On Death` 100
- Våben (WeaponData): `Is Melee`, `Melee Range` 1, `Melee Arc` 120, `Shoot Interval` 0.6, `Damage` 1, `Noise Radius` 2
- EnemyAI: `Patrol Speed` 2.5, `Chase Speed` 6.5, `View Distance` 8, `View Angle` 120, `Reaction Time` 0.1, `Turn Speed` 720

### Sniper
- Health: `Max Health` 2, `Score On Death` 300
- Våben: `Damage` 3, `Bullet Speed` 30, `Range` 20, `Spread` 0, `Magazine Size` 1, `Reload Time` 2, `Shoot Interval` 1.5, `Noise Radius` 15
- EnemyAI: `View Distance` 14, `View Angle` 40, `Attack Range` 13, `Stop Distance` 10, `Retreat Distance` 6, `Charge Time` 1.2, `Turn Speed` 90, `Extra Spread` 0
- Laser: tilføj en `LineRenderer` (rød, bredde 0.03) som child og træk den ind i `Laser`. Den vises mens sniperen sigter, så man kan nå at rulle væk.

### Bombekaster
1. Lav en **Grenade-prefab**: sprite + `Rigidbody2D` (Gravity Scale 0) + `Grenade` + `Explosion` (`Radius` 2.5, træk evt. en eksplosions-effekt og lyd ind).
2. Våben: `Bullet Prefab` = granat-prefaben, `Bullet Speed` 7, `Range` 9, `Damage` 2 (bruges som eksplosionens skade), `Magazine Size` 1, `Reload Time` 2.5, `Shoot Interval` 2, `Noise Radius` 12
3. EnemyAI: `Attack Range` 8, `Stop Distance` 5, `Retreat Distance` 3, `Aim Tolerance` 25
- Granaten lander der hvor spilleren stod, da den blev kastet.
- Samme våben kan også gives til spilleren (`Magazine Size` 1, `Max Reserve Ammo` 5) - så kan man kaste granater.

### Skjold
- Tilføj `Shield` ved siden af `Health` (`Block Angle` 160). Evt. gnister og lyd i `Block Effect Prefab` / `Block Sound`.
- Health: `Max Health` 4, `Score On Death` 200. EnemyAI: `Chase Speed` 2.5.
- Skud og slag forfra blokeres. Man skal gå om bag den, eller bruge granater og eksplosive tønder (eksplosioner går forbi skjoldet).

### Eksplosiv tønde
- Sprite + Collider2D + `Health` (`Max Health` 1, `Death Action` Destroy) + `Explosion`.
- Træk `Explosion -> Explode()` ind i `Health -> On Death`. Tønder kan sætte hinanden i gang.

## Boss
1. Lav et Boss-objekt: stor sprite, Collider2D (ikke trigger), tag `Enemy`. Læg `BossAI` på (Health og Rigidbody2D kommer automatisk).
2. `Health`: `Max Health` 30-60, `Score On Death` 5000.
3. `BossAI` får to færdige faser, når den tilføjes. Træk et `WeaponData` ind i hvert skyde-angreb.
   - **AimedBurst**: salve mod spilleren (`Count` skud, `Interval` imellem)
   - **BulletRing**: ring af kugler (`Count` pr. ring, `Repeats` ringe). Sæt våbnets `Spread` til 0
   - **Charge**: løber ind i spilleren, og er bagefter stunnet i `Stun Time` sekunder
   - **Summon**: kalder fjender frem fra en prefab (`Summon Prefab`)
4. En ny fase starter, når livet falder under `Starts Below Health` (fx 0.5 = halvt liv). Fase 2 bør være hurtigere og have flere angreb.
5. **Svagt punkt**: slå `Only Vulnerable When Stunned` til. Så kan bossen kun skades efter et Charge-angreb (husk at have et Charge-angreb i fasen). Træk fx et stjerne-objekt ind i `Vulnerable Icon`.
6. **Livsbjælke**: lav en bjælke i Canvas. Træk dens Image ind i `Health -> Health Bar` og hele bjælke-gruppen ind i `Health Bar Root` (vises først når kampen starter).
7. **Boss-dør** (uden kode): i `On Fight Start` tilføjer du `Door.Close` og `Door.Lock`, og i `On Defeated` tilføjer du `Door.Unlock` og `Door.Open`.

Bossen kan også få et `Shield`.
