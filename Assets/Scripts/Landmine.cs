using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Landmine : MonoBehaviour
{
    [SerializeField] private float damage = 50f;
    [SerializeField] private GameObject livingHitParticles;
    
    private void OnTriggerEnter(Collider other) {
        var entity = other.gameObject.GetComponentInParent<Entity>();

        if (entity == null)
        {
            return;
        }
        
        entity.Hurt(damage);
        Instantiate(livingHitParticles, transform.position, Quaternion.identity);
    }
}
