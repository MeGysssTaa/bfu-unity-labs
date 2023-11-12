using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity : MonoBehaviour
{
    [SerializeField] protected float maxHealth = 100f;
    protected float health;
    
    [SerializeField] private AudioClip highHealthHurtSound;
    [SerializeField] private AudioClip mediumHealthHurtSound;
    [SerializeField] private AudioClip lowHealthHurtSound;
    [SerializeField] private AudioClip deathSound;

    protected bool isDeadOrDying;

    public virtual void Awake()
    {
        health = maxHealth;
    }

    public virtual void Hurt(float damage)
    {
        health -= damage;
        PlayHurtSound();
        
        if (ShouldDie)
        {
            Die();
        }
        else
        {
            PlayHurtAnimation();
        }
    }

    protected virtual void PlayHurtAnimation()
    {
        
    }

    protected virtual void PlayDeathAnimation()
    {
        
    }

    private bool ShouldDie => health < 1f;

    protected virtual void PlayHurtSound()
    {
        var sound = highHealthHurtSound;
        if (health <= 0.6f * maxHealth && mediumHealthHurtSound != null) sound = mediumHealthHurtSound;
        if (health <= 0.3f * maxHealth && lowHealthHurtSound != null) sound = lowHealthHurtSound;
        if (ShouldDie && deathSound != null) sound = deathSound;
        
        AudioSource.PlayClipAtPoint(sound, transform.position);
    }

    protected virtual void Die()
    {
        isDeadOrDying = true;
        PlayDeathAnimation();
    }
}
