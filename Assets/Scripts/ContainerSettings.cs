using UnityEngine;

[System.Serializable]
public class ContainerSettings {
    [Header("Layout")]
    public float spacing = 220f;
    public float offset = 50f;
    public Vector2 gridCellSize = new Vector2(220f, 320f);
    public int gridColumns = 3;
    public float cardHoverOffset = 30f;
}