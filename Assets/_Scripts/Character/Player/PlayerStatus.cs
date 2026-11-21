
using System;
using UnityEngine;


/// <summary> Handles <see cref="Player"/> status such as their health. </summary>
public class PlayerStatus : MonoBehaviour
{
    public int Health => health;
    public int MaxHealth => maxHealth;

    public bool Dead => health <= 0;

    public event Action<int> OnDamaged
    {
        add { onDamaged += value; }
        remove { onDamaged -= value; }
    }

    internal bool Initialized { get; private set; } = true;


    private int health = 3;
    private int maxHealth = 3;
    private Action<int> onDamaged = null;



    #region Public Methods ======================================================================== Public Methods

    public void Heal(int healingAmount)
    {
        if (healingAmount <= 0 || health <= 0) { return; }

        health = Mathf.Min(health + healingAmount, maxHealth);
    }

    public void Damage(int damageAmount)
    {
        if (damageAmount <= 0 || Dead) { return; }

        health = Mathf.Max(health - damageAmount, 0);
        onDamaged?.Invoke(health);
    }

    #endregion Public Methods
}
