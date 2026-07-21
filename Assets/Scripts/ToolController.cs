using UnityEngine;

public class ToolController : MonoBehaviour
{
    public NewMonoBehaviourScript playermov;

    [SerializeField] float offsetDistance = 1.2f;
    [SerializeField] float pickupZone = 1.5f;

    [Header("Click Assist")]
    [Tooltip("How close the cursor can be to a reachable trash collider and still count as a hit.")]
    [SerializeField, Min(0f)] float cursorAssistRadius = 1f;

    private void Awake()
    {
        playermov = GetComponent<NewMonoBehaviourScript>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            UseTool();
        }
    }

    private void UseTool()
    {
        Camera gameCamera = Camera.main;
        if (gameCamera == null)
        {
            Debug.LogWarning("ToolController could not find the Main Camera.", this);
            return;
        }

        Vector2 clickPosition = gameCamera.ScreenToWorldPoint(Input.mousePosition);
        Collider2D[] colliders = Physics2D.OverlapCircleAll(clickPosition, cursorAssistRadius);
        Tool clickedTool = null;
        bool clickedDirectly = false;
        int topSortingLayer = int.MinValue;
        int topSortingOrder = int.MinValue;
        float closestColliderDistance = float.MaxValue;

        // Direct hits win. Otherwise, click assist picks the closest reachable
        // trash collider inside the small cursor radius.
        foreach (Collider2D collider in colliders)
        {
            Tool candidate = collider.GetComponentInParent<Tool>();
            if (candidate == null)
            {
                continue;
            }

            Vector2 origin = transform.position;
            float interactionRange = offsetDistance + pickupZone;
            if (Vector2.Distance(origin, candidate.transform.position) > interactionRange)
            {
                continue;
            }

            SpriteRenderer renderer = candidate.GetComponentInChildren<SpriteRenderer>();
            int sortingLayer = renderer != null
                ? SortingLayer.GetLayerValueFromID(renderer.sortingLayerID)
                : int.MinValue;
            int sortingOrder = renderer != null ? renderer.sortingOrder : int.MinValue;
            bool isDirectHit = collider.OverlapPoint(clickPosition);
            float cursorDistance = ((Vector2)collider.ClosestPoint(clickPosition) - clickPosition).sqrMagnitude;

            bool isBetterTarget = (isDirectHit && !clickedDirectly) ||
                                  (isDirectHit == clickedDirectly && cursorDistance < closestColliderDistance) ||
                                  (isDirectHit == clickedDirectly && Mathf.Approximately(cursorDistance, closestColliderDistance) &&
                                   (sortingLayer > topSortingLayer || sortingLayer == topSortingLayer && sortingOrder > topSortingOrder));

            if (isBetterTarget)
            {
                clickedTool = candidate;
                clickedDirectly = isDirectHit;
                topSortingLayer = sortingLayer;
                topSortingOrder = sortingOrder;
                closestColliderDistance = cursorDistance;
            }
        }

        clickedTool?.Hit();
    }
}
