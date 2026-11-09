using UnityEngine;

public class BridgePiece : MonoBehaviour
{
    public float hoverHeight = 0.18f;
    public float hoverSpeed = 1.8f;
    public float spinSpeed = 32f;

    private Vector3 homePosition;
    private Renderer pieceRenderer;
    private Material glowMaterial;
    private Light pieceGlow;

    private void Start()
    {
        homePosition = transform.position;
        pieceRenderer = GetComponent<Renderer>();
        if (pieceRenderer != null)
        {
            glowMaterial = pieceRenderer.material;
            if (glowMaterial.HasProperty("_BaseColor")) glowMaterial.SetColor("_BaseColor", new Color(0.45f, 0.82f, 0.7f));
            else if (glowMaterial.HasProperty("_Color")) glowMaterial.color = new Color(0.45f, 0.82f, 0.7f);
            if (glowMaterial.HasProperty("_EmissionColor"))
            {
                glowMaterial.EnableKeyword("_EMISSION");
                glowMaterial.SetColor("_EmissionColor", new Color(0.12f, 0.9f, 0.62f));
            }
        }

        pieceGlow = gameObject.AddComponent<Light>();
        pieceGlow.type = LightType.Point;
        pieceGlow.color = new Color(0.24f, 1f, 0.72f);
        pieceGlow.intensity = 1.5f;
        pieceGlow.range = 3.2f;
        pieceGlow.shadows = LightShadows.None;
        HollowwoodEffects.CreateAura(transform, pieceGlow.color);
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        Vector3 position = homePosition;
        position.y += Mathf.Sin(Time.time * hoverSpeed + transform.position.x) * hoverHeight;
        transform.position = position;

        float pulse = 0.78f + Mathf.Sin(Time.time * 3.2f + homePosition.z) * 0.22f;
        if (pieceGlow != null) pieceGlow.intensity = 1.2f + pulse * 0.9f;
        if (glowMaterial != null && glowMaterial.HasProperty("_EmissionColor"))
            glowMaterial.SetColor("_EmissionColor", new Color(0.12f, 0.9f, 0.62f) * pulse);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (GameManager.Instance != null) GameManager.Instance.CollectPiece();
        Destroy(gameObject);
    }
}
