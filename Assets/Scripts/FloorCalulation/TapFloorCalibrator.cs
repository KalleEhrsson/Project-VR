using UnityEngine;
using Unity.XR.CoreUtils;
using TMPro;
using UnityEngine.UI;
using UnityEngine.XR;

public class TapFloorCalibrator : MonoBehaviour
{
    #region Variables

    public XROrigin xrOrigin;
    public Transform controller;
    public Transform cameraOffset;

    public float stabilityThreshold = 0.002f;     // When controller is considered still
    public float descentThreshold = 0.01f;        // How fast must it move downward
    public float holdTimeRequired = 1f;         // Time on floor required
    public float smooth = 12f;                    // Rig move smoothness

    public AudioSource audioSource;
    public AudioClip dingClip;

    public float hapticStrength = 0.1f;           // Subtle vibration while holding
    public float successHapticStrength = 0.3f;    // Stronger vibration on success

    public bool showGizmos = true;

    float targetOffset = 0f;
    float holdTimer = 0f;
    float lastY = 0f;

    bool waitingForTouch = false;
    bool touchedSurface = false;
    bool calibrated = false;

    GameObject popup;
    string debugText = "";

    Color gizmoIdle = Color.red;             // While waiting for touch
    Color gizmoTouch = Color.blue;           // While holding on floor
    Color gizmoCalibrated = Color.green;     // After calibration success

    #endregion


    #region Startup

    void Start()
    {
        if (PlayerPrefs.HasKey("FloorOffset"))
        {
            float saved = PlayerPrefs.GetFloat("FloorOffset");
            ApplyInstant(saved);
        }
    }

    #endregion


    #region Public API

    public void BeginCalibration()
    {
        waitingForTouch = true;
        touchedSurface = false;
        calibrated = false;
        holdTimer = 0;

        lastY = controller.position.y;

        ShowPopup("Touch the floor with your controller...");
    }

    #endregion


    #region Update Loop

    void Update()
    {
        SmoothMove();

        if (!waitingForTouch)
            return;

        float currentY = controller.position.y;
        float movement = Mathf.Abs(currentY - lastY);
        float downwardSpeed = lastY - currentY;
        lastY = currentY;

        debugText =
            $"Y: {currentY:F3}\n" +
            $"Movement: {movement:F4}\n" +
            $"DownSpeed: {downwardSpeed:F4}\n" +
            $"Hold: {holdTimer:F2}\n" +
            $"Touched: {touchedSurface}";

        // After touching surface
        if (touchedSurface)
        {
            if (movement > stabilityThreshold)
            {
                holdTimer = 0;
                ShowPopup("Hold still...");
                return;
            }

            SendHaptics(hapticStrength, 0.1f);

            holdTimer += Time.deltaTime;
            ShowPopup("Hold still...");

            if (holdTimer >= holdTimeRequired)
            {
                Calibrate();
            }

            return;
        }

        // Detect touch moment
        bool movingDownward = downwardSpeed > descentThreshold;
        bool stableNow = movement < stabilityThreshold;

        if (movingDownward && stableNow)
        {
            touchedSurface = true;
            ShowPopup("Hold still...");
        }
    }

    #endregion


    #region Calibration

    void Calibrate()
    {
        waitingForTouch = false;
        calibrated = true;

        float controllerY = controller.position.y;
        targetOffset = -controllerY;

        PlayerPrefs.SetFloat("FloorOffset", targetOffset);
        PlayerPrefs.Save();

        SendHaptics(successHapticStrength, 0.2f);
        PlayDing();

        ShowPopup("Floor calibrated!");
    }

    #endregion


    #region Movement

    void SmoothMove()
    {
        if (!Mathf.Approximately(cameraOffset.localPosition.y, targetOffset))
        {
            float newY = Mathf.Lerp(
                cameraOffset.localPosition.y,
                targetOffset,
                Time.deltaTime * smooth
            );

            cameraOffset.localPosition = new Vector3(
                cameraOffset.localPosition.x,
                newY,
                cameraOffset.localPosition.z
            );
        }
    }

    void ApplyInstant(float offset)
    {
        targetOffset = offset;
        cameraOffset.localPosition =
            new Vector3(cameraOffset.localPosition.x, offset, cameraOffset.localPosition.z);
    }

    #endregion


    #region Haptics + Audio

    void SendHaptics(float amplitude, float duration)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (device.isValid)
            device.SendHapticImpulse(0, amplitude, duration);
    }

    void PlayDing()
    {
        if (audioSource != null && dingClip != null)
            audioSource.PlayOneShot(dingClip, 0.7f);
    }

    #endregion


    #region Popup System

    void ShowPopup(string message)
    {
        if (popup == null)
            popup = CreatePopup();

        popup.GetComponentInChildren<TextMeshProUGUI>().text = message;

        Transform cam = xrOrigin.Camera.transform;
        popup.transform.position = cam.position + cam.forward * 0.8f;
        popup.transform.rotation = Quaternion.LookRotation(cam.forward);
    }

    GameObject CreatePopup()
    {
        GameObject canvasObj = new GameObject("PopupCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 500;

        canvasObj.AddComponent<GraphicRaycaster>();

        RectTransform rt = canvasObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(500, 200);
        rt.localScale = Vector3.one * 0.0018f;

        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(canvasObj.transform, false);
        Image img = bg.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.7f);

        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.sizeDelta = new Vector2(500, 200);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(bg.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 48;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        RectTransform textRT = tmp.GetComponent<RectTransform>();
        textRT.sizeDelta = new Vector2(480, 180);

        return canvasObj;
    }

    #endregion


    #region Gizmos

    void OnDrawGizmos()
    {
        if (!showGizmos || controller == null)
            return;

        Gizmos.color = calibrated ? gizmoCalibrated :
                      touchedSurface ? gizmoTouch :
                      gizmoIdle;

        Vector3 pos = controller.position;

        Gizmos.DrawSphere(pos, 0.02f);

        Vector3 floorPoint = new Vector3(pos.x, 0, pos.z);
        Gizmos.DrawLine(pos, floorPoint);

        Gizmos.DrawWireCube(floorPoint, new Vector3(0.1f, 0.002f, 0.1f));
    }

    #endregion
}