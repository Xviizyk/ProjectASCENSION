using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;
using System.Collections.Generic;

public class WeaponRanged : Weapon
{
    [Header("Enums")]
    [SerializeField] private Rarity rarity = Rarity.Normal;
    [SerializeField] private Modification modification = Modification.New;
    [SerializeField] private Effect effect = Effect.None;
    [Header("Name and description")]
    [SerializeField] private string weaponName;
    [SerializeField] private string weaponDescription;
    [Header("Characteristics")]
    [SerializeField] private int maxAmmo;
    [SerializeField] private int maxAmount;
    [SerializeField] private float mana;
    [SerializeField] private float damage;
    [SerializeField] private float splashDamage;
    [SerializeField] private float fireRate;
    [SerializeField] private float multiplySpeed;
    [SerializeField] private float splashRadius;
    [SerializeField] private int currentAmmo;
    [Header("Prefabs")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform shootPoint;
    [Header("Projectile")]
    [SerializeField] private float reloadTime;
    [SerializeField] private float projectileLifeTime;
    [SerializeField] private float projectileSpeed;

    private float nextFireTime;

    public override Rarity WeaponRarity => rarity;
    public override Modification WeaponModification => modification;
    public override Effect WeaponEffect => effect;
    public override int MaxAmmo => maxAmmo;
    public override float Mana => mana;
    public override float Damage => damage;
    public override float SplashDamage => splashDamage;
    public override int MaxAmount => maxAmount;
    public override string WeaponDescription => weaponDescription;
    public override string WeaponName => weaponName; 
    public override float ReloadTime => reloadTime;
    public override float ProjectileLifeTime => projectileLifeTime;
    public override float ProjectileSpeed => projectileSpeed;

    public InputActionAsset generalActions; 
    private InputActionMap computerPlayerActions;
    private InputAction shootAction;
    private InputAction aimAction;

    private bool isShooting;

    public override float Firerate
    {
        get => fireRate;
        set => fireRate = Mathf.Max(0.01f, value);
    }

    public override float MultiplySpeed
    {
        get => multiplySpeed;
        set => multiplySpeed = Mathf.Max(0.1f, value);
    }

    public override float SplashRadius
    {
        get => splashRadius;
        set => splashRadius = Mathf.Max(0f, value);
    }

    private void OnEnable()
    {
        computerPlayerActions = generalActions.FindActionMap("PC");
        shootAction = computerPlayerActions.FindAction("LMB");
        aimAction = computerPlayerActions.FindAction("RMB");
        shootAction.started += _ => isShooting = true;
        shootAction.canceled += _ => isShooting = false;
        
        shootAction.Enable();
        aimAction.Enable();
    }

    private void OnDisable()
    {
        shootAction.Disable();
        aimAction.Disable();
    }

    private void Awake()
    {
        currentAmmo = maxAmmo;
    }

    private void Update()
    {
        if (isShooting)
        {
            ShootHandler();
        }
        Rotate();
    }

    public override void Rotate()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;
        Vector3 direction = mousePos - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public override void ShootHandler()
    {
        if (Time.time < nextFireTime)
            return;

        if (currentAmmo <= 0)
        {
            StartCoroutine(ReloadHandler());
            return;
        }

        ShootAiming();
        SpawnProjectile();

        currentAmmo--;
        nextFireTime = Time.time + Firerate / MultiplySpeed;
    }

    public override void ShootAiming()
    {
        return;
    }

    public override void SpawnProjectile()
    {
        if (projectilePrefab == null || shootPoint == null) return;

        GameObject projectile = Instantiate(
            projectilePrefab,
            shootPoint.position,
            shootPoint.rotation
        );

        RangedProjectile proj = projectile.GetComponent<RangedProjectile>();
        if (proj != null)
        {
            proj.Init(Damage, SplashDamage, SplashRadius, ProjectileLifeTime, ProjectileSpeed);
        }
    }

    public override IEnumerator ReloadHandler()
    {
        yield return new WaitForSeconds(reloadTime);
        currentAmmo = maxAmmo;
    }
}
