
using UnityEngine;

public class DogFollow : MonoBehaviour
{
    [Header("Player Settings")]
    public Transform player;

    [Header("Follow Settings")]
    public float followDistance = 1.8f;
    public float sideOffset = 1.2f;
    public float followSpeed = 5f;
    public float rotationSpeed = 8f;

    [Header("Ground Settings")]
    public float groundHeight = 0f;

    [Header("Dog Sounds")]
    public AudioClip barkClip;
    public AudioClip pantClip;
    [Range(0f, 1f)] public float barkVolume = 0.7f;
    [Range(0f, 1f)] public float pantVolume = 0.22f;
    public Vector2 barkInterval = new Vector2(18f, 32f);

    AudioSource barkSource;
    AudioSource pantSource;
    Animator dogAnimator;
    float nextBarkTime;
    bool playingRunAnimation;

    void Start()
    {
        dogAnimator = GetComponentInChildren<Animator>();

        barkSource = gameObject.AddComponent<AudioSource>();
        barkSource.playOnAwake = false;
        barkSource.spatialBlend = 1f;
        barkSource.dopplerLevel = 0f;
        barkSource.rolloffMode = AudioRolloffMode.Logarithmic;
        barkSource.minDistance = 1.5f;
        barkSource.maxDistance = 18f;

        if (pantClip != null)
        {
            pantSource = gameObject.AddComponent<AudioSource>();
            pantSource.clip = pantClip;
            pantSource.loop = true;
            pantSource.playOnAwake = false;
            pantSource.volume = pantVolume;
            pantSource.spatialBlend = 1f;
            pantSource.dopplerLevel = 0f;
            pantSource.rolloffMode = AudioRolloffMode.Logarithmic;
            pantSource.minDistance = 1.5f;
            pantSource.maxDistance = 12f;
        }

        ScheduleNextBark();
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.HasStarted)
        {
            if (pantSource != null && pantSource.isPlaying) pantSource.Stop();
            return;
        }
        if (pantSource != null && !pantSource.isPlaying) pantSource.Play();
        if (barkClip != null && barkSource != null && Time.time >= nextBarkTime)
        {
            barkSource.PlayOneShot(barkClip, barkVolume);
            ScheduleNextBark();
        }
    }

    void ScheduleNextBark()
    {
        nextBarkTime = Time.time + Random.Range(barkInterval.x, barkInterval.y);
    }

    void LateUpdate()
    {
        if (GameManager.Instance != null && !GameManager.Instance.HasStarted) return;
        // Stop if no player is assigned
        if (player == null)
        {
            return;
        }

        // Calculate the dog's target position
        Vector3 targetPosition =
            player.position
            - player.forward * followDistance
            + player.right * sideOffset;

        bool shouldRun = (targetPosition - transform.position).sqrMagnitude > 0.12f;
        if (dogAnimator != null && shouldRun != playingRunAnimation)
        {
            playingRunAnimation = shouldRun;
            string state = shouldRun ? "A_CartoonAnimal-Dog_Run" : "A_CartoonAnimal-Dog_Idle";
            dogAnimator.CrossFadeInFixedTime(state, 0.15f);
        }

        // Keep the dog at the chosen ground height
        targetPosition.y = groundHeight;

        // Smoothly move the dog toward the target
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSpeed * Time.deltaTime
        );

        // Make the dog face the player
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }
}
