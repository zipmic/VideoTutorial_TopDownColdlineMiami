using UnityEngine;

// Lille hjælper til lyd - lægges IKKE på noget objekt.
// Andre scripts kalder: SoundPlayer.Play(lyd, position);
// Lyden får sit eget lille objekt, så den spiller færdig selvom fjenden eller pickuppen bliver fjernet.
public static class SoundPlayer
{
    public static void Play(AudioClip clip, Vector3 position, float minPitch = 0.9f, float maxPitch = 1.1f)
    {
        if (clip == null) return;

        GameObject soundObject = new GameObject("Lyd: " + clip.name);
        soundObject.transform.position = position;

        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.pitch = Random.Range(minPitch, maxPitch); // Lidt forskellig hver gang
        source.Play();

        // Fjern objektet når lyden er færdig
        Object.Destroy(soundObject, clip.length / source.pitch + 0.1f);
    }
}
