using UnityEngine;
using System.Collections;
using Player.MovementLogic;

public class WeaponShooter : MonoBehaviour
{
    public enum WeaponMode { Melee, AK47, Shotgun }
    
    [SerializeField] private GameObject AK47;
    [SerializeField] private GameObject Shotgun;
    [SerializeField] private GameObject arms;
    [SerializeField] public Config config;
    [SerializeField] private Stats stats;
    [SerializeField] private Transform firePoint1;
    [SerializeField] private Transform firePoint2;
    [SerializeField] public WeaponMode _currentMode = WeaponMode.AK47;
    [SerializeField] private Coroutine _actionCoroutine = null;
    
    [SerializeField] private Vector2 _currentGunDirection;
    
    private bool _isCharacterGrounded = true;
    private Movement _movement;

    void Awake()
    {
        _movement = GetComponent<Movement>();
    }
    private void OnEnable()
    {
        if (_movement != null)
            _movement.OnGroundedStateChanged += isGrounded => _isCharacterGrounded = isGrounded;
    }

    private void Start() =>
        SetWeaponActive(_currentMode);

    void Update()
    {
        HandleWeaponSwitch();
        HandleShootInput();
        HandleReloadInput();
        UpdateWeaponDirection();
    }
    
    private void UpdateWeaponDirection()
    {
        GameObject activeWeapon = GetActiveWeaponObject();
        Transform firePoint = GetActiveFirePoint();

        if (activeWeapon != null)
        {
            RotateWeapon(activeWeapon, firePoint);
            _currentGunDirection = activeWeapon.transform.right;
        }
    }

    private void HandleWeaponSwitch()
    {
        var newMode = _currentMode;

        if (Input.GetKeyDown(KeyCode.Alpha1)) newMode = WeaponMode.Melee;
        else if (Input.GetKeyDown(KeyCode.Alpha2)) newMode = WeaponMode.AK47;
        else if (Input.GetKeyDown(KeyCode.Alpha3)) newMode = WeaponMode.Shotgun;

        if (newMode == _currentMode) return;
        
        StopCurrentAction();
        _currentMode = newMode;
        SetWeaponActive(_currentMode);
    }
    
    private void HandleShootInput()
    {
        if (_currentMode == WeaponMode.Melee) return;
        
        if (Input.GetMouseButtonDown(0) && _actionCoroutine == null)
        {
            _actionCoroutine = StartCoroutine(ShootingLoop());
        }
        else if (Input.GetMouseButtonUp(0) && _actionCoroutine != null)
        {
        }
    }

    private void HandleReloadInput()
    {
        if (!Input.GetKeyDown(KeyCode.R)) return;
        
        StopCurrentAction();
            
        if (CanReload(_currentMode))
        {
            _actionCoroutine = StartCoroutine(ReloadAction());
        }
    }

    private IEnumerator ShootingLoop()
    {
        while (Input.GetMouseButton(0))
            if (_currentMode == WeaponMode.AK47)
            {
                if (stats.ammo1 > 0)
                {
                    ShootRifle();
                    stats.ammo1--;
                    yield return new WaitForSeconds(config.AK47_FireRate);
                }
                else
                {
                    yield return StartCoroutine(ReloadAction());
                    break;
                }
            }
            else if (_currentMode == WeaponMode.Shotgun)
            {
                if (stats.ammo2 > 0)
                {
                    ShootShotgun();
                    stats.ammo2--;
                    yield return new WaitForSeconds(config.Shotgun_FireRate);
                }
                else
                {
                    yield return StartCoroutine(ReloadAction());
                    break;
                }
            }
            else
            { 
                break;
            }
        _actionCoroutine = null;
    }

    private IEnumerator ReloadAction()
    {
        var reloadTime = _currentMode == WeaponMode.AK47 ? config.AK47_ReloadTime : config.Shotgun_ReloadTime;
        var maxAmmo = _currentMode == WeaponMode.AK47 ? config.AK47_MaxAmmo : config.Shotgun_MaxAmmo;

        if (_currentMode == WeaponMode.AK47) stats.ammo1 = 0;
        else stats.ammo2 = 0;

        yield return new WaitForSeconds(reloadTime);

        if (_currentMode == WeaponMode.AK47) stats.ammo1 = maxAmmo;
        else stats.ammo2 = maxAmmo;
        
        _actionCoroutine = null;
    }

    private void ShootRifle()
    {
        var spread = config.AK47_BaseSpread;
        if (!_isCharacterGrounded) spread *= 3f;
        
        Vector2 dir = Quaternion.Euler(0, 0, Random.Range(-spread, spread)) * _currentGunDirection;
        
        var bulletSpawnOffset = firePoint1.right * 0.1f; 
        
        FireBullet(dir, config.AK47_BulletPrefab, config.GlobalBulletSpeed, firePoint1, bulletSpawnOffset);
    }

    private void ShootShotgun()
    {
        for (var i = 0; i < config.Shotgun_PelletCount; i++)
        {
            var spread = Random.Range(-config.Shotgun_MaxSpread, config.Shotgun_MaxSpread);
            
            Vector2 dir = Quaternion.Euler(0, 0, spread) * _currentGunDirection;
            
            var offset = firePoint2.right * config.Shotgun_XOffset;
            
            FireBullet(dir, config.Shotgun_BulletPrefab, config.GlobalBulletSpeed, firePoint2, offset);
        }
    }

    private void FireBullet(Vector2 direction, GameObject bulletPrefab, float bulletSpeed, Transform firePoint, Vector3 offset = default)
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position + offset, firePoint.rotation);
        Rigidbody2D rbBullet = bullet.GetComponent<Rigidbody2D>();
        
        if (rbBullet != null)
        {
             rbBullet.linearVelocity = direction.normalized * bulletSpeed;
        }
        Destroy(bullet, 5f);
    }

    private void StopCurrentAction()
    {
        if (_actionCoroutine != null)
            StopCoroutine(_actionCoroutine);
        
        _actionCoroutine = null;
    }

    private bool CanReload(WeaponMode mode) =>
        mode switch
        {
            WeaponMode.AK47 => stats.ammo1 < config.AK47_MaxAmmo,
            WeaponMode.Shotgun => stats.ammo2 < config.Shotgun_MaxAmmo,
            _ => false
        };

    private GameObject GetActiveWeaponObject() =>
        _currentMode switch
        {
            WeaponMode.AK47 => AK47,
            WeaponMode.Shotgun => Shotgun,
            _ => null
        };
    
    private Transform GetActiveFirePoint() =>
        _currentMode switch
        {
            WeaponMode.AK47 => firePoint1,
            WeaponMode.Shotgun => firePoint2,
            _ => null
        };

    void SetWeaponActive(WeaponMode mode)
    {
        AK47.SetActive(mode == WeaponMode.AK47);
        Shotgun.SetActive(mode == WeaponMode.Shotgun);
        arms.SetActive(mode == WeaponMode.Melee);
    }

    private static void RotateWeapon(GameObject weapon, Transform firePoint)
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 weaponPos = weapon.transform.position;
        var direction = mouseWorldPos - weaponPos;
        var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        weapon.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        
        var localScale = weapon.transform.localScale;
        
        var flipY = angle is > 90f or < -90f ? -Mathf.Abs(localScale.y) : Mathf.Abs(localScale.y);
        
        weapon.transform.localScale = new Vector3(-Mathf.Abs(localScale.x), flipY, localScale.z);
    }
}
