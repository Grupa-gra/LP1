using System.Collections;
using TMPro;
using UnityEngine;

public static class CollectibleItemGlob
{
    public static int ScoreCount = 0;
}

public class CollectibleItem : MonoBehaviour, IInteractable
{
    [SerializeField] private string itemName = "Przedmiot";

    [SerializeField] private TextMeshProUGUI score;
    [SerializeField] private TextMeshProUGUI scoreInfo;

    [SerializeField] private float popupDuration = 2f;
    [SerializeField] private float startOffsetY = 15f;

    public int scoreValue = 10;

    public float respawnTime = 30f;
    private bool isCollected = false;

    [Header("Grow Animation")]
    [SerializeField] private float growDuration = 0.5f;
    [SerializeField] private Vector3 targetScale = Vector3.one;

    private Coroutine growCoroutine;

    private RectTransform scoreInfoRect;
    private CanvasGroup scoreInfoGroup;

    private void Start()
    {
        targetScale = transform.localScale;

        if (scoreInfo != null)
        {
            scoreInfoRect = scoreInfo.GetComponent<RectTransform>();
            scoreInfoGroup = scoreInfo.GetComponent<CanvasGroup>();

            if (scoreInfoGroup == null)
                scoreInfoGroup = scoreInfo.gameObject.AddComponent<CanvasGroup>();

            scoreInfoGroup.alpha = 0f;
        }
    }

    public string GetInteractPrompt()
    {
        if (isCollected) return string.Empty;
        return itemName;
    }

    public string GetCollectText()
    {
        string color = scoreValue < 0 ? "red" : "white";
        string sign = scoreValue >= 0 ? "+" : "";

        return $"<color={color}>{sign}{scoreValue} {itemName}</color>";
    }

    public void Interact()
    {
        if (isCollected) return;

        CollectibleItemGlob.ScoreCount += scoreValue;

        if (score != null)
            score.text = "Wynik: " + CollectibleItemGlob.ScoreCount;

        if (scoreInfo != null)
        {
            StopCoroutine(nameof(ScorePopupRoutine));
            StartCoroutine(ScorePopupRoutine());
        }

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator ScorePopupRoutine()
    {
        scoreInfo.text = GetCollectText();

        Vector2 startPos = scoreInfoRect.anchoredPosition;
        Vector2 hiddenPos = startPos + Vector2.up * startOffsetY;

        scoreInfoRect.anchoredPosition = hiddenPos;
        scoreInfoGroup.alpha = 1f;

        float t = 0f;

        while (t < popupDuration)
        {
            t += Time.deltaTime;

            scoreInfoRect.anchoredPosition = Vector2.Lerp(
                hiddenPos,
                startPos,
                t / popupDuration
            );

            scoreInfoGroup.alpha = Mathf.Lerp(1f, 0f, t / popupDuration);

            yield return null;
        }

        scoreInfoGroup.alpha = 0f;
    }

    private IEnumerator RespawnRoutine()
    {
        isCollected = true;

        ToggleVisibility(false);

        yield return new WaitForSeconds(respawnTime);

        ToggleVisibility(true);

        transform.localScale = Vector3.zero;

        if (growCoroutine != null)
            StopCoroutine(growCoroutine);

        growCoroutine = StartCoroutine(GrowRoutine());

        isCollected = false;
    }

    private IEnumerator GrowRoutine()
    {
        float t = 0f;

        while (t < growDuration)
        {
            t += Time.deltaTime;

            float progress = t / growDuration;
            float eased = 1f - Mathf.Pow(1f - progress, 3f);

            transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, eased);

            yield return null;
        }

        transform.localScale = targetScale;
    }

    private void ToggleVisibility(bool state)
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = state;

        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = state;
    }
}