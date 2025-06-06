using UnityEngine;

public class FallingNote : MonoBehaviour {
    public int LaneIndex { get; private set; }
    public bool IsHit { get; private set; }
    public float Duration { get; private set; }

    private float targetHitTime;
    private float speed;
    private float hitZoneY;
    private bool shouldDestroy;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Vector3 originalScale;

    [Header("Visual Settings")]
    public Color[] laneColors = new Color[]
    {
        Color.red, Color.blue, Color.green, Color.yellow
    };

    [Header("Duration Visual Settings")]
    public float minNoteHeight = 0.5f;
    public float noteWidth = 1f;

    void Awake() {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        originalColor = spriteRenderer.color;
        originalScale = transform.localScale;
    }

    public void Initialize(int laneIndex, float targetHitTime, float speed, float hitZoneY, float duration = 0f) {
        this.LaneIndex = laneIndex;
        this.targetHitTime = targetHitTime;
        this.speed = speed;
        this.hitZoneY = hitZoneY;
        this.Duration = duration;

        // Set lane color
        if (laneIndex < laneColors.Length) {
            spriteRenderer.color = laneColors[ laneIndex ];
            originalColor = laneColors[ laneIndex ];
        }

        // Adjust visual size based on duration
        SetupNoteVisuals();
    }

    void SetupNoteVisuals() {
        if (Duration > 0f) {
            // Create a square/rectangle based on duration
            float visualHeight = Mathf.Max(minNoteHeight, Duration * speed);
            transform.localScale = new Vector3(noteWidth, visualHeight, originalScale.z);

            // Create or ensure we have a proper sprite for the rectangle
            if (spriteRenderer.sprite == null) {
                // Create a simple white sprite if none exists
                Texture2D texture = new Texture2D(1, 1);
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            }
        } else {
            // Single point note - keep original scale
            transform.localScale = originalScale;
        }
    }

    public void UpdateNote(float currentTime) {
        if (IsHit || shouldDestroy) return;

        // Move note downward
        transform.Translate(Vector3.down * speed * Time.deltaTime);

        // Check if note should be destroyed (accounting for duration)
        float noteBottom = transform.position.y - (transform.localScale.y * 0.5f);
        if (noteBottom < hitZoneY - 2f) {
            shouldDestroy = true;
        }

        // Change color based on distance to the hit zone
        float noteCenter = transform.position.y;
        float distanceToHitZone = Mathf.Abs(noteCenter - hitZoneY);

        if (distanceToHitZone < 1f) {
            float flashIntensity = 1f - (distanceToHitZone / 1f);
            spriteRenderer.color = Color.Lerp(originalColor, Color.white, flashIntensity * 0.5f);
        }
    }

    public bool IsInHitZone() {
        if (Duration > 0f) {
            // For duration notes, check if any part of the note is in the hit zone
            float noteTop = transform.position.y + (transform.localScale.y * 0.5f);
            float noteBottom = transform.position.y - (transform.localScale.y * 0.5f);

            return noteBottom <= hitZoneY && noteTop >= hitZoneY;
        } else {
            // For point notes, use distance threshold
            float distance = Mathf.Abs(transform.position.y - hitZoneY);
            return distance <= 1f; // Adjust threshold as needed
        }
    }

    public float GetHitAccuracy() {
        float noteCenter = transform.position.y;
        return Mathf.Abs(noteCenter - hitZoneY);
    }

    public void Hit() {
        IsHit = true;
        StartCoroutine(HitEffect());
    }

    public bool ShouldDestroy() => shouldDestroy;

    System.Collections.IEnumerator HitEffect() {
        Vector3 hitScale = transform.localScale;
        Color fadeColor = spriteRenderer.color;

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            transform.localScale = Vector3.Lerp(hitScale, hitScale * 1.5f, progress);
            fadeColor.a = Mathf.Lerp(1f, 0f, progress);
            spriteRenderer.color = fadeColor;

            yield return null;
        }

        shouldDestroy = true;
    }
}
