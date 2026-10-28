using UnityEngine;

// En lydeffekt med tilfældig pitch, så gentagne lyde (skud, skridt, slag) ikke lyder ens.
// Sæt den som felt på et script, vælg en lyd, og skru på Min/Max Pitch (fx 0.9 og 1.1).
// Lyden afspilles på sit eget midlertidige objekt, så den spiller færdig selvom fjenden, kuglen
// eller samleobjektet bliver destrueret med det samme.
[System.Serializable]
public class SoundEffect
{
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.5f, 2f)] public float minPitch = 0.95f;    // Laveste pitch (1 = normal)
    [Range(0.5f, 2f)] public float maxPitch = 1.05f;    // Højeste pitch. Samme som Min = ingen variation

    public void Play(Vector2 position)
    {
        if (clip == null) return;

        float pitch = Random.Range(Mathf.Min(minPitch, maxPitch), Mathf.Max(minPitch, maxPitch));

        GameObject sound = new GameObject("Sfx " + clip.name);
        sound.transform.position = position;

        AudioSource source = sound.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.spatialBlend = 0f; // 2D: lige høj uanset hvor kameraet er
        source.playOnAwake = false;
        source.Play();

        Object.Destroy(sound, clip.length / Mathf.Max(pitch, 0.01f) + 0.1f);
    }
}
