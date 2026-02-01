using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Health : MonoBehaviour
{ 
    public float health = 100f;
    public float bleeding;
    public float bleedingRate;
    public float bleedingPerOne;

    public void TakeDamage(float damage)
    {
        health -= damage;
    }

    public void AddBleeding(float bleed)
    {
        bleeding += bleed;
    }

    void Update(){
        if (bleeding > 0) StartCoroutine(BleedingHandler());
        if (bleeding < 0) bleeding = 0;
    }

    IEnumerator BleedingHandler(){
        yield return new WaitForSeconds(bleedingRate);
        health -= bleedingPerOne;
        bleeding -= bleedingPerOne;
    }
}