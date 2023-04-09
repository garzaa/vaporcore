using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour {
	public GameObject impact;
	public bool randomImpactRotation;
	HitboxBase attackHitbox;

	Rigidbody2D rb2d;

	bool impacted = false;

	void Awake() {
		rb2d = GetComponent<Rigidbody2D>();
		attackHitbox = GetComponent<HitboxBase>();
	}

	void OnTriggerEnter2D(Collider2D other) {
		if (impact && !impacted) {
			if (!attackHitbox.attacksPlayer) {
				if (other.gameObject.CompareTag(Tags.Player)) {
					return;
				}
				// don't let enemy attacks block it
				if (other.GetComponent<HitboxBase>()) {
					return;
				}
			}
			
			// if it's just a random trigger, do nothing
			if (!other.GetComponent<Hurtbox>() && other.isTrigger) return;

			impacted = true;
			GameObject i = Instantiate(impact, transform.position, Quaternion.identity, null);
			if (randomImpactRotation) {
				i.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
			}
			Destroy(gameObject);
		}
	}
}
