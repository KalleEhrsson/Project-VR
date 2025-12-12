using UnityEngine;

public class GrabPoint : MonoBehaviour
{
    public int priority = 10;

    static GameObject bubblePrefab;
    GameObject bubbleInstance;

    void Awake()
    {
        if (bubblePrefab == null)
            bubblePrefab = Resources.Load<GameObject>("GrabBubble");
    }

    public bool IsAboveFloor()
    {
        return transform.position.y >= TapFloorCalibrator.RealFloorY + 0.02f;
    }

    public void Show()
    {
        if (!IsAboveFloor())
            return;

        if (bubblePrefab == null)
        {
            Debug.LogError("GrabBubble prefab not found in Resources folder");
            return;
        }

        if (bubbleInstance == null)
        {
            bubbleInstance = Instantiate(bubblePrefab, transform);
            bubbleInstance.transform.localPosition = Vector3.zero;
            bubbleInstance.transform.localRotation = Quaternion.identity;

            Vector3 prefabScale = bubblePrefab.transform.localScale;
            float parentScale = transform.lossyScale.x;
            bubbleInstance.transform.localScale = prefabScale / parentScale;
        }

        bubbleInstance.SetActive(true);
    }

    public void Hide()
    {
        if (bubbleInstance != null)
            bubbleInstance.SetActive(false);
    }
}