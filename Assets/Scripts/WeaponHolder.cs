using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

// Lægges på Player. Erstatter Gun + GunAim.
// Holder spillerens våben (WeaponData), ammo, sigter mod musen og skifter våben.
//   Venstre musetast = skyd / slå      R = reload
//   Musehjul eller 1, 2, 3 = skift våben
public class WeaponHolder : MonoBehaviour
{
    // Et våben spilleren bærer + hvor meget ammo der er i det lige nu
    public class WeaponSlot
    {
        public WeaponData data;
        public int magazine;
        public int reserve;
    }

    [Header("Våben")]
    public List<WeaponData> startWeapons = new List<WeaponData>(); // Våben man starter med
    public int maxSlots = 3;                                       // Hvor mange våben man kan bære

    [Header("Visning (må gerne være tomme)")]
    public Transform weaponPivot;           // Objekt (child af Player) der drejer rundt om spilleren
    public SpriteRenderer weaponRenderer;   // Skifter sprite når man skifter våben (findes på pivot hvis tom)
    public Transform muzzle;                // Hvor kuglerne starter (child af pivot). Hvis tom bruges pivot
    public float distanceFromPlayer = 0.7f;

    [Header("UI (må gerne være tom)")]
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI weaponNameText;
    public Image weaponIcon;

    [Header("Events")]
    public UnityEvent onShoot;
    public UnityEvent onWeaponChanged;

    public WeaponData CurrentWeapon => CurrentSlot != null ? CurrentSlot.data : null;

    private WeaponSlot CurrentSlot => (currentIndex >= 0 && currentIndex < slots.Count) ? slots[currentIndex] : null;

    private readonly List<WeaponSlot> slots = new List<WeaponSlot>();
    private int currentIndex = -1;

    private Camera cam;
    private Health health;
    private Vector3 pivotStartScale;
    private Vector2 aimDirection = Vector2.right;
    private float aimDistance = 10f; // Afstand til musen - granater lander der

    private float shootTimer = 0f;
    private bool isReloading = false;
    private float reloadTimer = 0f;
    private float swingTimer = 0f;

    void Start()
    {
        cam = Camera.main;
        health = GetComponent<Health>();

        if (weaponPivot != null)
        {
            pivotStartScale = weaponPivot.localScale;

            if (weaponRenderer == null)
            {
                weaponRenderer = weaponPivot.GetComponentInChildren<SpriteRenderer>();
            }
        }

        foreach (WeaponData weapon in startWeapons)
        {
            if (weapon != null && slots.Count < maxSlots)
            {
                slots.Add(NewSlot(weapon, weapon.startReserveAmmo));
            }
        }

        if (slots.Count > 0)
        {
            currentIndex = 0;
        }

        RefreshWeapon();
    }

    void Update()
    {
        if (health != null && health.IsDead) return;

        if (shootTimer > 0f) shootTimer -= Time.deltaTime;
        if (swingTimer > 0f) swingTimer -= Time.deltaTime;

        UpdateAim();
        HandleSwitching();

        WeaponSlot slot = CurrentSlot;
        if (slot == null) return;

        if (isReloading)
        {
            reloadTimer -= Time.deltaTime;
            if (reloadTimer <= 0f) FinishReload(slot);
            return;
        }

        if (slot.data.isMelee)
        {
            HandleMelee(slot);
        }
        else
        {
            HandleRanged(slot);
        }
    }

    // ---------- Sigte ----------

    void UpdateAim()
    {
        if (cam != null && Mouse.current != null)
        {
            Vector3 mousePos = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 toMouse = (Vector2)mousePos - (Vector2)transform.position;
            if (toMouse.sqrMagnitude > 0.0001f)
            {
                aimDirection = toMouse.normalized;
                aimDistance = toMouse.magnitude;
            }
        }

        if (weaponPivot == null) return;

        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

        // Sving-animation for nærkamp: våbnet svinger henover sigtet
        float swingOffset = 0f;
        WeaponData weapon = CurrentWeapon;
        if (weapon != null && weapon.isMelee && swingTimer > 0f)
        {
            float progress = 1f - swingTimer / Mathf.Max(weapon.swingTime, 0.01f);
            float side = aimDirection.x < 0 ? -1f : 1f;
            swingOffset = Mathf.Lerp(weapon.meleeArc * 0.5f, -weapon.meleeArc * 0.5f, progress) * side;
        }

        weaponPivot.position = transform.position + (Vector3)(aimDirection * distanceFromPlayer);
        weaponPivot.rotation = Quaternion.Euler(0f, 0f, angle + swingOffset);

        // Vend våbnet på venstre side, så det ikke står på hovedet
        Vector3 scale = pivotStartScale;
        scale.y = aimDirection.x < 0 ? -Mathf.Abs(pivotStartScale.y) : Mathf.Abs(pivotStartScale.y);
        weaponPivot.localScale = scale;
    }

    float AimAngle()
    {
        return Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
    }

    // ---------- Skyd og slå ----------

    void HandleRanged(WeaponSlot slot)
    {
        // Auto reload når magasinet er tomt
        if (slot.magazine <= 0 && slot.reserve > 0)
        {
            StartReload();
            return;
        }

        // Tryk R for at reloade - kun hvis magasinet ikke er fuldt og der er ammo tilbage
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (slot.magazine < slot.data.magazineSize && slot.reserve > 0)
            {
                StartReload();
                return;
            }
        }

        if (slot.magazine > 0 && shootTimer <= 0f && WantsToShoot(slot.data))
        {
            Vector2 origin = muzzle != null ? (Vector2)muzzle.position : (Vector2)transform.position + aimDirection * distanceFromPlayer;
            slot.data.Shoot(origin, AimAngle(), gameObject, 0f, aimDistance);

            slot.magazine--;
            shootTimer = slot.data.shootInterval;
            onShoot.Invoke();
            UpdateUI();
        }
    }

    void HandleMelee(WeaponSlot slot)
    {
        if (shootTimer > 0f || !WantsToShoot(slot.data)) return;

        slot.data.MeleeAttack(transform.position, AimAngle(), gameObject);

        shootTimer = slot.data.shootInterval;
        swingTimer = slot.data.swingTime;
        onShoot.Invoke();
    }

    bool WantsToShoot(WeaponData weapon)
    {
        if (Mouse.current == null) return false;

        return weapon.automatic ? Mouse.current.leftButton.isPressed : Mouse.current.leftButton.wasPressedThisFrame;
    }

    void StartReload()
    {
        isReloading = true;
        reloadTimer = CurrentWeapon.reloadTime;
        UpdateUI();
    }

    void FinishReload(WeaponSlot slot)
    {
        // Tag så mange skud som muligt fra reserven
        int needed = slot.data.magazineSize - slot.magazine;
        int amount = Mathf.Min(needed, slot.reserve);

        slot.magazine += amount;
        slot.reserve -= amount;

        isReloading = false;
        UpdateUI();
    }

    // ---------- Skift våben ----------

    void HandleSwitching()
    {
        if (slots.Count < 2) return;

        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll > 0f) EquipSlot((currentIndex + 1) % slots.Count);
            else if (scroll < 0f) EquipSlot((currentIndex - 1 + slots.Count) % slots.Count);
        }

        if (Keyboard.current == null) return;

        for (int i = 0; i < slots.Count && i < 9; i++)
        {
            Key key = (Key)((int)Key.Digit1 + i);
            if (Keyboard.current[key].wasPressedThisFrame)
            {
                EquipSlot(i);
            }
        }
    }

    void EquipSlot(int index)
    {
        if (index == currentIndex || index < 0 || index >= slots.Count) return;

        currentIndex = index;
        isReloading = false; // Skifter man våben, afbrydes reload
        shootTimer = 0.1f;   // Lille pause før man kan skyde med det nye
        RefreshWeapon();
    }

    // ---------- Kaldes fra Pickup ----------

    // Samler et våben op. Har man det allerede, får man bare ammo.
    // Er alle pladser fyldt, erstatter det våbnet man holder.
    // reserveAmmo = 0 betyder: brug våbnets standard.
    public bool PickupWeapon(WeaponData weapon, int reserveAmmo = 0)
    {
        if (weapon == null) return false;

        WeaponSlot existing = FindSlot(weapon);
        if (existing != null)
        {
            if (weapon.isMelee) return false;
            return AddAmmo(reserveAmmo > 0 ? reserveAmmo : weapon.magazineSize, weapon);
        }

        int reserve = Mathf.Min(reserveAmmo > 0 ? reserveAmmo : weapon.startReserveAmmo, weapon.maxReserveAmmo);
        WeaponSlot slot = NewSlot(weapon, reserve);

        if (slots.Count < maxSlots)
        {
            slots.Add(slot);
            currentIndex = slots.Count - 1;
        }
        else
        {
            slots[currentIndex] = slot;
        }

        isReloading = false;
        shootTimer = 0.1f;
        RefreshWeapon();
        return true;
    }

    // Giver ammo til et bestemt våben - eller det man holder (eller første skydevåben) hvis forWeapon er tom.
    // Returnerer false hvis der ikke var plads eller intet våben at give ammo til.
    public bool AddAmmo(int amount, WeaponData forWeapon = null)
    {
        WeaponSlot slot = forWeapon != null ? FindSlot(forWeapon) : FindRangedSlot();

        if (slot == null || slot.data.isMelee) return false;
        if (slot.reserve >= slot.data.maxReserveAmmo) return false;

        slot.reserve = Mathf.Min(slot.reserve + amount, slot.data.maxReserveAmmo);
        UpdateUI();
        return true;
    }

    WeaponSlot FindSlot(WeaponData weapon)
    {
        foreach (WeaponSlot slot in slots)
        {
            if (slot.data == weapon) return slot;
        }

        return null;
    }

    WeaponSlot FindRangedSlot()
    {
        if (CurrentSlot != null && !CurrentSlot.data.isMelee) return CurrentSlot;

        foreach (WeaponSlot slot in slots)
        {
            if (!slot.data.isMelee) return slot;
        }

        return null;
    }

    WeaponSlot NewSlot(WeaponData weapon, int reserve)
    {
        return new WeaponSlot
        {
            data = weapon,
            magazine = weapon.magazineSize,
            reserve = Mathf.Min(reserve, weapon.maxReserveAmmo)
        };
    }

    // ---------- Visning ----------

    void RefreshWeapon()
    {
        WeaponData weapon = CurrentWeapon;

        if (weaponRenderer != null)
        {
            weaponRenderer.sprite = weapon != null ? weapon.weaponSprite : null;
            weaponRenderer.enabled = weapon != null && weapon.weaponSprite != null;
        }

        if (weaponNameText != null)
        {
            weaponNameText.text = weapon != null ? weapon.weaponName : "";
        }

        if (weaponIcon != null)
        {
            weaponIcon.sprite = weapon != null ? weapon.weaponSprite : null;
            weaponIcon.enabled = weapon != null && weapon.weaponSprite != null;
        }

        UpdateUI();
        onWeaponChanged.Invoke();
    }

    void UpdateUI()
    {
        if (ammoText == null) return;

        WeaponSlot slot = CurrentSlot;
        if (slot == null || slot.data.isMelee)
        {
            ammoText.text = "";
        }
        else if (isReloading)
        {
            ammoText.text = "Reloading...";
        }
        else
        {
            ammoText.text = slot.magazine + " / " + slot.reserve;
        }
    }
}
