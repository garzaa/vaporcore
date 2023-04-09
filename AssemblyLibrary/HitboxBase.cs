using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class HitboxBase : MonoBehaviour {
	public bool attacksPlayer;
	public bool indiscriminate;
	
	public bool spawnHitmarkerAtCenter;
	public bool singleHitPerActive = true;
	IAttackLandListener[] attackLandListeners;
	Collider2D[] colliders;
	HashSet<Hurtbox> hurtboxesHitThisActive = new HashSet<Hurtbox>();
	HashSet<EntityBase> entitiesHitThisActive = new HashSet<EntityBase>();
	CameraZoom cameraZoom;
	bool hitboxOutLastFrame = false;

	public AttackData data;

	public UnityEvent OnAttackLand;

	virtual protected void Start() {
		gameObject.layer = LayerMask.NameToLayer(Layers.Hitboxes);
		attackLandListeners = GetComponentsInParent<IAttackLandListener>();
		colliders = GetComponents<Collider2D>();
		cameraZoom = GameObject.FindObjectOfType<CameraZoom>();
	}

	void Update() {
		bool hitboxOut = false;
		foreach (Collider2D collider in colliders) {
			if (collider.enabled) {
				hitboxOut = true;
				break;
			}
		}

		if (!hitboxOutLastFrame && hitboxOut) {
			OnHitboxOut();
		} else if (!hitboxOut && hitboxOutLastFrame) {
			hurtboxesHitThisActive.Clear();
			entitiesHitThisActive.Clear();
		}

		hitboxOutLastFrame = hitboxOut;
	}

	void OnHitboxOut() {
	
	}

	protected virtual bool CanHit(Hurtbox hurtbox) {
		if (hurtbox.gameObject.CompareTag(Tags.Player) && !attacksPlayer) return false;
		if (attacksPlayer && !hurtbox.gameObject.CompareTag(Tags.Player) && !indiscriminate) return false;

		if (hurtboxesHitThisActive.Contains(hurtbox)) {
			return false;
		}

		EntityBase e = hurtbox.GetComponentInParent<EntityBase>();
		if (e && entitiesHitThisActive.Contains(e)) return false;
		if (e && e == GetComponentInParent<EntityBase>()) return false;

		return true;
	}

	void OnTriggerEnter2D(Collider2D other) {

	}

	protected virtual void Hit(Hurtbox hurtbox, Collider2D other) {

	}
}
