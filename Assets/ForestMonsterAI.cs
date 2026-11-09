using UnityEngine;

public class ForestMonsterAI : MonoBehaviour
{
    [Header("Guardian Model")]
    public GameObject golemPrefab;
    public Texture2D golemAlbedo;
    public float moveSpeed = 1.8f;
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;
    public int attackDamage = 1;
    public AudioClip attackSound;
    [Range(0f, 1f)] public float attackVolume = 0.8f;

    private Transform player;
    private GameManager gameManager;
    private float nextAttackTime;
    private Vector3 homePosition;
    private bool appearanceBuilt;
    private Animator golemAnimator;
    private string currentAnimation;
    private float animationLockUntil;
    private AudioSource attackAudioSource;

    private void Awake()
    {
        SetupAttackAudio();
        BuildGuardianAppearance();
    }

    public void Initialize(Transform playerTarget, GameManager manager)
    {
        player = playerTarget;
        gameManager = manager;
        homePosition = transform.position;
        SetupAttackAudio();
        // Ensure the imported guardian is ready as soon as the final relic
        // activates this object, even if its activation order changes.
        BuildGuardianAppearance();
    }

    private void OnEnable()
    {
        SetupAttackAudio();
        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }
        if (gameManager == null) gameManager = GameManager.Instance;
        homePosition = transform.position;
    }


    private void SetupAttackAudio()
    {
        if (attackAudioSource == null) attackAudioSource = GetComponent<AudioSource>();
        if (attackAudioSource == null) attackAudioSource = gameObject.AddComponent<AudioSource>();
        attackAudioSource.playOnAwake = false;
        attackAudioSource.spatialBlend = 1f;
        attackAudioSource.dopplerLevel = 0f;
        attackAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        attackAudioSource.minDistance = 3f;
        attackAudioSource.maxDistance = 30f;
    }

    private void ApplySupportedGolemMaterials(GameObject golem)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Texture");
        if (shader == null) return;

        foreach (Renderer modelRenderer in golem.GetComponentsInChildren<Renderer>(true))
        {
            Material[] originals = modelRenderer.sharedMaterials;
            Material[] converted = new Material[originals.Length];
            for (int i = 0; i < originals.Length; i++)
            {
                Material source = originals[i];
                Material material = new Material(shader);
                Texture albedo = source != null ? source.mainTexture : null;
                if (albedo == null) albedo = golemAlbedo;
                if (albedo != null)
                {
                    if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", albedo);
                    if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", albedo);
                }

                Color tint = Color.white;
                if (source != null && source.HasProperty("_Color")) tint = source.GetColor("_Color");
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
                if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
                converted[i] = material;
            }
            modelRenderer.sharedMaterials = converted;
        }
    }

    private void BuildGuardianAppearance()
    {
        if (appearanceBuilt) return;
        appearanceBuilt = true;

        if (golemPrefab != null)
        {
            Renderer oldRenderer = GetComponent<Renderer>();
            if (oldRenderer != null) oldRenderer.enabled = false;

            GameObject golem = Instantiate(golemPrefab, transform);
            golem.name = "Realistic Rock Golem";
            golem.transform.localPosition = Vector3.zero;
            golem.transform.localRotation = Quaternion.identity;
            // The prefab's mesh is authored at a very small local scale.
            // A near unit scale makes the guardian readable beside the player.
            golem.transform.localScale = Vector3.one * 0.85f;
            ApplySupportedGolemMaterials(golem);
            golemAnimator = golem.GetComponentInChildren<Animator>();
            PlayAnimation("Idle");
            return;
        }

        Renderer body = GetComponent<Renderer>();
        if (body != null)
        {
            Material material = body.material;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(0.12f, 0.24f, 0.16f));
            else if (material.HasProperty("_Color")) material.color = new Color(0.12f, 0.24f, 0.16f);
        }

        Transform details = new GameObject("Guardian Details").transform;
        details.SetParent(transform, false);
        AddEye(details, new Vector3(-0.22f, 0.22f, 0.36f));
        AddEye(details, new Vector3(0.22f, 0.22f, 0.36f));
        AddHorn(details, new Vector3(-0.34f, 0.57f, 0.02f), -24f);
        AddHorn(details, new Vector3(0.34f, 0.57f, 0.02f), 24f);
    }

    private static void AddEye(Transform parent, Vector3 localPosition)
    {
        GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = "Amber Eye";
        eye.transform.SetParent(parent, false);
        eye.transform.localPosition = localPosition;
        eye.transform.localScale = Vector3.one * 0.12f;
        Collider collider = eye.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        Renderer renderer = eye.GetComponent<Renderer>();
        Material material = CreateLitMaterial(new Color(1f, 0.62f, 0.18f));
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(0.7f, 0.21f, 0.025f));
        }
        renderer.material = material;
    }

    private static void AddHorn(Transform parent, Vector3 localPosition, float tilt)
    {
        GameObject horn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        horn.name = "Bark Antler";
        horn.transform.SetParent(parent, false);
        horn.transform.localPosition = localPosition;
        horn.transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
        horn.transform.localScale = new Vector3(0.09f, 0.48f, 0.09f);
        Collider collider = horn.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        horn.GetComponent<Renderer>().material = CreateLitMaterial(new Color(0.19f, 0.12f, 0.065f));
    }

    private static Material CreateLitMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        else if (material.HasProperty("_Color")) material.color = color;
        return material;
    }

    private void Update()
    {
        if (player == null || (gameManager != null && (!gameManager.MonsterAwake || gameManager.IsComplete || gameManager.IsGameOver))) return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;
        if (distance > attackRange)
        {
            Vector3 next = Vector3.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);
            next.y = homePosition.y;
            transform.position = next;
            PlayAnimation("Walk");
        }
        else if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackCooldown;
            PlayAnimation("Attack01", 0.7f);
            if (attackSound != null && attackAudioSource != null)
                attackAudioSource.PlayOneShot(attackSound, attackVolume);
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null) health.TakeDamage(attackDamage);
        }
        else if (Time.time >= animationLockUntil) PlayAnimation("Idle");

        if (toPlayer.sqrMagnitude > 0.01f)
        {
            Quaternion face = Quaternion.LookRotation(toPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, face, 5f * Time.deltaTime);
        }
    }

    private void PlayAnimation(string stateName, float lockSeconds = 0f)
    {
        if (golemAnimator == null) return;
        if (currentAnimation != stateName)
        {
            golemAnimator.CrossFadeInFixedTime(stateName, 0.18f);
            currentAnimation = stateName;
        }
        animationLockUntil = Mathf.Max(animationLockUntil, Time.time + lockSeconds);
    }
}
