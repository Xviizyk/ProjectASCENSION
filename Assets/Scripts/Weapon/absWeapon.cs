using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public abstract class Weapon : MonoBehaviour
{
    public enum Rarity { 
        Common, 
        Normal, 
        Super_Rare, 
        Rare, 
        Exalted, 
        Mythical, 
        Super_Mythical, 
        Legendary, 
        Cosmic 
    }

    public enum Modification { 
        None, 
        Broken, 
        Repaired, 
        New, 
        Reinforced, 
        Weakened, 
        Military, 
        Mechanical, 
        Cleared, 
        Cast, 
        SelfMade,
        Training, 
        SpecialMade,
        Custom
    }

    public enum Effect { 
        None, 
        Vampiric, 
        Fire, 
        Instakill, 
        Explosive, 
        Poison 
    }

    public abstract float ReloadTime { get; }
    public abstract float ProjectileLifeTime { get; }
    public abstract float ProjectileSpeed { get; }
    public abstract Rarity WeaponRarity { get; }
    public abstract Modification WeaponModification { get; }
    public abstract Effect WeaponEffect { get; }
    public abstract int MaxAmmo { get; }
    public abstract float Mana { get; }
    public abstract float Damage { get; }
    public abstract float SplashDamage { get; }
    public abstract float Firerate { get; set; }
    public abstract float MultiplySpeed { get; set; }
    public abstract int MaxAmount { get; }
    public abstract float SplashRadius { get; set; }
    public abstract string WeaponDescription { get; }
    public abstract string WeaponName { get; }

    public abstract IEnumerator ReloadHandler();
    public abstract void ShootHandler();
    public abstract void ShootAiming();
    public abstract void SpawnProjectile();
    public abstract void Rotate(); 
}
