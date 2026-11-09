using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Player model")]
    public GameObject unityChanModelPrefab;
    public float modelScale = 1f;
    public float moveSpeed = 4.5f;
    public float sprintSpeed = 10f;
    public float turnSpeed = 12f;
    public float jumpHeight = 1.25f;
    [Tooltip("Small safety delay to ignore duplicate jump presses during takeoff/landing.")]
    public float jumpRepeatLockout = 0.25f;

    private CharacterController controller;
    private Camera mainCamera;
    private Animator modelAnimator;
    private float verticalVelocity;
    private bool jumpQueued;
    private bool jumpButtonNeedsRelease;
    private float nextJumpAllowedTime;

    private void Start()
    {
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        capsule.enabled = false;
        MeshRenderer placeholder = GetComponent<MeshRenderer>();
        if (placeholder != null) placeholder.enabled = false;

        controller = GetComponent<CharacterController>();
        if (controller == null) controller = gameObject.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = 0.42f;
        controller.center = Vector3.zero;
        controller.stepOffset = 0.3f;
        controller.slopeLimit = 45f;
        controller.skinWidth = 0.025f;
        controller.minMoveDistance = 0f;

        if (unityChanModelPrefab != null)
        {
            GameObject model = Instantiate(unityChanModelPrefab, transform);
            model.name = "Unity-Chan Player Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one * modelScale;
            DisableModelDemoOverlays(model);
            modelAnimator = model.GetComponentInChildren<Animator>();
            PrepareModelMaterials(model);

            // Imported character prefabs can have different pivot heights. Align
            // the visible model's feet to the controller's actual ground contact.
            AlignModelFeet(model);
        }

        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            CameraFollow follow = mainCamera.GetComponent<CameraFollow>();
            if (follow == null) follow = mainCamera.gameObject.AddComponent<CameraFollow>();
            follow.target = transform;
        }

        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health == null) gameObject.AddComponent<PlayerHealth>();
    }

    private static void DisableModelDemoOverlays(GameObject model)
    {
        string[] demoScripts =
        {
            "FaceUpdate",
            "IdleChanger",
            "UnityChanControlScriptWithRgidBody",
            "IKCtrlRightHand",
            "RandomWind"
        };

        foreach (MonoBehaviour behaviour in model.GetComponentsInChildren<MonoBehaviour>(true))
        {
            bool drawsDemoGui = behaviour.GetType().GetMethod(
                "OnGUI",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.DeclaredOnly) != null;
            if (drawsDemoGui || System.Array.IndexOf(demoScripts, behaviour.GetType().Name) >= 0)
                behaviour.enabled = false;
        }
    }

    private static void PrepareModelMaterials(GameObject model)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) return;

        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null || material.shader == urpLit) continue;

                Texture mainTexture = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
                Color baseColor = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
                material.shader = urpLit;
                if (mainTexture != null) material.SetTexture("_BaseMap", mainTexture);
                material.SetColor("_BaseColor", baseColor);
                material.SetFloat("_Smoothness", 0.35f);
            }
            renderer.materials = materials;
        }
    }

    private void AlignModelFeet(GameObject model)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

        float groundY = transform.position.y - controller.height * 0.5f + controller.center.y;
        model.transform.position += Vector3.up * (groundY - bounds.min.y);
    }

    private void Update()
    {
        // Keep the player still until the title screen is dismissed, then allow
        // the bridge crossing after the guardian falls.
        if (GameManager.Instance != null &&
            (!GameManager.Instance.HasStarted || GameManager.Instance.IsGameOver || GameManager.Instance.IsComplete)) return;
        if (Keyboard.current == null || controller == null) return;

        Vector2 input = Vector2.zero;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1f;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.y += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.y -= 1f;
        input = Vector2.ClampMagnitude(input, 1f);

        // Map controls to the camera's horizontal view so W moves toward the
        // center of the screen. CameraFollow pauses its automatic heading
        // alignment while a movement key is held, preventing a feedback loop.
        Vector3 cameraForward = mainCamera != null
            ? Vector3.ProjectOnPlane(mainCamera.transform.forward, Vector3.up).normalized
            : Vector3.forward;
        Vector3 cameraRight = mainCamera != null
            ? Vector3.ProjectOnPlane(mainCamera.transform.right, Vector3.up).normalized
            : Vector3.right;
        Vector3 direction = cameraRight * input.x + cameraForward * input.y;
        direction = Vector3.ClampMagnitude(direction, 1f);
        float speed = Keyboard.current.leftShiftKey.isPressed ? sprintSpeed : moveSpeed;
        if (modelAnimator != null)
        {
            // Use the forward walk cycle while moving. The imported run cycle
            // has a strong airborne bounce that looks like repeated jumping;
            // speed up the walk cycle for sprinting instead of blending into it.
            bool isMoving = direction.sqrMagnitude > 0.001f;
            // The controller exits Idle above 0.1; 0.15 activates its walk
            // cycle while remaining near the walk end of the blend tree.
            modelAnimator.SetFloat("Speed", isMoving ? 0.15f : 0f, 0.12f, Time.deltaTime);
            modelAnimator.speed = isMoving
                ? Mathf.Clamp(speed / Mathf.Max(0.1f, moveSpeed), 1f, 2f)
                : 1f;
            // This controller turns the character toward the movement vector.
            // Keeping Direction at zero selects the forward walk/run clips;
            // blending left/right while the root is turning makes the model
            // visibly move sideways during ordinary movement and jumps.
            modelAnimator.SetFloat("Direction", 0f);
        }

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), turnSpeed * Time.deltaTime);
        }

        // Jump is deliberately bound to a fresh Space key press only. Re-arm
        // after the Input System reports a real key release, not a transient
        // non-pressed frame, so held/repeated input cannot auto-jump.
        var spaceKey = Keyboard.current.spaceKey;
        if (spaceKey.wasReleasedThisFrame) jumpButtonNeedsRelease = false;

        if (controller.isGrounded)
        {
            verticalVelocity = -1f;
            if (spaceKey.wasPressedThisFrame &&
                !jumpButtonNeedsRelease &&
                Time.time >= nextJumpAllowedTime)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
                jumpQueued = true;
                jumpButtonNeedsRelease = true;
                nextJumpAllowedTime = Time.time + Mathf.Max(0f, jumpRepeatLockout);
            }
        }
        else verticalVelocity += Physics.gravity.y * Time.deltaTime;

        controller.Move((direction * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
        if (modelAnimator != null && jumpQueued)
        {
            modelAnimator.SetTrigger("Jump");
            jumpQueued = false;
        }
    }
}
