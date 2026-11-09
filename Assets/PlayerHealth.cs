using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 5;
    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;

    private float invulnerableUntil;

    private void Start() => ResetHealth();

    public void ResetHealth()
    {
        CurrentHealth = maxHealth;
        invulnerableUntil = 0f;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || Time.time < invulnerableUntil || (GameManager.Instance != null && GameManager.Instance.IsGameOver)) return;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        invulnerableUntil = Time.time + 1f;
        if (CurrentHealth == 0 && GameManager.Instance != null) GameManager.Instance.PlayerDefeated();
    }
}
