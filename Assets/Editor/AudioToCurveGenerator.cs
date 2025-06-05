using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System;

public class EnhancedAudioToCurveGenerator : EditorWindow {
    [Header("Audio Analysis Settings")]
    public AudioClip audioClip;
    public int fftSize = 1024;
    public float overlapFactor = 0.75f;
    public FFTWindow windowType = FFTWindow.BlackmanHarris;

    [Header("Peak Detection Settings")]
    public float peakThreshold = 0.01f;
    public float minPeakDistance = 3f; // Minimum distance between peaks in frequency bins
    public int maxPeaksPerFrame = 10;
    public float minFrequency = 80f;    // Low C (C2)
    public float maxFrequency = 2000f;  // High C (C7)

    [Header("Frequency Tracking Settings")]
    public float frequencyTolerance = 0.05f; // 5% tolerance for frequency matching
    public int minTrackLength = 3; // Minimum frames to consider a valid track
    public float trackDecayTime = 0.2f; // Time before a track is considered ended

    [Header("Curve Generation Settings")]
    public float spikeDuration = 0.1f;
    public float maxSpikeHeight = 1f;
    public bool logarithmicPitchMapping = true;
    public bool combineOverlappingSpikes = true;

    [Header("Generated Curve")]
    public AnimationCurve generatedCurve;

    private UnityEngine.Vector2 scrollPosition;
    private bool isAnalyzing = false;
    private string analysisStatus = "";
    private List<FrequencyTrack> frequencyTracks = new List<FrequencyTrack>();
    private AudioSource tempAudioSource;

    [MenuItem("Window/Audio Analysis/Enhanced Audio to Curve Generator")]
    public static void ShowWindow() {
        GetWindow<EnhancedAudioToCurveGenerator>("Enhanced Audio to Curve Generator");
    }

    void OnGUI() {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Enhanced Audio to Animation Curve Generator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Audio Input Section
        GUILayout.Label("Audio Input", EditorStyles.boldLabel);
        audioClip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip", audioClip, typeof(AudioClip), false);

        GUILayout.Space(10);

        // FFT Analysis Settings Section
        GUILayout.Label("FFT Analysis Settings", EditorStyles.boldLabel);
        fftSize = EditorGUILayout.IntPopup("FFT Size", fftSize,
            new string[] { "256", "512", "1024", "2048", "4096" },
            new int[] { 256, 512, 1024, 2048, 4096 });
        overlapFactor = EditorGUILayout.Slider("Window Overlap", overlapFactor, 0f, 0.9f);
        windowType = (FFTWindow)EditorGUILayout.EnumPopup("Window Type", windowType);

        GUILayout.Space(10);

        // Peak Detection Settings Section
        GUILayout.Label("Peak Detection Settings", EditorStyles.boldLabel);
        peakThreshold = EditorGUILayout.Slider("Peak Threshold", peakThreshold, 0.001f, 0.1f);
        minPeakDistance = EditorGUILayout.Slider("Min Peak Distance (bins)", minPeakDistance, 1f, 10f);
        maxPeaksPerFrame = EditorGUILayout.IntSlider("Max Peaks Per Frame", maxPeaksPerFrame, 1, 20);
        minFrequency = EditorGUILayout.FloatField("Min Frequency (Hz)", minFrequency);
        maxFrequency = EditorGUILayout.FloatField("Max Frequency (Hz)", maxFrequency);

        GUILayout.Space(10);

        // Frequency Tracking Settings Section
        GUILayout.Label("Frequency Tracking Settings", EditorStyles.boldLabel);
        frequencyTolerance = EditorGUILayout.Slider("Frequency Tolerance", frequencyTolerance, 0.01f, 0.2f);
        minTrackLength = EditorGUILayout.IntSlider("Min Track Length", minTrackLength, 1, 10);
        trackDecayTime = EditorGUILayout.Slider("Track Decay Time (s)", trackDecayTime, 0.05f, 1f);

        GUILayout.Space(10);

        // Curve Generation Settings Section
        GUILayout.Label("Curve Generation Settings", EditorStyles.boldLabel);
        spikeDuration = EditorGUILayout.Slider("Spike Duration (s)", spikeDuration, 0.01f, 1f);
        maxSpikeHeight = EditorGUILayout.FloatField("Max Spike Height", maxSpikeHeight);
        logarithmicPitchMapping = EditorGUILayout.Toggle("Logarithmic Pitch Mapping", logarithmicPitchMapping);
        combineOverlappingSpikes = EditorGUILayout.Toggle("Combine Overlapping Spikes", combineOverlappingSpikes);

        GUILayout.Space(20);

        // Generate Button
        GUI.enabled = audioClip != null && !isAnalyzing;
        if (GUILayout.Button("Generate Animation Curve", GUILayout.Height(30))) {
            GenerateAnimationCurve();
        }
        GUI.enabled = true;

        GUILayout.Space(10);

        // Analysis Status
        if (!string.IsNullOrEmpty(analysisStatus)) {
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.HelpBox(analysisStatus, MessageType.Info);
        }

        GUILayout.Space(10);

        // Generated Curve Section
        if (generatedCurve != null) {
            GUILayout.Label("Generated Curve", EditorStyles.boldLabel);
            generatedCurve = EditorGUILayout.CurveField("Animation Curve", generatedCurve);

            GUILayout.Space(10);

            if (GUILayout.Button("Save Curve as Asset")) {
                SaveCurveAsAsset();
            }
        }

        EditorGUILayout.EndScrollView();
    }

    void GenerateAnimationCurve() {
        if (audioClip == null) {
            analysisStatus = "Please assign an audio clip.";
            return;
        }

        isAnalyzing = true;
        analysisStatus = "Analyzing audio with FFT...";
        frequencyTracks.Clear();

        try {
            // Create temporary GameObject with AudioSource for analysis
            GameObject tempGO = new GameObject("TempAudioAnalysis");
            tempAudioSource = tempGO.AddComponent<AudioSource>();
            tempAudioSource.clip = audioClip;
            tempAudioSource.loop = false;
            tempAudioSource.volume = 0f; // Mute during analysis

            // Perform FFT analysis
            List<FrequencyPeak> allPeaks = PerformFFTAnalysis();

            // Track frequencies over time
            List<NoteEvent> noteEvents = TrackFrequenciesOverTime(allPeaks);

            // Generate animation curve from note events
            generatedCurve = CreateAnimationCurveFromNotes(noteEvents, audioClip.length);

            analysisStatus = $"Analysis complete! Found {noteEvents.Count} note events from {allPeaks.Count} total peaks.";

            // Clean up
            DestroyImmediate(tempGO);
        }
        catch (System.Exception e) {
            analysisStatus = $"Error during analysis: {e.Message}";
            Debug.LogError($"Audio analysis error: {e}");

            if (tempAudioSource != null && tempAudioSource.gameObject != null)
                DestroyImmediate(tempAudioSource.gameObject);
        }
        finally {
            isAnalyzing = false;
        }
    }

    List<FrequencyPeak> PerformFFTAnalysis() {
        List<FrequencyPeak> allPeaks = new List<FrequencyPeak>();

        // Calculate analysis parameters
        int hopSize = Mathf.RoundToInt(fftSize * (1f - overlapFactor));
        float frameTime = (float)hopSize / audioClip.frequency;
        int totalFrames = Mathf.CeilToInt((audioClip.length * audioClip.frequency) / hopSize);

        // Get raw audio data
        float[] audioData = new float[ audioClip.samples * audioClip.channels ];
        audioClip.GetData(audioData, 0);

        // Convert to mono if needed
        if (audioClip.channels == 2) {
            audioData = ConvertToMono(audioData);
        }

        // Prepare spectrum array
        float[] spectrum = new float[ fftSize ];

        // Analyze each frame
        for (int frame = 0; frame < totalFrames; frame++) {
            float currentTime = frame * frameTime;
            int startSample = frame * hopSize;

            if (startSample + fftSize > audioData.Length)
                break;

            // Extract windowed segment
            float[] segment = new float[ fftSize ];
            Array.Copy(audioData, startSample, segment, 0, fftSize);

            // Apply window function
            ApplyWindowFunction(segment, windowType);

            // Perform FFT using Unity's built-in method
            // Note: We'll use a workaround since Unity's GetSpectrumData requires playing audio
            Complex[] complexSpectrum = PerformFFT(segment);

            // Convert complex spectrum to magnitude spectrum
            for (int i = 0; i < fftSize / 2; i++) {
                spectrum[ i ] = (float)complexSpectrum[ i ].Magnitude;
            }

            // Find peaks in this frame
            List<FrequencyPeak> framePeaks = FindPeaksInSpectrum(spectrum, currentTime, audioClip.frequency);
            allPeaks.AddRange(framePeaks);

            // Update progress
            if (frame % 100 == 0) {
                analysisStatus = $"Analyzing frame {frame}/{totalFrames}...";
                EditorUtility.DisplayProgressBar("Audio Analysis", analysisStatus, (float)frame / totalFrames);
            }
        }

        EditorUtility.ClearProgressBar();
        return allPeaks;
    }

    Complex[] PerformFFT(float[] data) {
        int n = data.Length;
        Complex[] x = new Complex[ n ];

        // Convert to complex numbers
        for (int i = 0; i < n; i++) {
            x[ i ] = new Complex(data[ i ], 0);
        }

        // Perform FFT using Cooley-Tukey algorithm
        return FFT(x);
    }

    Complex[] FFT(Complex[] x) {
        int n = x.Length;
        if (n <= 1) return x;

        // Divide
        Complex[] even = new Complex[ n / 2 ];
        Complex[] odd = new Complex[ n / 2 ];

        for (int i = 0; i < n / 2; i++) {
            even[ i ] = x[ i * 2 ];
            odd[ i ] = x[ i * 2 + 1 ];
        }

        // Conquer
        Complex[] evenResult = FFT(even);
        Complex[] oddResult = FFT(odd);

        // Combine
        Complex[] result = new Complex[ n ];
        for (int i = 0; i < n / 2; i++) {
            Complex t = Complex.Exp(-2.0 * Math.PI * i / n * Complex.ImaginaryOne) * oddResult[ i ];
            result[ i ] = evenResult[ i ] + t;
            result[ i + n / 2 ] = evenResult[ i ] - t;
        }

        return result;
    }

    void ApplyWindowFunction(float[] data, FFTWindow windowType) {
        int n = data.Length;

        switch (windowType) {
            case FFTWindow.Rectangular:
                // No windowing
                break;

            case FFTWindow.Triangle:
                for (int i = 0; i < n; i++) {
                    data[ i ] *= 1f - Mathf.Abs((2f * i - n + 1f) / (n + 1f));
                }
                break;

            case FFTWindow.Hamming:
                for (int i = 0; i < n; i++) {
                    data[ i ] *= 0.54f - 0.46f * Mathf.Cos(2f * Mathf.PI * i / (n - 1f));
                }
                break;

            case FFTWindow.Hanning:
                for (int i = 0; i < n; i++) {
                    data[ i ] *= 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * i / (n - 1f)));
                }
                break;

            case FFTWindow.Blackman:
                for (int i = 0; i < n; i++) {
                    data[ i ] *= 0.42f - 0.5f * Mathf.Cos(2f * Mathf.PI * i / (n - 1f)) + 0.08f * Mathf.Cos(4f * Mathf.PI * i / (n - 1f));
                }
                break;

            case FFTWindow.BlackmanHarris:
                for (int i = 0; i < n; i++) {
                    float x = 2f * Mathf.PI * i / (n - 1f);
                    data[ i ] *= 0.35875f - 0.48829f * Mathf.Cos(x) + 0.14128f * Mathf.Cos(2f * x) - 0.01168f * Mathf.Cos(3f * x);
                }
                break;
        }
    }

    List<FrequencyPeak> FindPeaksInSpectrum(float[] spectrum, float timeStamp, int sampleRate) {
        List<FrequencyPeak> peaks = new List<FrequencyPeak>();
        int minBin = FrequencyToBin(minFrequency, sampleRate, spectrum.Length);
        int maxBin = FrequencyToBin(maxFrequency, sampleRate, spectrum.Length);

        // Find local maxima
        for (int i = minBin + 1; i < maxBin - 1 && peaks.Count < maxPeaksPerFrame; i++) {
            if (spectrum[ i ] > peakThreshold &&
                spectrum[ i ] > spectrum[ i - 1 ] &&
                spectrum[ i ] > spectrum[ i + 1 ]) {
                // Check minimum distance from other peaks
                bool tooClose = false;
                foreach (var existingPeak in peaks) {
                    if (Mathf.Abs(FrequencyToBin(existingPeak.frequency, sampleRate, spectrum.Length) - i) < minPeakDistance) {
                        // Keep the stronger peak
                        if (spectrum[ i ] > existingPeak.magnitude) {
                            peaks.Remove(existingPeak);
                            break;
                        } else {
                            tooClose = true;
                            break;
                        }
                    }
                }

                if (!tooClose) {
                    float frequency = BinToFrequency(i, sampleRate, spectrum.Length);
                    peaks.Add(new FrequencyPeak(timeStamp, frequency, spectrum[ i ]));
                }
            }
        }

        // Sort by magnitude (strongest peaks first)
        peaks.Sort((a, b) => b.magnitude.CompareTo(a.magnitude));

        return peaks;
    }

    List<NoteEvent> TrackFrequenciesOverTime(List<FrequencyPeak> allPeaks) {
        List<NoteEvent> noteEvents = new List<NoteEvent>();

        // Group peaks by time
        var peaksByTime = allPeaks.GroupBy(p => p.timeStamp).OrderBy(g => g.Key);

        foreach (var timeGroup in peaksByTime) {
            float currentTime = timeGroup.Key;

            foreach (var peak in timeGroup) {
                // Try to match with existing tracks
                FrequencyTrack matchingTrack = null;
                float minDistance = float.MaxValue;

                foreach (var track in frequencyTracks) {
                    if (currentTime - track.lastUpdateTime <= trackDecayTime) {
                        float freqDistance = Mathf.Abs(peak.frequency - track.frequency) / track.frequency;
                        if (freqDistance <= frequencyTolerance && freqDistance < minDistance) {
                            minDistance = freqDistance;
                            matchingTrack = track;
                        }
                    }
                }

                if (matchingTrack != null) {
                    // Update existing track
                    matchingTrack.AddPeak(peak);
                } else {
                    // Create new track
                    frequencyTracks.Add(new FrequencyTrack(peak));
                }
            }
        }

        // Convert valid tracks to note events
        foreach (var track in frequencyTracks) {
            if (track.peaks.Count >= minTrackLength) {
                // Use the peak with highest magnitude as the note event
                var strongestPeak = track.peaks.OrderByDescending(p => p.magnitude).First();
                noteEvents.Add(new NoteEvent(track.startTime, strongestPeak.frequency, strongestPeak.magnitude));
            }
        }

        return noteEvents.OrderBy(n => n.time).ToList();
    }

    AnimationCurve CreateAnimationCurveFromNotes(List<NoteEvent> notes, float clipLength) {
        AnimationCurve curve = new AnimationCurve();

        // Add initial keyframe at zero
        curve.AddKey(new Keyframe(0f, 0f));

        if (combineOverlappingSpikes) {
            notes = CombineOverlappingNotes(notes);
        }

        foreach (var note in notes) {
            float spikeHeight = CalculateSpikeHeight(note.frequency);

            // Add keyframe just before the spike
            curve.AddKey(new Keyframe(note.time - 0.001f, 0f));

            // Add spike peak
            Keyframe peakKey = new Keyframe(note.time, spikeHeight);
            peakKey.inTangent = float.PositiveInfinity;  // Sharp rise
            peakKey.outTangent = float.NegativeInfinity; // Sharp fall
            curve.AddKey(peakKey);

            // Add keyframe just after the spike
            curve.AddKey(new Keyframe(note.time + spikeDuration, 0f));
        }

        // Add final keyframe at zero
        curve.AddKey(new Keyframe(clipLength, 0f));

        return curve;
    }

    List<NoteEvent> CombineOverlappingNotes(List<NoteEvent> notes) {
        List<NoteEvent> combinedNotes = new List<NoteEvent>();

        foreach (var note in notes.OrderBy(n => n.time)) {
            bool merged = false;

            for (int i = 0; i < combinedNotes.Count; i++) {
                if (Mathf.Abs(note.time - combinedNotes[ i ].time) <= spikeDuration) {
                    // Combine notes: use weighted average of frequency and max energy
                    var existingNote = combinedNotes[ i ];
                    float totalEnergy = note.energy + existingNote.energy;
                    float newFrequency = (note.frequency * note.energy + existingNote.frequency * existingNote.energy) / totalEnergy;
                    float newTime = (note.time * note.energy + existingNote.time * existingNote.energy) / totalEnergy;

                    combinedNotes[ i ] = new NoteEvent(newTime, newFrequency, totalEnergy);
                    merged = true;
                    break;
                }
            }

            if (!merged) {
                combinedNotes.Add(note);
            }
        }

        return combinedNotes;
    }

    float CalculateSpikeHeight(float frequency) {
        if (logarithmicPitchMapping) {
            float minLog = Mathf.Log(minFrequency);
            float maxLog = Mathf.Log(maxFrequency);
            float freqLog = Mathf.Log(frequency);

            float normalizedPitch = (freqLog - minLog) / (maxLog - minLog);
            return Mathf.Clamp01(normalizedPitch) * maxSpikeHeight;
        } else {
            float normalizedPitch = (frequency - minFrequency) / (maxFrequency - minFrequency);
            return Mathf.Clamp01(normalizedPitch) * maxSpikeHeight;
        }
    }

    float[] ConvertToMono(float[] stereoData) {
        float[] monoData = new float[ stereoData.Length / 2 ];
        for (int i = 0; i < monoData.Length; i++) {
            monoData[ i ] = (stereoData[ i * 2 ] + stereoData[ i * 2 + 1 ]) / 2f;
        }
        return monoData;
    }

    int FrequencyToBin(float frequency, int sampleRate, int fftSize) {
        return Mathf.RoundToInt(frequency * fftSize / sampleRate);
    }

    float BinToFrequency(int bin, int sampleRate, int fftSize) {
        return (float)bin * sampleRate / fftSize;
    }

    void SaveCurveAsAsset() {
        if (generatedCurve == null) return;

        string path = EditorUtility.SaveFilePanelInProject(
            "Save Animation Curve",
            "EnhancedAudioGeneratedCurve",
            "asset",
            "Save the generated animation curve as an asset");

        if (!string.IsNullOrEmpty(path)) {
            AnimationCurveAsset asset = ScriptableObject.CreateInstance<AnimationCurveAsset>();
            asset.curve = generatedCurve;

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorGUIUtility.PingObject(asset);
            analysisStatus = $"Curve saved to: {path}";
        }
    }
}

[System.Serializable]
public class FrequencyPeak {
    public float timeStamp;
    public float frequency;
    public float magnitude;

    public FrequencyPeak(float timeStamp, float frequency, float magnitude) {
        this.timeStamp = timeStamp;
        this.frequency = frequency;
        this.magnitude = magnitude;
    }
}

[System.Serializable]
public class FrequencyTrack {
    public List<FrequencyPeak> peaks = new List<FrequencyPeak>();
    public float frequency;
    public float startTime;
    public float lastUpdateTime;

    public FrequencyTrack(FrequencyPeak initialPeak) {
        peaks.Add(initialPeak);
        frequency = initialPeak.frequency;
        startTime = initialPeak.timeStamp;
        lastUpdateTime = initialPeak.timeStamp;
    }

    public void AddPeak(FrequencyPeak peak) {
        peaks.Add(peak);
        // Update frequency as weighted average
        float totalMagnitude = peaks.Sum(p => p.magnitude);
        frequency = peaks.Sum(p => p.frequency * p.magnitude) / totalMagnitude;
        lastUpdateTime = peak.timeStamp;
    }
}

[System.Serializable]
public class NoteEvent {
    public float time;
    public float frequency;
    public float energy;

    public NoteEvent(float time, float frequency, float energy) {
        this.time = time;
        this.frequency = frequency;
        this.energy = energy;
    }
}

[CreateAssetMenu(fileName = "AnimationCurve", menuName = "Audio Analysis/Animation Curve Asset")]
public class AnimationCurveAsset : ScriptableObject {
    public AnimationCurve curve;
}
