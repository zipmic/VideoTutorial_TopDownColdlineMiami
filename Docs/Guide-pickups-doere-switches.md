# Guide: Pickups, nøgler, døre og switches

Alle prefabs ligger i `Assets/Prefabs/`. Træk dem ud i banen, og ret værdierne på prefaben i Inspector.

**Før du starter:** Spilleren skal have `PlayerController` (så følger `Health` og `PlayerInventory` med) og `WeaponHolder`, og have en Collider2D der **ikke** er trigger. Pickups og døre reagerer kun på spilleren.

## Pickups (generelt)

Alle pickups er en firkantet trigger med scriptet `Pickup`. Man samler dem op ved at gå ind i dem.
Typen vælges i feltet `Type`. Ellers er felterne:
- `Amount`: hvor meget den giver (liv, skud, point...).
- `Pickup Sound`: lyd + pitch (se lyd-afsnittet i den anden guide).
- `Respawn Time`: 0 = forsvinder for altid. Fx 30 = kommer tilbage efter 30 sekunder.
- `On Picked Up`: UnityEvent, fx åbn en dør, spawn en fjende eller tænd en lyd.
- Sprite skiftes på SpriteRenderer. Collider-størrelsen er samtidig opsamlings-radius.

### Health pickup (`Pickup_Health`)
Giver `Amount` liv (standard 1). Står `Only When Needed` til (standard), bliver den liggende, hvis spilleren har fuldt liv, og kan samles op senere, når man mangler liv.
Vil du have en stor medpakke: lav en kopi og sæt `Amount` til 3.

### Ammo pickup (`Pickup_Ammo`)
Giver `Amount` skud (standard 12) til **reserven** på det våben man holder.
- Holder man en kniv eller et granatvåben, går den til første skydevåben man har.
- Vil du kun give ammo til et bestemt våben (fx haglgeværet), så træk det våbens WeaponData ind i `Weapon`.
- Er reserven fuld (`Max Reserve Ammo` i WeaponData), bliver pickuppen liggende.

### Weapon pickup (`Pickup_Weapon_Shotgun`, `Pickup_Weapon_Knife`)
Giver spilleren et nyt våben.
1. Træk et WeaponData-asset (fra `Assets/Prefabs/Weapons/`) ind i `Weapon`.
2. `Amount` = ammo i reserven. 0 betyder våbnets egen `Start Reserve Ammo`.
3. Har man allerede våbnet, giver den i stedet ammo (kniv: ingenting, og den ligger der stadig).
4. Man kan bære `Max Slots` våben (standard 3). Er alle pladser fyldt, **erstatter det nye våben det man holder**.
5. Er `Weapon Sprite` sat i WeaponData, viser pickuppen den. Ellers bruges pickuppens egen sprite.
Våbenskift: tallene 1-3 eller musehjulet. R genlader.

### Coin og Gold (`Pickup_Coin` 250 point, `Pickup_Gold` 1000 point)
Er `Type = Score`. Giver `Amount` point.
- Kræver en `ScoreManager` i banen (tomt GameObject). Uden den får du en advarsel i Console.
- Pointene er faste: de lægges direkte oveni scoren, ganges ikke med combo-multiplikatoren og starter eller forlænger heller ikke en combo. Kun drab giver combo.
- ScoreManager tæller dem automatisk. Træk en TextMeshPro-tekst ind i `Collectibles Text`, så viser den "Fundet 2 / 5".
- Lav egne: kopier prefaben, skift sprite, lyd og `Amount` (fx "Energidrik" 250, "Guldbarre" 1000).

### Nøgle (`Pickup_Key_Red`, `Pickup_Key_Blue`)
Er `Type = Key`. Nøglen får en tekst i `Key Id` (fx `red`, `blue`, `kontor`).
Nøglen ligger i spillerens `PlayerInventory`, og en dør åbnes af en nøgle med **samme** Key Id.
Lav en ny farve: kopier prefaben, skriv et nyt `Key Id` og skift farven på sprite.

## Døre

Alle døre er et rektangel (2 x 0,3) på layer `Walls`, med scriptet `Door`. De glider 2 enheder til siden, når de åbner (`Slide Offset`).
- Drej døren i Scene-vinduet, så den passer til væggen. Skal den glide op/ned, så sæt `Slide Offset` til fx (0, 2).
- Sæt `Style` til `Swing` for en dør der svinger (sprite-pivot ved hængslet) eller `Disappear` for en der bare forsvinder.
- Åben dør har ingen collider, så man kan gå igennem og fjender kan se igennem.

### Almindelig dør (`Door_Interact`)
Man står tæt på (`Interact Radius` 1,5) og trykker **E** (`Interact Key`). Tryk igen for at lukke den.

### Automatisk dør (`Door_Automatic`)
`Opens By = Automatic`: åbner af sig selv, når spilleren kommer tæt på.
- `Enemies Can Open`: fjender åbner den også (standard til).
- Vil du have, at den lukker igen: slå `Close When Left` til og sæt `Close Delay` (sekunder).
- Vil du have en automatisk dør, der kræver nøgle: sæt `Lock Mode = Needs Key` og `Required Key Id` på den. Fjender kan så ikke åbne den.

### Dør med nøgle (`Door_Key_Red`, `Door_Key_Blue`)
1. Læg et `Pickup_Key_Red` et sted i banen.
2. Døren har `Lock Mode = Needs Key` og `Required Key Id = red`. Samme tekst som nøglens `Key Id`.
3. Spilleren går hen til døren og trykker E. Har man nøglen, åbner den. Ellers spilles `Locked Sound`, og `On Locked Attempt` kaldes (fx vis en tekst "Kræver rød nøgle").
4. `Consume Key`: nøglen bruges op ved åbning (standard til). Slå fra, hvis én nøgle skal kunne åbne flere døre.
5. Fjender kan ikke åbne låste døre.

### Dør der åbnes af en switch (`Door_Switch`)
`Lock Mode = Locked Until Triggered`. Spilleren kan ikke åbne den selv. Den åbnes kun af en Switch eller et event (se nedenfor).
Eksempel boss-dør: `Boss -> On Fight Start`: `Door.Close` + `Door.Lock`. `On Defeated`: `Door.Unlock` + `Door.Open`.

## Switches

- `Switch_Button`: stå tæt på og tryk E. Tænd/sluk hver gang.
- `Switch_Button_OneTime`: kan kun bruges én gang.
- `Switch_PressurePlate`: aktiv, så længe spilleren står på den.
- `Switch_PressurePlate_Stay`: bliver nede, også efter man går af.

**Sådan kobler du en switch til en dør (uden kode):**
1. Læg en switch og en dør (fx `Door_Switch`) i scenen.
2. Vælg switchen. Under `On Activated` klikker du **+**.
3. Træk **døren fra Hierarchy** (ikke prefaben i Project) ind i objekt-feltet.
4. I dropdown vælger du `Door -> Open`.
5. Til knappen/pladen med `On Deactivated` vælger du `Door -> Close`, hvis døren skal lukke igen, når man går af pladen.
Man kan tilføje flere handlinger på samme switch: åbn flere døre, spawn fjender, tænd en alarm (`Enemy` osv.).
Vil fjender også kunne trykke på en trykplade: slå `Enemies Activate` til.

## Hurtig opskrift: lille bane med nøgle og boss-dør

1. Sæt en `Pickup_Key_Red` i et hjørne og en `Door_Key_Red` i en væg.
2. Sæt `Pickup_Coin` på en afsides plads. Tilføj en `ScoreManager`.
3. Sæt en `Pickup_Health` før bossen og `Pickup_Ammo` ved våbnet.
4. Sæt `Door_Switch` foran bossen og en `Switch_PressurePlate` i rummet før. Træk døren ind i pladens `On Activated -> Door.Open`.
