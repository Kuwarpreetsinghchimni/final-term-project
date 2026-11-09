using UnityEngine;

public class MagicProjectile : MonoBehaviour
{
    public float speed = 22f;
    public int damage = 1;
    public float lifetime = 3.5f;

    private Rigidbody body;
    private GameObject lightningVisual;
    private LineRenderer lightningGlow;
    private LineRenderer lightningCore;
    private Material glowMaterial;
    private Material coreMaterial;
    private float nextBoltRefresh;
    private const int BoltPointCount = 9;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.useGravity = false;
            body.isKinematic = true;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        Renderer projectileRenderer = GetComponent<Renderer>();
        if (projectileRenderer != null) projectileRenderer.enabled = false;

        GameObject lightningPrefab = Resources.Load<GameObject>("ProjectileLightning");
        if (lightningPrefab != null)
        {
            lightningVisual = Instantiate(lightningPrefab, transform);
            lightningVisual.transform.localPosition = Vector3.zero;
            lightningVisual.transform.localRotation = Quaternion.identity;
            // The projectile prefab is scaled to 0.3, so compensate here to keep
            // the imported effect at a readable world size.
            lightningVisual.transform.localScale = Vector3.one * 3f;
        }

        CreateVisibleLightningBolt();
    }

    private void Start() => Destroy(gameObject, lifetime);

    private void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
        if (Time.time >= nextBoltRefresh)
        {
            RefreshLightningBolt();
            nextBoltRefresh = Time.time + 0.045f;
        }
    }

    private void CreateVisibleLightningBolt()
    {
        if (lightningVisual == null)
        {
            lightningVisual = new GameObject("Visible Lightning Bolt");
            lightningVisual.transform.SetParent(transform, false);
            lightningVisual.transform.localScale = Vector3.one * 3f;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Sprites/Default");
        if (shader == null) return;

        glowMaterial = CreateBoltMaterial(shader, new Color(0.08f, 0.55f, 1f, 1f));
        coreMaterial = CreateBoltMaterial(shader, new Color(0.82f, 0.98f, 1f, 1f));
        lightningGlow = CreateBoltLine("Lightning glow", glowMaterial, 0.24f);
        lightningCore = CreateBoltLine("Lightning core", coreMaterial, 0.085f);

        Light boltLight = lightningVisual.AddComponent<Light>();
        boltLight.type = LightType.Point;
        boltLight.color = new Color(0.12f, 0.55f, 1f);
        boltLight.intensity = 2.4f;
        boltLight.range = 2.5f;
        boltLight.shadows = LightShadows.None;
        RefreshLightningBolt();
    }

    private static Material CreateBoltMaterial(Shader shader, Color color)
    {
        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 2f);
        }
        return material;
    }

    private LineRenderer CreateBoltLine(string objectName, Material material, float width)
    {
        GameObject lineObject = new GameObject(objectName);
        lineObject.transform.SetParent(lightningVisual.transform, false);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.alignment = LineAlignment.View;
        line.positionCount = BoltPointCount;
        line.startWidth = width;
        line.endWidth = width * 0.55f;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.material = material;
        return line;
    }

    private void RefreshLightningBolt()
    {
        if (lightningGlow == null || lightningCore == null) return;

        for (int i = 0; i < BoltPointCount; i++)
        {
            float t = i / (BoltPointCount - 1f);
            float jitter = (i == 0 || i == BoltPointCount - 1) ? 0f : 1f;
            Vector3 point = new Vector3(
                Random.Range(-0.22f, 0.22f) * jitter,
                Random.Range(-0.22f, 0.22f) * jitter,
                Mathf.Lerp(-1.15f, 1.05f, t));
            lightningGlow.SetPosition(i, point);
            lightningCore.SetPosition(i, point * 0.72f);
        }
    }

    private void OnDestroy()
    {
        if (glowMaterial != null) Destroy(glowMaterial);
        if (coreMaterial != null) Destroy(coreMaterial);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) return;
        MonsterHealth monster = other.GetComponentInParent<MonsterHealth>();
        if (monster != null)
        {
            monster.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger) Destroy(gameObject);
    }
}
