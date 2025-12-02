using UnityEngine;
using UnityEngine.InputSystem;

public class VRLocomotion : MonoBehaviour
{
    public InputActionProperty moveAction;
    public Transform head;
    CharacterController cc;

    float gravity = -9.81f;
    float verticalVel;

    void Start()
    {
        cc = GetComponent<CharacterController>();

        if (moveAction.action != null)
            moveAction.action.Enable(); // important for XR sample actions
    }

    void Update()
    {
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

    void UpdateCollider()
    {
        float headHeight = Mathf.Clamp(head.localPosition.y, 1f, 2f);
        cc.height = headHeight;
        cc.center = new Vector3(0, cc.height / 2f, 0);
    }
}