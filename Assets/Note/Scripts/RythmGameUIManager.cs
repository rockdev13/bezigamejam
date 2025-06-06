using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RythmGameUIManager : MonoBehaviour {
    [Header("UI Elements")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI hitFeedbackText;
    public TextMeshProUGUI comboText;
    public Button startButton;
    public Button stopButton;

    [Header("References")]
    public RythmGameManager gameManager;

    private int currentCombo;
    private float feedbackTimer;

    void Start() {
        if (gameManager != null) {
            gameManager.OnScoreUpdate += UpdateScore;
            gameManager.OnNoteHit += OnNoteHit;
            gameManager.OnNoteMiss += OnNoteMiss;
        }

        if (startButton != null)
            startButton.onClick.AddListener(() => gameManager.StartGame());

        if (stopButton != null)
            stopButton.onClick.AddListener(() => gameManager.StopGame());

        UpdateScore(0, "");
    }

    void Update() {
        // Hide feedback text after delay
        if (feedbackTimer > 0) {
            feedbackTimer -= Time.deltaTime;
            if (feedbackTimer <= 0 && hitFeedbackText != null) {
                hitFeedbackText.text = "";
            }
        }
    }

    void UpdateScore(int score, string hitType) {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";

        if (hitFeedbackText != null && !string.IsNullOrEmpty(hitType)) {
            hitFeedbackText.text = hitType;
            feedbackTimer = 1f;
        }
    }

    void OnNoteHit(int scoreGain) {
        currentCombo++;
        UpdateComboDisplay();
    }

    void OnNoteMiss() {
        currentCombo = 0;
        UpdateComboDisplay();
    }

    void UpdateComboDisplay() {
        if (comboText != null) {
            if (currentCombo > 1)
                comboText.text = $"Combo: {currentCombo}";
            else
                comboText.text = "";
        }
    }

    void OnDestroy() {
        if (gameManager != null) {
            gameManager.OnScoreUpdate -= UpdateScore;
            gameManager.OnNoteHit -= OnNoteHit;
            gameManager.OnNoteMiss -= OnNoteMiss;
        }
    }
}
