using UnityEngine;
using UnityEngine.InputSystem;

public class Locomotion : MonoBehaviour
{
    #region Inspector Stuff (Input And Rig References)
    [Tooltip("Move input action for locomotion. Must be assigned per input setup.")]
    [SerializeField]
    private InputActionProperty moveAction; // Kept serialized because movement bindings differ between action maps

    [Tooltip("Head transform used to orient movement. Auto-resolved from Camera.main if left empty.")]
    [SerializeField]
    private Transform head; // Visual rig reference required for correct locomotion heading
    #endregion

    #region Cached Components (Self Setup)
    private CharacterController cc;
    #endregion

    #region Current State (What Is Happening Right Now)
    private float gravity = -9.81f;
    private float verticalVel;
    #endregion

    #region Unity Lifetime (Awake Enable Disable Destroy)
    private void Awake()
    {
        head ??= Camera.main != null ? Camera.main.transform : null;
        if (head == null)
        {
            Debug.LogError($"Locomotion on {name} requires a head transform or a Camera tagged MainCamera.");
            enabled = false;
        }
    }

    private void Start()
    {
        cc = GetComponent<CharacterController>();
        if (cc == null)
        {
            Debug.LogError($"Locomotion on {name} requires a CharacterController.");
            enabled = false;
            return;
        }

        if (moveAction.action != null)
            moveAction.action.Enable(); // important for XR sample actions
        else
            Debug.LogWarning($"Locomotion on {name} is missing move input action.");
    }
    #endregion

    #region Main Logic (What Actually Happens)
    private void Update()
    {
        if (!enabled || cc == null || head == null)
            return;

        if (moveAction.action == null)
            return;

        Vector2 input = moveAction.action.ReadValue<Vector2>();

        Vector3 forward = new Vector3(head.forward.x, 0, head.forward.z).normalized;
        Vector3 right = new Vector3(head.right.x, 0, head.right.z).normalized;

        Vector3 move = (forward * input.y + right * input.x);

        cc.Move(move * 3f * Time.deltaTime);

        if (cc.isGrounded && verticalVel < 0) verticalVel = -1f;
        verticalVel += gravity * Time.deltaTime;

        cc.Move(Vector3.up * verticalVel * Time.deltaTime);

        UpdateCollider();
    }

    private void UpdateCollider()
    {
        float headHeight = Mathf.Clamp(head.localPosition.y, 1f, 2f);
        cc.height = headHeight;
        cc.center = new Vector3(0, cc.height / 2f, 0);
    }
    #endregion
}
