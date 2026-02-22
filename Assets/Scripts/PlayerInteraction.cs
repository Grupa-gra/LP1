using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private LayerMask interactLayer;
    [SerializeField] private TextMeshProUGUI promptText;

    private Camera _cam;

    void Start()
    {
        _cam = Camera.main;
    }

    void Update()
    {
        CheckForInteractable();

        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
            TryInteract();
    }

    private IInteractable _currentInteractable;

    void CheckForInteractable()
    {
        Ray ray = new Ray(_cam.transform.position, _cam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactLayer))
        {
            if (hit.collider.TryGetComponent(out IInteractable interactable))
            {
                _currentInteractable = interactable;
                if (promptText) promptText.text = interactable.GetInteractPrompt();
                return;
            }
        }

        _currentInteractable = null;
        if (promptText) promptText.text = "";
    }

    void TryInteract()
    {
        _currentInteractable?.Interact();
    }
}