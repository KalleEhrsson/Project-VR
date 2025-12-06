using UnityEngine;
using Unity.XR.CoreUtils;
using TMPro;
using UnityEngine.UI;
using UnityEngine.XR;

public class TapFloorCalibrator : MonoBehaviour
{
    #region Variables

    public XROrigin xrOrigin;
    public Transform cameraOffset;
    
    public Transform gameFloorMarker;   // visual marker or debug plane
    public Transform gameFloorCollider; // the real floor for gameplay
    
    public static float RealFloorY = 0f;

    public Transform leftController;
    public Transform rightController;

    Transform bestController;

    public float stabilityThreshold = 0.002f;     
    public float holdTimeRequired = 1f;           
    public float smooth = 12f;                    

    public AudioSource audioSource;
    public AudioClip dingClip;

    public float hapticStrength = 0.1f;           
    public float successHapticStrength = 0.3f;    

    public bool showGizmos = true;

    float targetOffset = 0f;

    float leftStableTime = 0f;
    float rightStableTime = 0f;

    float lastLeftY = 0f;
    float lastRightY = 0f;
    
    float leftDownwardTime = 0f;
    float rightDownwardTime = 0f;
    public float downwardRequired = 0.15f; // How long it must be moving downward

    bool waitingForTouch = false;
    bool calibrated = false;

    GameObject popup;
    string debugText = "";

    Color gizmoIdle = Color.red;
    Color gizmoTouch = Color.blue;
    Color gizmoCalibrated = Color.green;

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
        calibrated = false;

        leftStableTime = 0f;
        rightStableTime = 0f;

        lastLeftY = leftController.position.y;
        lastRightY = rightController.position.y;

        ShowPopup("Place both controllers on the floor");
    }

    #endregion


    #region Update Loop

    void Update()
    {
        SmoothMove();

        if (!waitingForTouch)
            return;

        float headY = xrOrigin.Camera.transform.position.y;

        // LEFT CONTROLLER
        float leftY = leftController.position.y;
        float leftMove = Mathf.Abs(leftY - lastLeftY);
        float leftDownSpeed = lastLeftY - leftY; // positive = going down

        bool leftStable = leftMove < stabilityThreshold;
        bool leftLow = leftY < headY - 0.6f;

        // track downward motion
        if (leftDownSpeed > 0.003f)
            leftDownwardTime += Time.deltaTime;
        else
            leftDownwardTime = 0f;

        // only start stability timer if it moved downward first
        if (leftLow && leftStable && leftDownwardTime > downwardRequired)
            leftStableTime += Time.deltaTime;
        else
            leftStableTime = 0f;

        lastLeftY = leftY;


        // RIGHT CONTROLLER
        float rightY = rightController.position.y;
        float rightMove = Mathf.Abs(rightY - lastRightY);
        float rightDownSpeed = lastRightY - rightY;

        bool rightStable = rightMove < stabilityThreshold;
        bool rightLow = rightY < headY - 0.6f;

        if (rightDownSpeed > 0.003f)
            rightDownwardTime += Time.deltaTime;
        else
            rightDownwardTime = 0f;

        if (rightLow && rightStable && rightDownwardTime > downwardRequired)
            rightStableTime += Time.deltaTime;
        else
            rightStableTime = 0f;

        lastRightY = rightY;

        // Pick the better (lowest) controller automatically
        bestController = leftY < rightY ? leftController : rightController;
        float bestHeight = bestController.position.y;

        // Debug readout
        debugText =
            $"L_Y: {leftY:F3}  R_Y: {rightY:F3}\n" +
            $"L_Stable: {leftStableTime:F2}  R_Stable: {rightStableTime:F2}\n" +
            $"Best: {(bestController == leftController ? "Left" : "Right")}";

        // If ANY controller stays low + stable long enough → calibrate
        if (leftStableTime >= holdTimeRequired || rightStableTime >= holdTimeRequired)
        {
            Calibrate(bestHeight);
            return;
        }

        ShowPopup("Hold controllers still on the floor...");
    }

    #endregion


    #region Calibration

    void Calibrate(float chosenHeight)
    {
        waitingForTouch = false;
        calibrated = true;

        targetOffset = -chosenHeight;

        PlayerPrefs.SetFloat("FloorOffset", targetOffset);
        PlayerPrefs.Save();

        // Update global floor height
        RealFloorY = chosenHeight;

        // Update game scene objects
        if (gameFloorMarker != null)
            gameFloorMarker.position = new Vector3(gameFloorMarker.position.x, chosenHeight, gameFloorMarker.position.z);

        if (gameFloorCollider != null)
            gameFloorCollider.position = new Vector3(gameFloorCollider.position.x, chosenHeight, gameFloorCollider.position.z);

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
        if (!showGizmos)
            return;

        Transform c = bestController != null ? bestController : leftController;
        if (c == null) return;

        Gizmos.color = calibrated ? gizmoCalibrated : gizmoIdle;

        Vector3 pos = c.position;
        Gizmos.DrawSphere(pos, 0.02f);

        Vector3 floorPoint = new Vector3(pos.x, 0, pos.z);
        Gizmos.DrawLine(pos, floorPoint);
        Gizmos.DrawWireCube(floorPoint, new Vector3(0.1f, 0.002f, 0.1f));
    }

    #endregion
}