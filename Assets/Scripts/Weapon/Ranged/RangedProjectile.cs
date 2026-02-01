using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public class RangedProjectile : MonoBehaviour
{
    [Header("Ignored tags")]
    public List<string> tags;
    [Header("Stored data")]
    [SerializeField] private float lifeTime;
    [SerializeField] private float speed;
    [SerializeField] private float damage;
    [SerializeField] private float splashDamage;
    [SerializeField] private float splashRadius;
    [Header("Other data")]
    [SerializeField] private List<Sprite> splashSprites;
    [SerializeField] private SpriteRenderer sprRend; 
    [SerializeField] private bool triggered;

    public void Init(float dmg, float splDmg, float splRad, float prjLF, float prjSp)
    {
        Transform spriteSplash = transform.Find("splashSprite");
        sprRend = spriteSplash.GetComponent<SpriteRenderer>();
        sprRend.sprite = null;
        damage = dmg;
        splashDamage = splDmg;
        splashRadius = splRad;
        lifeTime = prjLF;
        speed = prjSp;

        Vector3 splVct = spriteSplash.localScale;
        splVct.x = splRad*2;
        splVct.y = splRad*2;
        spriteSplash.localScale = splVct;
    }

    void Update()
    {
        if (!triggered) transform.Translate(Vector2.right * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (tags.Contains(collision.tag)) return;

        triggered = true;

        Transform bulletSprite = transform.Find("bulletSprite");
        SpriteRenderer bltRend = bulletSprite.transform.GetComponent<SpriteRenderer>();
        bltRend.sprite = null;

        if (splashRadius > 0)
        {
            StartCoroutine(showSplashSprites());
            SplashDamageHandler();
        }

        else {
            SingleDamageHandler(collision);
            Destroy(gameObject);
        }
        Destroy(gameObject, lifeTime);
    }

    IEnumerator showSplashSprites()
    {
        triggered = true;
        for (int i = 0; i < splashSprites.Count; i++ )
        {
            sprRend.sprite = splashSprites[i];
            yield return new WaitForSeconds(30/splashSprites.Count);
        }
        Destroy(gameObject);
    }

    private void SingleDamageHandler(Collider2D target)
    {
        target.GetComponent<Health>()?.TakeDamage(damage);
    }

    private void SplashDamageHandler()
    {
        Collider2D[] targets = Physics2D.OverlapCircleAll(transform.position, splashRadius);
        foreach (var trgt in targets)
        {
            trgt.GetComponent<Health>()?.TakeDamage(splashDamage);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, splashRadius);
    }
}