using System.Collections;
using UnityEngine;

public class MonsterHealth : MonoBehaviour
{
    [Min(1)] public int maxHealth = 5;
    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    private bool defeated;
    private Renderer[] hitRenderers;
    private Material[][] hitMaterials;
    private Color[][] originalBaseColors;
    private Color[][] originalEmissionColors;
    private bool[][] hasBaseColor;
    private bool[][] hasEmissionColor;
    private AudioSource hitAudio;
    private AudioClip hitClip;
    private Coroutine flashRoutine;

    private void Start() => ResetHealth();

    public void ResetHealth()
    {
        CurrentHealth = maxHealth;
        defeated = false;
    }

    public void TakeDamage(int damage)
    {
        if (defeated || damage <= 0) return;
        CurrentHealth = Mathf.Max(CurrentHealth - damage, 0);
        PlayHitFeedback();
        if (CurrentHealth > 0) return;

        defeated = true;
        if (GameManager.Instance != null) GameManager.Instance.MonsterDefeated();
        StartCoroutine(HideAfterImpact());
    }

    private IEnumerator HideAfterImpact()
    {
        yield return new WaitForSeconds(0.24f);
        gameObject.SetActive(false);
    }

    private void PlayHitFeedback()
    {
        CacheHitMaterials();
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashMaterials());

        if (hitAudio == null)
        {
            hitAudio = GetComponent<AudioSource>();
            if (hitAudio == null) hitAudio = gameObject.AddComponent<AudioSource>();
            hitAudio.playOnAwake = false;
            hitAudio.spatialBlend = 1f;
            hitAudio.minDistance = 3f;
            hitAudio.maxDistance = 32f;
            hitClip = Resources.Load<AudioClip>("ProjectileMagicSound");
        }
        if (hitClip != null) hitAudio.PlayOneShot(hitClip, 0.75f);

        HollowwoodEffects.Burst(transform.position + Vector3.up, new Color(0.35f, 0.8f, 1f), 26, 0.9f, 3.5f);
    }

    private void CacheHitMaterials()
    {
        if (hitMaterials != null) return;
        hitRenderers = GetComponentsInChildren<Renderer>(true);
        hitMaterials = new Material[hitRenderers.Length][];
        originalBaseColors = new Color[hitRenderers.Length][];
        originalEmissionColors = new Color[hitRenderers.Length][];
        hasBaseColor = new bool[hitRenderers.Length][];
        hasEmissionColor = new bool[hitRenderers.Length][];

        for (int r = 0; r < hitRenderers.Length; r++)
        {
            hitMaterials[r] = hitRenderers[r].materials;
            originalBaseColors[r] = new Color[hitMaterials[r].Length];
            originalEmissionColors[r] = new Color[hitMaterials[r].Length];
            hasBaseColor[r] = new bool[hitMaterials[r].Length];
            hasEmissionColor[r] = new bool[hitMaterials[r].Length];
            for (int m = 0; m < hitMaterials[r].Length; m++)
            {
                Material material = hitMaterials[r][m];
                hasBaseColor[r][m] = material.HasProperty("_BaseColor") || material.HasProperty("_Color");
                hasEmissionColor[r][m] = material.HasProperty("_EmissionColor");
                if (material.HasProperty("_BaseColor")) originalBaseColors[r][m] = material.GetColor("_BaseColor");
                else if (material.HasProperty("_Color")) originalBaseColors[r][m] = material.GetColor("_Color");
                if (hasEmissionColor[r][m]) originalEmissionColors[r][m] = material.GetColor("_EmissionColor");
            }
        }
    }

    private IEnumerator FlashMaterials()
    {
        Color flashColor = new Color(0.48f, 0.82f, 1f);
        for (int r = 0; r < hitMaterials.Length; r++)
        {
            for (int m = 0; m < hitMaterials[r].Length; m++)
            {
                Material material = hitMaterials[r][m];
                if (material == null) continue;
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.Lerp(originalBaseColors[r][m], flashColor, 0.72f));
                else if (material.HasProperty("_Color")) material.SetColor("_Color", Color.Lerp(originalBaseColors[r][m], flashColor, 0.72f));
                if (hasEmissionColor[r][m])
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", flashColor * 1.8f);
                }
            }
        }

        yield return new WaitForSeconds(0.14f);

        for (int r = 0; r < hitMaterials.Length; r++)
        {
            for (int m = 0; m < hitMaterials[r].Length; m++)
            {
                Material material = hitMaterials[r][m];
                if (material == null) continue;
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", originalBaseColors[r][m]);
                else if (material.HasProperty("_Color")) material.SetColor("_Color", originalBaseColors[r][m]);
                if (hasEmissionColor[r][m]) material.SetColor("_EmissionColor", originalEmissionColors[r][m]);
            }
        }
        flashRoutine = null;
    }
}
