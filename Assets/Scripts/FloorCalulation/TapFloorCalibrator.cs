using UnityEngine;
using Unity.XR.CoreUtils;
using TMPro;
using UnityEngine.UI;
using UnityEngine.XR;

public class TapFloorCalibrator : MonoBehaviour
{
    #region Inspector Stuff (Rig And Scene References)
    
    [Tooltip("XR Origin for the player rig. Auto-resolved from parents if left empty.")]
    [SerializeField]
    private XROrigin xrOrigin;

    [Tooltip("Camera offset transform used for floor height adjustments. Auto-resolved from XR Origin.")]
    [SerializeField]
    private Transform cameraOffset; // Populated from XR Origin if left empty
    
    [Tooltip("Optional visual marker for the calibrated floor height.")]
    [SerializeField]
    private Transform gameFloorMarker;   // visual marker or debug plane

    [Tooltip("Optional gameplay floor collider that should match the calibrated height.")]
    [SerializeField]
    private Transform gameFloorCollider; // the real floor for gameplay

    [Tooltip("Left controller transform. Must be assigned because controller objects differ per rig.")]
    [SerializeField]
    private Transform leftController; // Kept serialized because controller objects differ per rig

    [Tooltip("Right controller transform. Must be assigned because controller objects differ per rig.")]
    [SerializeField]
    private Transform rightController; // Kept serialized because controller objects differ per rig

    [Tooltip("Audio source used for feedback. Auto-resolved from this object if left empty.")]
    [SerializeField]
    private AudioSource audioSource; // Scene audio source for feedback

    [Tooltip("Audio clip played on successful calibration.")]
    [SerializeField]
    private AudioClip dingClip;

    [SerializeField]
    private bool showGizmos = true;
    #endregion

    #region Tuning (Calibration Settings)
    [SerializeField]
    private float stabilityThreshold = 0.002f;     

    [SerializeField]
    private float holdTimeRequired = 1f;           

    [SerializeField]
    private float smooth = 12f;                    

    // [SerializeField]
    // private float hapticStrength = 0.1f;           

    [SerializeField]
    private float successHapticStrength = 0.3f;    

    [SerializeField]
    private float downwardRequired = 0.15f; // How long it must be moving downward
    #endregion

    #region Shared State (Global Floor Height)
    public static float RealFloorY = 0f;
    #endregion

    #region Current State (What Is Happening Right Now)
    private Transform bestController;
    
    private const float controllerLowOffset = 0.6f;
    private const float downwardSpeedThreshold = 0.003f;

    private float targetOffset = 0f;

    private float leftStableTime = 0f;
    private float rightStableTime = 0f;

    private float lastLeftY = 0f;
    private float lastRightY = 0f;
    
    private float leftDownwardTime = 0f;
    private float rightDownwardTime = 0f;

    private bool waitingForTouch = false;
    private bool calibrated = false;

    private GameObject popup;
    private string debugText = "";

    private readonly Color gizmoIdle = Color.red;
    private readonly Color gizmoTouch = Color.blue;
    private readonly Color gizmoCalibrated = Color.green;
    
    #endregion


    #region Unity Lifetime (Awake Start)
    
    private void Awake()
    {
        xrOrigin ??= GetComponentInParent<XROrigin>();
        if (xrOrigin != null && cameraOffset == null)
            cameraOffset = xrOrigin.CameraFloorOffsetObject != null
                ? xrOrigin.CameraFloorOffsetObject.transform
                : xrOrigin.transform;

        audioSource ??= GetComponent<AudioSource>();
        ValidateReferences();
    }

    private void Start()
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
        if (!AreReferencesValid())
            return;

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

    private void Update()
    {
        if (!AreReferencesValid())
            return;

        SmoothMove();

        if (!waitingForTouch)
            return;

        float headY = xrOrigin.Camera.transform.position.y;

        float leftY = UpdateControllerState(
            leftController,
            headY,
            ref lastLeftY,
            ref leftStableTime,
            ref leftDownwardTime
        );

        float rightY = UpdateControllerState(
            rightController,
            headY,
            ref lastRightY,
            ref rightStableTime,
            ref rightDownwardTime
        );

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

    private void Calibrate(float chosenHeight)
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

    private void SmoothMove()
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

    private void ApplyInstant(float offset)
    {
        targetOffset = offset;
        cameraOffset.localPosition =
            new Vector3(cameraOffset.localPosition.x, offset, cameraOffset.localPosition.z);
    }

    #endregion
    
    #region Controller Tracking

    private float UpdateControllerState(
        Transform controller,
        float headY,
        ref float lastY,
        ref float stableTime,
        ref float downwardTime)
    {
        float currentY = controller.position.y;
        float move = Mathf.Abs(currentY - lastY);
        float downSpeed = lastY - currentY; // positive = going down

        bool isStable = move < stabilityThreshold;
        bool isLow = currentY < headY - controllerLowOffset;

        // track downward motion
        if (downSpeed > downwardSpeedThreshold)
            downwardTime += Time.deltaTime;
        else
            downwardTime = 0f;

        // only start stability timer if it moved downward first
        if (isLow && isStable && downwardTime > downwardRequired)
            stableTime += Time.deltaTime;
        else
            stableTime = 0f;

        lastY = currentY;
        return currentY;
    }

    #endregion
    
    #region Haptics + Audio

    private void SendHaptics(float amplitude, float duration)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (device.isValid)
            device.SendHapticImpulse(0, amplitude, duration);
    }

    private void PlayDing()
    {
        if (audioSource != null && dingClip != null)
            audioSource.PlayOneShot(dingClip, 0.7f);
    }

    #endregion


    #region Popup System

    private void ShowPopup(string message)
    {
        if (popup == null)
            popup = CreatePopup();

        popup.GetComponentInChildren<TextMeshProUGUI>().text = message;

        Transform cam = xrOrigin.Camera.transform;
        popup.transform.position = cam.position + cam.forward * 0.8f;
        popup.transform.rotation = Quaternion.LookRotation(cam.forward);
    }

    private GameObject CreatePopup()
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

    private void OnDrawGizmos()
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

    #region Validation (Runtime Safety)
    private void ValidateReferences()
    {
        if (xrOrigin == null)
            Debug.LogError("TapFloorCalibrator requires an XR Origin.");
        if (cameraOffset == null)
            Debug.LogError("TapFloorCalibrator requires a camera offset transform.");
        if (leftController == null)
            Debug.LogError("TapFloorCalibrator requires a left controller transform.");
        if (rightController == null)
            Debug.LogError("TapFloorCalibrator requires a right controller transform.");
        if (audioSource == null)
            Debug.LogWarning("TapFloorCalibrator has no audio source for ding feedback.");
        if (dingClip == null)
            Debug.LogWarning("TapFloorCalibrator has no ding audio clip assigned.");
    }

    private bool AreReferencesValid()
    {
        if (xrOrigin == null || cameraOffset == null || leftController == null || rightController == null)
        {
            enabled = false;
            return false;
        }

        return true;
    }
    #endregion
}
