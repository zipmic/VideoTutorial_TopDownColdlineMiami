using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Lægges på ét tomt GameObject i banen (fx "ScoreManager").
// Drab giver point (sæt "Score On Death" på fjendens Health). Dræber du hurtigt efter hinanden
// vokser din combo, og dermed din multiplikator. Bliver du ramt, nulstilles comboen.
// Samleobjekter (Pickup med Type = Score) giver bonuspoint uden at påvirke comboen.
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Combo")]
    public float comboTime = 3f;            // Sekunder du har til næste drab før comboen forsvinder
    public float multiplierStep = 0.5f;     // Hvor meget multiplikatoren stiger pr. drab i comboen
    public float maxMultiplier = 8f;
    public float speedBonus = 1f;           // Ekstra multiplikator (op til) for at dræbe LYNHURTIGT efter forrige drab
    public bool resetComboWhenHit = true;   // Nulstil combo når spilleren tager skade

    [Header("UI (må gerne være tom)")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public Image comboTimerBar;             // Image med Image Type = Filled - tømmes mens comboen løber ud
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI collectiblesText; // Fx "Fundet 2 / 5"

    [Header("Point-popups (må gerne være tom)")]
    public ScorePopup popupPrefab;
    public Color killPopupColor = Color.white;
    public Color bonusPopupColor = Color.yellow;

    public int Score { get; private set; }
    public int Kills { get; private set; }
    public int Combo { get; private set; }
    public int HighScore { get; private set; }
    public int CollectiblesFound { get; private set; }
    public int CollectiblesTotal { get; private set; }

    public float Multiplier => Combo <= 1 ? 1f : Mathf.Min(1f + (Combo - 1) * multiplierStep, maxMultiplier);

    private float comboTimer = 0f;
    private string highScoreKey;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Der er mere end én ScoreManager i banen - den ekstra bliver fjernet", this);
            Destroy(this);
            return;
        }

        Instance = this;
        highScoreKey = "HighScore_" + SceneManager.GetActiveScene().name;
        HighScore = PlayerPrefs.GetInt(highScoreKey, 0);
    }

    void Start()
    {
        if (resetComboWhenHit)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            Health playerHealth = playerObject != null ? playerObject.GetComponent<Health>() : null;
            if (playerHealth != null)
            {
                playerHealth.onDamaged.AddListener(ResetCombo);
            }
        }

        RefreshUI();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (Combo > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
            {
                ResetCombo();
            }
        }

        if (comboTimerBar != null)
        {
            comboTimerBar.fillAmount = Combo > 0 ? Mathf.Clamp01(comboTimer / comboTime) : 0f;
        }
    }

    // Kaldes når noget med "Score On Death" dør
    public void AddKill(int basePoints, Vector2 position)
    {
        // Hvor stor en del af combo-tiden var tilbage? 1 = dræbte med det samme efter forrige drab
        float timeLeft = Combo > 0 ? Mathf.Clamp01(comboTimer / comboTime) : 0f;

        Combo++;
        Kills++;
        comboTimer = comboTime;

        float multiplier = Multiplier + speedBonus * timeLeft;
        int points = Mathf.RoundToInt(basePoints * multiplier);

        AddScore(points);
        SpawnPopup(position, "+" + points, killPopupColor);
        RefreshUI();
    }

    // Bonuspoint (fx et samleobjekt) - påvirker ikke comboen
    public void AddBonus(int points, Vector2 position)
    {
        AddScore(points);
        SpawnPopup(position, "+" + points, bonusPopupColor);
        RefreshUI();
    }

    // Pickup med Type = Score melder sig selv, så vi kan vise "fundet x / y"
    public void RegisterCollectible()
    {
        CollectiblesTotal++;
        RefreshUI();
    }

    public void CollectibleFound(int points, Vector2 position)
    {
        CollectiblesFound++;
        AddBonus(points, position);
    }

    public void ResetCombo()
    {
        if (Combo == 0) return;

        Combo = 0;
        comboTimer = 0f;
        RefreshUI();
    }

    void AddScore(int points)
    {
        Score += points;

        if (Score > HighScore)
        {
            HighScore = Score;
            PlayerPrefs.SetInt(highScoreKey, HighScore);
        }
    }

    void SpawnPopup(Vector2 position, string text, Color color)
    {
        if (popupPrefab == null) return;

        ScorePopup popup = Instantiate(popupPrefab, position + Vector2.up * 0.5f, Quaternion.identity);
        popup.Show(text, color);
    }

    void RefreshUI()
    {
        if (scoreText != null) scoreText.text = Score.ToString();
        if (highScoreText != null) highScoreText.text = "Best: " + HighScore;

        if (comboText != null)
        {
            comboText.text = Combo > 1 ? "x" + Multiplier.ToString("0.#") + "  COMBO " + Combo : "";
        }

        if (collectiblesText != null)
        {
            collectiblesText.text = CollectiblesTotal > 0 ? "Fundet " + CollectiblesFound + " / " + CollectiblesTotal : "";
        }
    }
}
