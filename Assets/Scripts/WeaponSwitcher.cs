using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// Lægges på Player.
// Træk våbnene (children af Player med Gun-scriptet, fx "Hand" og "Shotgun") ind i "Weapons".
// Det første våben har man fra start. De andre skal samles op med en WeaponPickup.
// Skift våben med 1, 2, 3, 4 eller musehjulet.
public class WeaponSwitcher : MonoBehaviour
{
    [Header("Våben")]
    public GameObject[] weapons;
    public bool startWithAllWeapons = false; // Godt til at teste

    [Header("UI (må gerne være tomme)")]
    public TextMeshProUGUI ammoText;         // Bruges af det våben man holder
    public TextMeshProUGUI weaponNameText;   // Viser navnet på våben-objektet

    private bool[] hasWeapon;
    private int currentIndex = 0;

    void Start()
    {
        hasWeapon = new bool[weapons.Length];
        for (int i = 0; i < weapons.Length; i++)
        {
            hasWeapon[i] = startWithAllWeapons || i == 0;
        }

        SelectWeapon(0);
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.digit1Key.wasPressedThisFrame) SelectWeapon(0);
            if (kb.digit2Key.wasPressedThisFrame) SelectWeapon(1);
            if (kb.digit3Key.wasPressedThisFrame) SelectWeapon(2);
            if (kb.digit4Key.wasPressedThisFrame) SelectWeapon(3);
        }

        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll > 0f) NextWeapon(1);
            if (scroll < 0f) NextWeapon(-1);
        }
    }

    // Går frem (1) eller tilbage (-1) til næste våben man har
    void NextWeapon(int step)
    {
        int index = currentIndex;
        for (int i = 0; i < weapons.Length; i++)
        {
            index = (index + step + weapons.Length) % weapons.Length;
            if (hasWeapon[index])
            {
                SelectWeapon(index);
                return;
            }
        }
    }

    void SelectWeapon(int index)
    {
        if (index >= weapons.Length || !hasWeapon[index] || weapons[index] == null) return;

        currentIndex = index;

        // Giv våbnet ammo-teksten, så alle våben bruger samme tekst
        Gun gun = weapons[index].GetComponent<Gun>();
        if (gun != null && ammoText != null)
        {
            gun.ammoText = ammoText;
        }

        // Kun det valgte våben er tændt
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null) weapons[i].SetActive(i == index);
        }

        if (weaponNameText != null)
        {
            weaponNameText.text = weapons[index].name;
        }
    }

    // Kaldes af WeaponPickup. Returnerer false hvis der ikke findes et våben med det navn.
    public bool GiveWeapon(string weaponName)
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null && weapons[i].name == weaponName)
            {
                hasWeapon[i] = true;
                SelectWeapon(i);
                return true;
            }
        }

        Debug.LogWarning("WeaponSwitcher: der er intet våben under Player der hedder '" + weaponName + "'");
        return false;
    }
}
