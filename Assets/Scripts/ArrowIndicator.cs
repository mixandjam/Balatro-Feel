using UnityEngine;
using static DebugLogger;

public class ArrowIndicator : MonoBehaviour {
    private LineRenderer mainLine;
    private GameObject headObject1;
    private GameObject headObject2;
    private LineRenderer headLine1;
    private LineRenderer headLine2;
    private bool isVisible = false;
    private Vector3 startPosition;
    private Vector3 endPosition;
    private float arrowWidth = 0.2f;
    private float headLength = 0.5f;
    private float headAngle = 25f;
    private Color currentColor = Color.red; // Default to red for visibility

    private void Awake() {
        SetupArrow();
    }

    private void SetupArrow() {
        // Ensure the arrow is part of the UI canvas
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null) {
            LogWarning("ArrowIndicator must be a child of a Canvas", LogTag.UI);
        }

        // Setup main line
        mainLine = gameObject.AddComponent<LineRenderer>();
        mainLine.positionCount = 2;
        mainLine.startWidth = arrowWidth;
        mainLine.endWidth = arrowWidth;
        mainLine.material = new Material(Shader.Find("Sprites/Default"));
        mainLine.material.color = currentColor;

        // Create separate GameObjects for the arrowheads
        headObject1 = new GameObject("ArrowHead1");
        headObject2 = new GameObject("ArrowHead2");
        headObject1.transform.SetParent(transform);
        headObject2.transform.SetParent(transform);

        headLine1 = headObject1.AddComponent<LineRenderer>();
        headLine2 = headObject2.AddComponent<LineRenderer>();

        SetupHeadLine(headLine1);
        SetupHeadLine(headLine2);

        // Set sorting order to appear above cards
        mainLine.sortingOrder = 100;
        headLine1.sortingOrder = 100;
        headLine2.sortingOrder = 100;

        // Ensure the arrow starts hidden
        Hide();
    }

    private void SetupHeadLine(LineRenderer line) {
        line.positionCount = 2;
        line.startWidth = arrowWidth * 1.2f; // Slightly wider at base
        line.endWidth = 0f; // Sharp point at tip
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.material.color = currentColor;
    }

    public static ArrowIndicator Create(Transform parent = null) {
        GameObject arrowObj = new GameObject("AttackArrow");
        if (parent != null) {
            arrowObj.transform.SetParent(parent, false);
            arrowObj.transform.localPosition = Vector3.zero;
        }
        return arrowObj.AddComponent<ArrowIndicator>();
    }

    public void SetColor(Color newColor) {
        currentColor = newColor;
        UpdateColors();
    }

    private void UpdateColors() {
        if (mainLine != null && mainLine.material != null) {
            mainLine.material.color = currentColor;
        }
        if (headLine1 != null && headLine1.material != null) {
            headLine1.material.color = currentColor;
        }
        if (headLine2 != null && headLine2.material != null) {
            headLine2.material.color = currentColor;
        }
    }

    public void Show(Vector3 start, Vector3 end) {
        startPosition = start;
        endPosition = end;

        Log($"Showing arrow from {start} to {end}", LogTag.UI);

        // Ensure lines are not null
        if (mainLine == null || headLine1 == null || headLine2 == null) {
            LogWarning("Arrow lines are null when trying to show", LogTag.UI);
            SetupArrow();
        }

        // Update main line
        mainLine.SetPosition(0, start);
        mainLine.SetPosition(1, end);
        mainLine.enabled = true;

        UpdateArrowHead();

        // Ensure visibility
        mainLine.gameObject.SetActive(true);
        headObject1.SetActive(true);
        headObject2.SetActive(true);

        isVisible = true;
    }

    private void UpdateArrowHead() {
        if (headLine1 == null || headLine2 == null) return;

        Vector3 direction = (endPosition - startPosition).normalized;
        Vector3 right = Quaternion.Euler(0, 0, 90) * direction;

        // Calculate points for the arrowhead
        Vector3 arrowTip = endPosition;
        Vector3 arrowBase = arrowTip - (direction * headLength);
        float baseWidth = headLength * Mathf.Tan(headAngle * Mathf.Deg2Rad);
        Vector3 arrowBaseLeft = arrowBase + (right * baseWidth);
        Vector3 arrowBaseRight = arrowBase - (right * baseWidth);

        // Set the positions for the arrowhead lines
        headLine1.SetPosition(0, arrowBaseLeft);
        headLine1.SetPosition(1, arrowTip);
        headLine1.enabled = true;

        headLine2.SetPosition(0, arrowBaseRight);
        headLine2.SetPosition(1, arrowTip);
        headLine2.enabled = true;

        // Update main line to connect with arrowhead base
        mainLine.SetPosition(1, arrowBase);
    }

    public void ShowSwapAction(Vector3 start1, Vector3 start2) {
        // Create a swap-style arrow between two positions
        startPosition = start1;
        endPosition = start2;

        Log($"Showing swap arrow from {start1} to {start2}", LogTag.UI);

        // Ensure lines are not null
        if (mainLine == null || headLine1 == null || headLine2 == null) {
            LogWarning("Arrow lines are null when trying to show swap", LogTag.UI);
            SetupArrow();
        }

        // Use a distinct yellow color for swap actions
        SetColor(Color.yellow);

        // Create a curved/wavy arrow to indicate swap
        Vector3 midPoint1 = Vector3.Lerp(start1, start2, 0.4f);
        Vector3 midPoint2 = Vector3.Lerp(start1, start2, 0.6f);

        midPoint1.y += 50f; // Add some arc to the arrow
        midPoint2.y += 50f;

        // Main line will be a curved line
        mainLine.positionCount = 3;
        mainLine.SetPosition(0, start1);
        mainLine.SetPosition(1, midPoint1);
        mainLine.SetPosition(2, start2);

        // Arrowheads will point from start to end
        UpdateSwapArrowHead(start1, start2);

        // Ensure visibility
        mainLine.gameObject.SetActive(true);
        headObject1.SetActive(true);
        headObject2.SetActive(true);

        isVisible = true;
    }

    private void UpdateSwapArrowHead(Vector3 start, Vector3 end) {
        if (headLine1 == null || headLine2 == null) return;

        Vector3 direction = (end - start).normalized;
        Vector3 right = Quaternion.Euler(0, 0, 90) * direction;

        // Calculate points for the arrowhead
        Vector3 arrowTip1 = end;
        Vector3 arrowBase1 = arrowTip1 - (direction * headLength);
        float baseWidth = headLength * Mathf.Tan(headAngle * Mathf.Deg2Rad);
        Vector3 arrowBaseLeft1 = arrowBase1 + (right * baseWidth);
        Vector3 arrowBaseRight1 = arrowBase1 - (right * baseWidth);

        // Set the positions for the first arrowhead lines
        headLine1.SetPosition(0, arrowBaseLeft1);
        headLine1.SetPosition(1, arrowTip1);
        headLine1.enabled = true;

        headLine2.SetPosition(0, arrowBaseRight1);
        headLine2.SetPosition(1, arrowTip1);
        headLine2.enabled = true;
    }

    public void Hide() {
        if (mainLine != null) mainLine.enabled = false;
        if (headLine1 != null) headLine1.enabled = false;
        if (headLine2 != null) headLine2.enabled = false;

        // Also deactivate GameObjects to ensure they're not visible
        if (mainLine != null) mainLine.gameObject.SetActive(false);
        if (headObject1 != null) headObject1.SetActive(false);
        if (headObject2 != null) headObject2.SetActive(false);

        isVisible = false;
    }

    public void UpdateEndPosition(Vector3 newEnd) {
        if (isVisible) {
            endPosition = newEnd;
            if (mainLine != null) {
                mainLine.SetPosition(1, newEnd);
            }
            UpdateArrowHead();
        }
    }

    public bool IsVisible() {
        return isVisible;
    }

    private void OnDestroy() {
        // Ensure clean destruction of arrow components
        if (headObject1 != null) Destroy(headObject1);
        if (headObject2 != null) Destroy(headObject2);

        if (mainLine != null && mainLine.material != null) Destroy(mainLine.material);
        if (headLine1 != null && headLine1.material != null) Destroy(headLine1.material);
        if (headLine2 != null && headLine2.material != null) Destroy(headLine2.material);
    }
}