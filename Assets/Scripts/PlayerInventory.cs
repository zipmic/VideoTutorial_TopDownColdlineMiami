using System.Collections.Generic;
using UnityEngine;

// Holder styr på de nøgler spilleren har samlet op.
// Tilføjes automatisk sammen med PlayerController.
public class PlayerInventory : MonoBehaviour
{
    private readonly HashSet<string> keys = new HashSet<string>();

    public void AddKey(string keyId)
    {
        if (string.IsNullOrEmpty(keyId)) return;

        keys.Add(keyId);
        Debug.Log("Samlede nøgle op: " + keyId);
    }

    public bool HasKey(string keyId)
    {
        return keys.Contains(keyId);
    }

    public void RemoveKey(string keyId)
    {
        keys.Remove(keyId);
    }
}
