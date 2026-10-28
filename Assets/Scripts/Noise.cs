using System;
using UnityEngine;

// Lille "lydsystem": når et våben skyder, kalder det Noise.Make().
// Fjender (EnemyAI) lytter og går hen mod lyden hvis de er tæt nok på.
public static class Noise
{
    // position, radius, hvem der lavede lyden
    public static event Action<Vector2, float, GameObject> OnNoise;

    public static void Make(Vector2 position, float radius, GameObject source)
    {
        if (radius <= 0f) return;
        OnNoise?.Invoke(position, radius, source);
    }

    // Så gamle lyttere ikke hænger fast mellem to play-sessions
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetEvent()
    {
        OnNoise = null;
    }
}
