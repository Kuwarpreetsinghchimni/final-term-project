using UnityEngine;
using UnityEngine.InputSystem;

public class WandShooter : MonoBehaviour
{
    public GameObject magicProjectilePrefab;
    public Transform shootingPoint;
    public float shootingDistance = 45f;
    public float fireCooldown = 0.45f;

    private Camera mainCamera;
    private float nextFireTime;
    private AudioSource shotAudioSource;
    private AudioClip shotSound;

    private void Start()
    {
        mainCamera = Camera.main;
        shotAudioSource = GetComponent<AudioSource>();
        if (shotAudioSource == null) shotAudioSource = gameObject.AddComponent<AudioSource>();
        shotAudioSource.playOnAwake = false;
        shotAudioSource.spatialBlend = 0f;
        shotSound = Resources.Load<AudioClip>("ProjectileMagicSound");
    }

    private void Update()
    {
        if (GameManager.Instance != null &&
            (!GameManager.Instance.HasStarted || GameManager.Instance.IsComplete || GameManager.Instance.IsGameOver)) return;
        if (Mouse.current == null || Keyboard.current == null) return;

        if ((Mouse.current.leftButton.wasPressedThisFrame || Keyboard.current.fKey.wasPressedThisFrame) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireCooldown;
            ShootMagic();
        }
    }

    private void ShootMagic()
    {
        if (magicProjectilePrefab == null) return;
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 target = ray.origin + ray.direction * shootingDistance;
        if (Physics.Raycast(ray, out RaycastHit hit, shootingDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            target = hit.point;

        Vector3 spawnPosition = shootingPoint != null
            ? shootingPoint.position
            : transform.position + Vector3.up * 1.1f + transform.forward * 0.65f;
        Vector3 direction = (target - spawnPosition).normalized;
        if (direction.sqrMagnitude < 0.001f) direction = transform.forward;
        Instantiate(magicProjectilePrefab, spawnPosition, Quaternion.LookRotation(direction));
        if (shotAudioSource != null && shotSound != null)
            shotAudioSource.PlayOneShot(shotSound);
    }
}
