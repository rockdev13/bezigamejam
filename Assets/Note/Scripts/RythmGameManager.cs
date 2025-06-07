using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class RythmGameManager : MonoBehaviour {
    [Header("Game Settings")]
    public AnimationCurveAsset curveAsset;
    public AudioSource musicSource;
    public float noteSpeed = 5f;
    public float noteLifetime = 10f;
    public float hitZoneY = -3f;
    public float perfectHitThreshold = 0.1f;
    public float goodHitThreshold = 0.3f;

    [Header("Lane Configuration")]
    public Transform[] laneTargets = new Transform[ 4 ];
    public Transform[] laneStartingPoints = new Transform[ 4 ];

    public GameObject notePrefab;
    public float[] laneFrequencyRanges = new float[] { 130f, 262f, 523f, 1047f };

    [Header("Input Actions")]
    public InputActionReference[] laneInputs = new InputActionReference[ 4 ];

    [Header("Curve Sampling")]
    public float curveThreshold = 0.1f;
    public float sampleRate = 30f;
    public float minLaneInterval = 0.15f;

    [Header("Duration Settings")]
    public float durationSampleWindow = 0.1f; // How far ahead to look for duration
    public float minNoteDuration = 0.2f; // Minimum duration to create a duration note

    [Header("Score System")]
    public int perfectScore = 100;
    public int goodScore = 50;
    public int missScore = 0;

    private AnimationCurve noteSpawnCurve;
    private List<FallingNote> activeNotes = new List<FallingNote>();

    // Lane timing tracking
    private float[] lastLaneSpawnTime = new float[ 4 ];

    private float gameStartTime;
    private float noteSpawnTime;
    private float musicTime;
    private float lastSampleTime;
    private float dropTime;

    private bool isPlaying;
    private bool musicStarted;
    private int totalScore;

    public event System.Action<int, string> OnScoreUpdate;
    public event System.Action<int> OnNoteHit;
    public event System.Action OnNoteMiss;

    void Start() {
        SetupInputActions();
        LoadAnimationCurve();
        CalculateDropTime();
    }

    void LoadAnimationCurve() {
        if (curveAsset != null && curveAsset.curve != null) {
            noteSpawnCurve = curveAsset.curve;
            Debug.Log($"Loaded animation curve with {noteSpawnCurve.keys.Length} keyframes");
            Debug.Log($"Curve duration: {curveAsset.GetCurveDuration():F2}s");
            Debug.Log($"Estimated notes: {curveAsset.CountSpikes(curveThreshold)}");
        } else {
            Debug.LogWarning("No curve asset assigned or curve is null! Please assign a curve :D");
        }
    }

    void CalculateDropTime() {
        if (laneStartingPoints.Length > 0 && laneStartingPoints[ 0 ] != null) {
            float spawnY = laneStartingPoints[ 0 ].position.y;
            float distance = spawnY - hitZoneY;
            dropTime = distance / noteSpeed;
        } else {
            Debug.LogWarning("No lane starting points configured. Using default drop time of 2 seconds.");
            dropTime = 2f;
        }
    }

    void SetupInputActions() {
        for (int i = 0; i < laneInputs.Length; i++) {
            if (laneInputs[ i ] != null) {
                int laneIndex = i;
                laneInputs[ i ].action.performed += (context) => OnLaneInput(laneIndex);
                laneInputs[ i ].action.Enable();
            }
        }
    }

    void Update() {
        if (!isPlaying) return;

        float currentRealTime = Time.time - gameStartTime;
        noteSpawnTime = currentRealTime;

        if (currentRealTime >= dropTime) {
            if (!musicStarted) {
                StartMusic();
            }
            musicTime = currentRealTime - dropTime;
        }

        if (noteSpawnCurve != null) {
            SampleCurveForNotes();
        }

        UpdateFallingNotes();
        CleanupMissedNotes();
    }

    public void StartGame() {
        if (noteSpawnCurve == null) {
            Debug.LogError("No animation curve loaded! Please assign a curve asset.");
            return;
        }

        if (musicSource.clip == null) {
            Debug.LogError("No audio clip assigned!");
            return;
        }

        gameStartTime = Time.time;
        noteSpawnTime = 0f;
        musicTime = 0f;
        lastSampleTime = 0f;
        totalScore = 0;

        for (int i = 0; i < lastLaneSpawnTime.Length; i++) {
            lastLaneSpawnTime[ i ] = -minLaneInterval;
        }

        isPlaying = true;
        musicStarted = false;
        activeNotes.Clear();
    }

    void StartMusic() {
        musicStarted = true;
        musicSource.Play();
    }

    public void StopGame() {
        isPlaying = false;
        musicStarted = false;
        musicSource.Stop();

        foreach (var note in activeNotes) {
            if (note != null && note.gameObject != null)
                Destroy(note.gameObject);
        }
        activeNotes.Clear();
    }

    void SampleCurveForNotes() {
        float deltaTime = 1f / sampleRate;

        while (lastSampleTime + deltaTime <= noteSpawnTime) {
            lastSampleTime += deltaTime;
            float curveValue = noteSpawnCurve.Evaluate(lastSampleTime);

            if (curveValue > curveThreshold) {
                float targetHitTime = lastSampleTime;
                float duration = CalculateNoteDuration(lastSampleTime);
                SpawnNoteFromCurveValue(curveValue, targetHitTime, duration);
            }
        }
    }

    float CalculateNoteDuration(float startTime) {
        float duration = 0f;
        float checkTime = startTime;
        float deltaTime = 1f / sampleRate;

        // Look ahead to see how long the curve stays above threshold
        while (checkTime <= startTime + durationSampleWindow) {
            checkTime += deltaTime;
            if (checkTime >= noteSpawnCurve.keys[ noteSpawnCurve.keys.Length - 1 ].time) break;

            float checkValue = noteSpawnCurve.Evaluate(checkTime);
            if (checkValue > curveThreshold) {
                duration = checkTime - startTime;
            } else {
                break;
            }
        }

        // Only return duration if it meets minimum threshold
        return duration >= minNoteDuration ? duration : 0f;
    }

    void SpawnNoteFromCurveValue(float curveValue, float targetHitTime, float duration) {
        float frequency = MapCurveValueToFrequency(curveValue);
        int laneIndex = GetLaneFromFrequency(frequency);

        if (laneIndex >= 0 && laneIndex < laneTargets.Length) {
            SpawnNote(laneIndex, targetHitTime, duration);
        }
    }

    float MapCurveValueToFrequency(float curveValue) {
        float minFreq = laneFrequencyRanges[ 0 ];
        float maxFreq = laneFrequencyRanges[ laneFrequencyRanges.Length - 1 ];
        return Mathf.Lerp(minFreq, maxFreq, curveValue);
    }

    int GetLaneFromFrequency(float frequency) {
        for (int i = 0; i < laneFrequencyRanges.Length; i++) {
            if (i == laneFrequencyRanges.Length - 1) {
                return i;
            }

            float currentFreq = laneFrequencyRanges[ i ];
            float nextFreq = laneFrequencyRanges[ i + 1 ];
            float midpoint = (currentFreq + nextFreq) / 2f;

            if (frequency < midpoint) {
                return i;
            }
        }

        return 0;
    }

    void SpawnNote(int laneIndex, float targetHitTime, float duration = 0f) {
        if (laneTargets[ laneIndex ] == null) return;

        float timeSinceLastSpawn = targetHitTime - lastLaneSpawnTime[ laneIndex ];
        if (timeSinceLastSpawn < minLaneInterval) {
            return;
        }

        Vector3 spawnPosition = laneStartingPoints[ laneIndex ].position;
        GameObject noteObject = Instantiate(notePrefab, spawnPosition, Quaternion.identity);

        FallingNote note = noteObject.GetComponent<FallingNote>();
        if (note == null) {
            note = noteObject.AddComponent<FallingNote>();
        }

        note.Initialize(laneIndex, targetHitTime, noteSpeed, hitZoneY, duration);
        activeNotes.Add(note);

        lastLaneSpawnTime[ laneIndex ] = targetHitTime;
    }

    void UpdateFallingNotes() {
        foreach (var note in activeNotes) {
            if (note != null) {
                note.UpdateNote(noteSpawnTime);
            }
        }
    }

    void CleanupMissedNotes() {
        for (int i = activeNotes.Count - 1; i >= 0; i--) {
            if (activeNotes[ i ] == null || activeNotes[ i ].ShouldDestroy()) {
                if (activeNotes[ i ] != null && activeNotes[ i ].gameObject != null) {
                    Destroy(activeNotes[ i ].gameObject);
                }
                activeNotes.RemoveAt(i);
            }
        }
    }

    void OnLaneInput(int laneIndex) {
        if (!isPlaying) return;

        FallingNote closestNote = null;
        float closestDistance = float.MaxValue;

        foreach (var note in activeNotes) {
            if (note != null && note.LaneIndex == laneIndex && !note.IsHit) {
                if (note.Duration > 0f) {
                    // For duration notes, check if they're in the hit zone
                    if (note.IsInHitZone()) {
                        float accuracy = note.GetHitAccuracy();
                        if (accuracy < closestDistance) {
                            closestDistance = accuracy;
                            closestNote = note;
                        }
                    }
                } else {
                    // For point notes, use distance
                    float distance = Mathf.Abs(note.transform.position.y - hitZoneY);
                    if (distance < closestDistance) {
                        closestDistance = distance;
                        closestNote = note;
                    }
                }
            }
        }

        if (closestNote != null) {
            HitNote(closestNote, closestDistance);
        }
    }

    void HitNote(FallingNote note, float distance) {
        string hitType;
        int scoreGain;

        if (distance <= perfectHitThreshold) {
            hitType = "Perfect!";
            scoreGain = perfectScore;
        } else if (distance <= goodHitThreshold) {
            hitType = "Good";
            scoreGain = goodScore;
        } else {
            hitType = "Miss";
            scoreGain = missScore;
            OnNoteMiss?.Invoke();
        }

        if (scoreGain > 0) {
            totalScore += scoreGain;
            note.Hit();
            OnNoteHit?.Invoke(scoreGain);
        }

        OnScoreUpdate?.Invoke(totalScore, hitType);
    }

    public float GetMusicTime() {
        return musicStarted ? musicTime : 0f;
    }

    public float GetDropTime() {
        return dropTime;
    }

    void OnDestroy() {
        for (int i = 0; i < laneInputs.Length; i++) {
            if (laneInputs[ i ] != null) {
                laneInputs[ i ].action.Disable();
            }
        }
    }
}