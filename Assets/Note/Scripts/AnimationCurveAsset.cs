using UnityEngine;

[CreateAssetMenu(fileName = "RhythmGameCurve", menuName = "Rhythm Game/Curve Asset")]
public class AnimationCurveAsset : ScriptableObject {
    [Header("Animation Curve")]
    public AnimationCurve curve = new AnimationCurve();

    [Header("Curve Info")]
    [TextArea(2, 4)]
    public string description = "Generated from audio analysis";
    public float originalAudioLength;
    public string sourceAudioName;

    [Header("Preview")]
    public bool showCurvePreview = true;

    void OnValidate() {
        if (curve != null && showCurvePreview) {
            Debug.Log($"Curve has {curve.keys.Length} keyframes, duration: {GetCurveDuration():F2}s");
        }
    }

    public float GetCurveDuration() {
        if (curve == null || curve.keys.Length == 0) return 0f;
        return curve.keys[ curve.keys.Length - 1 ].time;
    }

    public int CountSpikes(float threshold = 0.1f) {
        if (curve == null) return 0;

        int spikeCount = 0;
        for (int i = 0; i < curve.keys.Length; i++) {
            if (curve.keys[ i ].value > threshold)
                spikeCount++;
        }
        return spikeCount;
    }
}

