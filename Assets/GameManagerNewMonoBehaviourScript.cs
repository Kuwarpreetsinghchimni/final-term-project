using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Min(1)] public int totalPieces = 3;
    public TMP_Text pieceCounter;
    public GameObject forestMonster;
    public GameObject restoredBridge;
    [Header("Imported fantasy bridge")]
    public GameObject importedBridgePrefab;

    public int CollectedPieces { get; private set; }
    public bool HasStarted { get; private set; }
    public bool MonsterAwake { get; private set; }
    public bool IsBridgeRestored { get; private set; }
    public bool IsComplete { get; private set; }
    public bool IsGameOver { get; private set; }
    public string StatusMessage { get; private set; }

    private GameHUD hud;
    private PlayerHealth playerHealth;
    private Transform playerTransform;
    private BoxCollider bridgeExit;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        PrepareImportedBridge();
        hud = GetComponent<GameHUD>();
        if (hud == null) hud = gameObject.AddComponent<GameHUD>();
    }

    private void PrepareImportedBridge()
    {
        if (importedBridgePrefab == null) return;
        if (restoredBridge == null)
        {
            restoredBridge = new GameObject("RestoredBridge");
            restoredBridge.transform.position = Vector3.zero;
        }
        if (restoredBridge.transform.Find("Imported Fantasy Bridge") != null) return;

        foreach (Transform child in restoredBridge.transform)
        {
            if (!child.name.StartsWith("BridgeSlab")) continue;
            child.gameObject.SetActive(false);
        }

        GameObject bridge = Instantiate(importedBridgePrefab, restoredBridge.transform);
        bridge.name = "Imported Fantasy Bridge";
        bridge.transform.localPosition = new Vector3(0f, -0.3f, 0f);
        bridge.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        bridge.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
        // Keep imported materials where they resolve. The bridge package's SBSAR
        // material can be unavailable on machines without the Substance importer,
        // so give its meshes a lit, textured fallback instead of a flat unlit tint.
        Shader bridgeShader = Shader.Find("Universal Render Pipeline/Lit");
        if (bridgeShader == null) bridgeShader = Shader.Find("Standard");
        if (bridgeShader == null) bridgeShader = Shader.Find("Legacy Shaders/Diffuse");
        Material bridgeMaterial = new Material(bridgeShader);
        Color stone = new Color(0.88f, 0.82f, 0.69f, 1f);
        if (bridgeMaterial.HasProperty("_Color")) bridgeMaterial.SetColor("_Color", stone);
        if (bridgeMaterial.HasProperty("_BaseColor")) bridgeMaterial.SetColor("_BaseColor", stone);
        if (bridgeMaterial.HasProperty("_Metallic")) bridgeMaterial.SetFloat("_Metallic", 0f);
        if (bridgeMaterial.HasProperty("_Glossiness")) bridgeMaterial.SetFloat("_Glossiness", 0.18f);
        if (bridgeMaterial.HasProperty("_Smoothness")) bridgeMaterial.SetFloat("_Smoothness", 0.18f);
        bridgeMaterial.mainTexture = CreateBridgeStoneTexture();
        foreach (Renderer bridgeRenderer in bridge.GetComponentsInChildren<Renderer>(true))
            bridgeRenderer.sharedMaterial = bridgeMaterial;
        GameManager nestedManager = bridge.GetComponent<GameManager>();
        if (nestedManager != null) Destroy(nestedManager);
        foreach (Collider bridgeCollider in bridge.GetComponentsInChildren<Collider>(true))
            bridgeCollider.enabled = false;
    }


    private static Texture2D CreateBridgeStoneTexture()
    {
        const int size = 256;
        const int columns = 8;
        const int rows = 8;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGB24, true);
        texture.name = "Bridge Stone Surface";
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Trilinear;
        Color[] pixels = new Color[size * size];
        float[] stoneTones = { 0.82f, 0.91f, 0.76f, 0.87f, 0.95f, 0.79f };
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int cellX = x * columns / size;
                int cellY = y * rows / size;
                float localX = (x * columns) % size / (float)size;
                float localY = (y * rows) % size / (float)size;
                bool mortar = localX < 0.035f || localY < 0.035f ||
                    (cellY % 2 == 1 && localX < 0.09f) || (cellY % 2 == 0 && localX > 0.91f);
                float tone = stoneTones[(cellX * 3 + cellY * 5) % stoneTones.Length];
                float grain = (Mathf.PerlinNoise(x * 0.085f, y * 0.085f) - 0.5f) * 0.11f;
                float shade = mortar ? 0.48f : Mathf.Clamp01(tone + grain);
                pixels[y * size + x] = new Color(0.63f * shade, 0.57f * shade, 0.45f * shade);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(true, false);
        return texture;
    }

    private void Start()
    {
        CollectedPieces = 0;
        HasStarted = false;
        MonsterAwake = false;
        IsBridgeRestored = false;
        IsComplete = false;
        IsGameOver = false;
        StatusMessage = "Find the three ancient relics hidden around the clearing.";

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth == null) playerHealth = player.AddComponent<PlayerHealth>();
            playerHealth.ResetHealth();
        }

        // The guardian is spawned only after the last relic is collected.
        if (forestMonster != null) forestMonster.SetActive(false);
        // Keep the imported bridge hidden until the guardian is defeated.
        if (restoredBridge != null) restoredBridge.SetActive(false);
        UpdateCounter();
    }

    public void CollectPiece()
    {
        if (!HasStarted || IsComplete || IsGameOver || CollectedPieces >= totalPieces) return;

        CollectedPieces++;
        UpdateCounter();

        if (CollectedPieces >= totalPieces)
        {
            MonsterAwake = true;
            StatusMessage = "The forest golem has emerged. Defeat it with your magic.";
            if (forestMonster != null)
            {
                Vector3 guardianScale = forestMonster.transform.localScale;
                forestMonster.transform.localScale = guardianScale * 0.08f;
                forestMonster.SetActive(true);
                ForestMonsterAI ai = forestMonster.GetComponent<ForestMonsterAI>();
                if (ai == null) ai = forestMonster.AddComponent<ForestMonsterAI>();
                ai.Initialize(GameObject.FindGameObjectWithTag("Player")?.transform, this);
                MonsterHealth health = forestMonster.GetComponent<MonsterHealth>();
                if (health != null) health.ResetHealth();
                StartCoroutine(EmergeGuardian(forestMonster.transform, guardianScale));
                HollowwoodEffects.Burst(forestMonster.transform.position + Vector3.up * 0.5f,
                    new Color(0.76f, 0.55f, 0.3f), 72, 1.8f, 2.2f);
            }
        }
        else
        {
            int remaining = totalPieces - CollectedPieces;
            StatusMessage = remaining == 1
                ? "One relic remains. The guardian is almost awake."
                : "Relic recovered. Keep searching the forest.";
        }
    }

    public void MonsterDefeated()
    {
        if (IsBridgeRestored || IsComplete) return;

        IsBridgeRestored = true;
        MonsterAwake = false;
        StatusMessage = "The guardian is at peace. Cross the restored bridge to finish.";

        if (restoredBridge != null)
        {
            restoredBridge.SetActive(true);
            EnableBridgeParts(restoredBridge.transform);
            HollowwoodEffects.LightRestoredBridge(restoredBridge.transform);
            CreateBridgeExit(restoredBridge.transform);
        }
    }

    public void StartGame()
    {
        if (!HasStarted && !IsGameOver && !IsComplete) HasStarted = true;
    }

    private void FinishGame()
    {
        if (IsComplete) return;
        IsComplete = true;
        StatusMessage = "Hollowwood is restored. You made it safely across the bridge.";
    }

    private void CreateBridgeExit(Transform bridge)
    {
        Renderer[] renderers = bridge.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0 || bridgeExit != null) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        bool alongX = bounds.size.x >= bounds.size.z;
        Vector3 axis = alongX ? Vector3.right : Vector3.forward;
        float length = alongX ? bounds.size.x : bounds.size.z;
        float sign = 1f;
        if (playerTransform != null)
        {
            float plusDistance = (bounds.center + axis * length * 0.5f - playerTransform.position).sqrMagnitude;
            float minusDistance = (bounds.center - axis * length * 0.5f - playerTransform.position).sqrMagnitude;
            sign = plusDistance >= minusDistance ? 1f : -1f;
        }

        GameObject exit = new GameObject("Bridge Exit Zone");
        exit.transform.position = bounds.center + axis * sign * (length * 0.5f + 3f);
        exit.transform.position = new Vector3(exit.transform.position.x, bounds.center.y + 1f, exit.transform.position.z);
        bridgeExit = exit.AddComponent<BoxCollider>();
        bridgeExit.isTrigger = true;
        bridgeExit.size = alongX ? new Vector3(4.5f, 6f, 9f) : new Vector3(9f, 6f, 4.5f);
    }

    private IEnumerator EmergeGuardian(Transform guardian, Vector3 fullScale)
    {
        float duration = 0.8f;
        float elapsed = 0f;
        while (elapsed < duration && guardian != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // A restrained overshoot makes the guardian feel weighty as it lands.
            float eased = 1f + 0.12f * Mathf.Sin(t * Mathf.PI) * (1f - t) - Mathf.Pow(1f - t, 3f);
            guardian.localScale = fullScale * eased;
            yield return null;
        }
        if (guardian != null) guardian.localScale = fullScale;
    }

    public void PlayerDefeated()
    {
        IsGameOver = true;
        StatusMessage = "You have fallen in the Hollowwood. Press R to try again.";
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void Update()
    {
        if (IsBridgeRestored && !IsComplete && bridgeExit != null && playerTransform != null && bridgeExit.bounds.Contains(playerTransform.position))
            FinishGame();

        if ((IsGameOver || IsComplete) && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            RestartGame();
    }

    private void EnableBridgeParts(Transform parent)
    {
        foreach (Transform part in parent)
        {
            if (part.name.StartsWith("BridgeSlab"))
            {
                part.gameObject.SetActive(false);
                continue;
            }
            part.gameObject.SetActive(true);
            foreach (Renderer partRenderer in part.GetComponents<Renderer>()) partRenderer.enabled = true;
            foreach (Collider partCollider in part.GetComponents<Collider>()) partCollider.enabled = true;
            EnableBridgeParts(part);
        }
    }

    private void UpdateCounter()
    {
        if (pieceCounter != null)
        {
            pieceCounter.text = "Relics " + CollectedPieces + " / " + totalPieces;
            pieceCounter.gameObject.SetActive(false);
        }
    }
}

internal static class HollowwoodEffects
{
    private static Material particleMaterial;

    public static void CreateAura(Transform parent, Color color)
    {
        GameObject aura = new GameObject("Relic Sparkle Aura");
        aura.SetActive(false);
        aura.transform.SetParent(parent, false);
        ParticleSystem particles = aura.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.playOnAwake = false;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 1.15f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.32f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.11f);
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 32;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 10f;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.24f;
        shape.radiusThickness = 0f;

        ConfigureParticleRenderer(particles, color);
        aura.SetActive(true);
        particles.Play();
    }

    public static void Burst(Vector3 position, Color color, int count, float size, float speed)
    {
        GameObject burst = new GameObject("Hollowwood Magic Burst");
        burst.SetActive(false);
        burst.transform.position = position;
        ParticleSystem particles = burst.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.45f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.035f, size * 0.09f);
        main.startColor = color;
        main.gravityModifier = 0.35f;
        main.maxParticles = Mathf.Max(16, count);

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(count, 1, 300)) });
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = size * 0.22f;
        shape.radiusThickness = 0.15f;

        ConfigureParticleRenderer(particles, color);
        burst.SetActive(true);
        particles.Play();
        Object.Destroy(burst, 3f);
    }

    public static void LightRestoredBridge(Transform bridge)
    {
        Transform oldLights = bridge.Find("Bridge Wayfinding Lights");
        if (oldLights != null) return;

        Renderer[] renderers = bridge.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

        Burst(bounds.center + Vector3.up * 0.25f, new Color(0.55f, 0.95f, 0.82f), 110, 2.5f, 2.8f);
        Transform lights = new GameObject("Bridge Wayfinding Lights").transform;
        lights.SetParent(bridge, true);
        Vector3 axis = bounds.size.x >= bounds.size.z ? Vector3.right : Vector3.forward;
        float length = Mathf.Max(2f, bounds.size.x >= bounds.size.z ? bounds.size.x : bounds.size.z);
        Color[] colors = { new Color(0.35f, 0.95f, 0.78f), new Color(1f, 0.72f, 0.38f), new Color(0.35f, 0.95f, 0.78f) };
        for (int i = 0; i < colors.Length; i++)
        {
            GameObject lightObject = new GameObject("Bridge Glow " + (i + 1));
            lightObject.transform.SetParent(lights, true);
            lightObject.transform.position = bounds.center + axis * ((i - 1) * length * 0.32f) + Vector3.up * 0.7f;
            Light bridgeLight = lightObject.AddComponent<Light>();
            bridgeLight.type = LightType.Point;
            bridgeLight.color = colors[i];
            bridgeLight.intensity = 1.35f;
            bridgeLight.range = 5f;
            bridgeLight.shadows = LightShadows.None;
        }
    }

    private static void ConfigureParticleRenderer(ParticleSystem particles, Color color)
    {
        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        if (particleMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                particleMaterial = new Material(shader);
                if (particleMaterial.HasProperty("_BaseColor")) particleMaterial.SetColor("_BaseColor", color);
                if (particleMaterial.HasProperty("_Color")) particleMaterial.SetColor("_Color", color);
            }
        }
        if (particleMaterial != null) renderer.material = particleMaterial;
    }
}
