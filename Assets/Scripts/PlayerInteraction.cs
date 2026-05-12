using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactRange = 30f;
    [SerializeField] private LayerMask Mushrooms;
    [SerializeField] private TextMeshProUGUI promptText;

    [Header("Crosshair")] [SerializeField] private Image crosshair;
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite interactSprite;

    private Camera _cam;
    private IInteractable _currentInteractable;

    void Start()
    {
        _cam = Camera.main;
        SetDefault();
    }

    void Update()
    {
        CheckForInteractable();

        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
        {
            _currentInteractable?.Interact();
        }
    }

    void CheckForInteractable()
    {
        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        // Debug.DrawRay(ray.origin, ray.direction * interactRange, Color.yellow);
        // Debug.DrawRay(ray.origin, Vector3.up * 0.1f, Color.red);
        // Debug.DrawRay(ray.origin, Vector3.right * 0.1f, Color.red);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, Mushrooms))
        {
            if (((1 << hit.collider.gameObject.layer) & Mushrooms) != 0 &&
                hit.collider.GetComponentInParent<IInteractable>() is IInteractable interactable)
            {
                string prompt = interactable.GetInteractPrompt();

                if (!string.IsNullOrEmpty(prompt))
                {
                    _currentInteractable = interactable;

                    if (promptText)
                        promptText.text = prompt;

                    SetInteract();
                    return;
                }
            }
        }

        _currentInteractable = null;

        if (promptText)
            promptText.text = "";

        SetDefault();
    }

    void SetDefault()
    {
        if (crosshair != null)
            crosshair.sprite = defaultSprite;
    }

    void SetInteract()
    {
        if (crosshair != null)
            crosshair.sprite = interactSprite;
    }
}