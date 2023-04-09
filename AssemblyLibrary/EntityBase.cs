using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

[RequireComponent(typeof(WallCheck))]
[RequireComponent(typeof(GroundCheck))]
[RequireComponent(typeof(EntityShader))]
public class EntityBase : MonoBehaviour, IHitListener {
	#pragma warning disable 0649
	[SerializeField] AudioResource defaultFootfall;
	[SerializeField] protected AudioResource landNoise;
	[SerializeField] bool suppressAnimatorWarnings = false;
	#pragma warning restore 0649

	protected Animator animator;
	protected Rigidbody2D rb2d;
	protected int groundMask;
	protected Collider2D collider2d;
	public bool facingRight { get; private set; }
	protected GroundData groundData;
	protected WallCheckData wallData;
	protected Collider2D groundColliderLastFrame;
	public EntityShader shader { get; private set; }
	public bool stunned { get; private set; }
	
	GroundCheck groundCheck;
	AudioResource currentFootfall;
	PhysicsMaterial2D defaultMaterial;

	static GameObject jumpDust;
	protected static GameObject landDust;
	static GameObject footfallDust;
	
	bool canGroundHitEffect = true;
	public bool staggerable = true;
	public bool takesEnvironmentDamage = true;
	bool invincible = false;
	
	bool canFlip = true;

	Collider2D[] overlapResults;

	bool hitstopPriority;
    Coroutine hitstopRoutine;
	float duration;
	Vector2 hitstopExitVelocity;


	GameObject lastSafeObject;
	Vector3 lastSafeOffset;

	float fallStart = 0;
	float ySpeedLastFrame = 0;

	Coroutine safetySaver;
	
	protected const float groundFlopStunTime = 6f/12f;

	protected virtual void Awake() {
		animator = GetComponent<Animator>();
		if (suppressAnimatorWarnings) animator.logWarnings = false;
        rb2d = GetComponent<Rigidbody2D>();
		shader = GetComponent<EntityShader>();
        groundMask = 1 << LayerMask.NameToLayer(Layers.Ground);
        collider2d = GetComponent<Collider2D>();
        groundCheck = GetComponent<GroundCheck>();
        groundData = groundCheck.groundData;
		wallData = GetComponent<WallCheck>().wallData;
		if (!jumpDust) jumpDust = Resources.Load<GameObject>("Runtime/JumpDust");
		if (!landDust) landDust = Resources.Load<GameObject>("Runtime/LandDust");
		if (!footfallDust) footfallDust = Resources.Load<GameObject>("Runtime/FootfallDust");
		defaultMaterial = rb2d.sharedMaterial;
	}

    public void DoHitstop(float duration, Vector2 exitVelocity, bool priority=false, bool selfFlinch = false) {
        if (hitstopPriority && !priority) return;
		if (hitstopRoutine != null) {
			StopCoroutine(hitstopRoutine);
			hitstopRoutine = null;
		} else {
			hitstopExitVelocity = exitVelocity;
		}

		this.duration = duration;
		animator.speed = 0f;
		rb2d.constraints = RigidbodyConstraints2D.FreezeAll;
		if (selfFlinch) shader.Flinch(exitVelocity, duration);
		hitstopRoutine = StartCoroutine(EndHitstop());
    }

    IEnumerator EndHitstop() {
        yield return new WaitForSeconds(duration);
		rb2d.constraints = RigidbodyConstraints2D.FreezeRotation;
		rb2d.velocity = hitstopExitVelocity;
        hitstopPriority = false;
        animator.speed = 1;
		hitstopRoutine = null;
    }

    void InterruptHitstop() {
		if (hitstopRoutine != null) StopCoroutine(hitstopRoutine);
		hitstopPriority = false;
		animator.speed = 1f;
    }

	public void JumpDust() {
		Vector2 pos = new Vector2(
			transform.position.x,
			collider2d.bounds.min.y
		);
		Instantiate(jumpDust, pos, Quaternion.identity, null);
	}

	public void LandDust() {
		Vector2 pos = new Vector2(
			transform.position.x,
			collider2d.bounds.min.y
		);
		Instantiate(landDust, pos, Quaternion.identity, null);
	}

	public void FootfallDust() {
		// when transitioning into a falling animation
		if (!groundData.grounded) return;
		Vector2 pos = new Vector2(
			transform.position.x,
			collider2d.bounds.min.y
		);
		GameObject d = Instantiate(footfallDust, pos, Quaternion.identity, null);
		// keep track of facing left/right
		d.transform.localScale = transform.localScale;
	}

	public void SetInvincible(bool b) {
		if (b) {
			invincible = true;
			shader.StartFlashingWhite();
		} else {
			invincible = false;
			shader.StopFlashingWhite();
		}
	}

	public void CanBeHit(HitboxBase attack) {
		if (invincible) return;
		if (!takesEnvironmentDamage && attack is EnvironmentHitbox) return;
	}


	public void OnHit(HitboxBase hitbox) {
		if (staggerable) {
			Vector2 v = GetKnockback(hitbox);
			// if it's envirodamage, return to safety
			if (hitbox is EnvironmentHitbox) {
				// transition to air hurt isn't happening here...why
				rb2d.velocity = Vector2.zero;
				CancelInvoke(nameof(ReturnToSafety));
				if (safetySaver != null) StopCoroutine(safetySaver);
				Invoke(nameof(ReturnToSafety), hitbox.data.hitstop);
			} else {
				// heavier people get knocked back less
				rb2d.velocity = v * (1f/rb2d.mass);
				// flip to attack
				float attackX = hitbox.transform.position.x;
				if (facingRight && attackX<transform.position.x) {
					Flip();
				} else if (!facingRight && attackX>transform.position.x) {
					Flip();
				}
			}

			if (hitbox.data.stunLength > 0) {
				StunFor(hitbox.data.stunLength, hitbox.data.hitstop);
				if (hitbox is EnvironmentHitbox) {
					// instant tumble for return to safety
					animator.SetBool("Tumbling", true);
				} else {
					if (!groundData.grounded) {
						animator.Update(1f);
					}
				}
			}
			DoHitstop(hitbox.data.hitstop, rb2d.velocity, selfFlinch: true);
			shader.FlashWhite();
		} else {
			shader.FlinchOnce(GetKnockback(hitbox));
		}
	}

	public Vector2 GetKnockback(HitboxBase attack) {
		Vector2 v = attack.data.GetKnockback(attack, this.gameObject);
		if (groundData.grounded && v.y < 0 && v.y > -5) {
			v.y = 0;
		}
		if (!attack.data.autolink) {
			float attackX = attack.transform.position.x;
			v.x *= attackX > transform.position.x ? -1 : 1;
		}
		return v;
	}

	public void StunFor(float seconds, float hitstopDuration) {
		animator.SetTrigger("OnHit");
		stunned = true;
		animator.SetBool("Stunned", true);
		animator.SetBool("Tumbling", false);
		CancelInvoke(nameof(UnStun));
		Invoke(nameof(UnStun), seconds+hitstopDuration);
	}

	public void CancelStun() {
		CancelInvoke(nameof(UnStun));
		UnStun();
	}

	void UnStun() {
		animator.SetBool("Stunned", false);
		if (groundData.grounded) {
			animator.SetBool("Tumbling", false);
		} else {
		}
		stunned = false;
		rb2d.sharedMaterial = defaultMaterial;
	}


	protected virtual void Update() {
		UpdateFootfallSound();
		if (groundData.hitGround && canGroundHitEffect && fallStart-transform.position.y > 1) {
			if (!stunned && defaultFootfall) {
				FootfallSound();
			}
			LandDust();
			canGroundHitEffect = false;
			this.WaitAndExecute(() => canGroundHitEffect=true, 0.1f);
		}
		if (wallData.hitWall) {
			landNoise?.PlayFrom(this.gameObject);
			GameObject g = Instantiate(landDust);
			bool wallRight = wallData.direction > 0;
			float x = wallRight ? collider2d.bounds.max.x : collider2d.bounds.min.x;
			g.transform.position = new Vector2(x, transform.position.y);
			g.transform.eulerAngles = new Vector3(0, 0, wallRight ? 90 : -90);
			OnWallHit();
		}
		RectifyEntityCollision();

		if (groundData.hitGround) {
			safetySaver = StartCoroutine(SaveLastSafePosition());
		}

		if (ySpeedLastFrame>=0 && rb2d.velocity.y<0) {
			fallStart = transform.position.y;
		} 
		ySpeedLastFrame = rb2d.velocity.y;
	}

	void RectifyEntityCollision() {
		if (!staggerable || invincible) return;
		// push self away if standing on top of someone
		if (stunned) return;
		overlapResults = Physics2D.OverlapBoxAll(
			transform.position,
			collider2d.bounds.size / 2f,
			0,
			Layers.EnemyCollidersMask | Layers.PlayerMask
		);
		Collider2D overlapping = null;
		for (int i=0; i<overlapResults.Length; i++) {
			if (overlapResults[i] != collider2d) {
				overlapping = overlapResults[i];
				break;
			}
		}
		if (overlapping) {
			rb2d.AddForce(Vector3.Project((transform.position - overlapping.transform.position), Vector3.right).normalized * 4f * Time.timeScale);
		}
	}

	protected virtual void OnWallHit() {

	}

	void UpdateFootfallSound() {
		if (!groundData.grounded) {
			return;
		}
        if (groundData.groundCollider != groundColliderLastFrame) {
            AudioResource s = groundData.groundCollider.GetComponent<GroundProperties>()?.footfallSound;
            if (s != null) {
                currentFootfall = s;
            } else {
                currentFootfall = defaultFootfall;
            }
        }
        groundColliderLastFrame = groundData.groundCollider;
    }

	public void FootfallSound() {
		currentFootfall.PlayFrom(this.gameObject);
	}

	public void DisableFlip() {
		canFlip = false;
	}

	public void EnableFlip() {
		canFlip = true;
	}

	public void Flip() {
        if (!canFlip) return;
		_Flip();
    }

	public void _Flip() {
		facingRight = !facingRight;
        transform.localScale = Vector3.Scale(transform.localScale, new Vector3(-1, 1, 1));
	}

	public void FlipTo(GameObject target) {
		if (transform.position.x < target.transform.position.x && !facingRight) {
			_Flip();
		} else if (target.transform.position.x < transform.position.x && facingRight) {
			_Flip();
		}
	}

	public Vector2Int ForwardVector() {
		return new Vector2Int(
            Forward(),
            1
        );
	}

	public int Forward() {
		return facingRight ? 1 : -1;
	}

	public void AddAttackImpulse(Vector2 impulse) {
		rb2d.AddForce(impulse * ForwardVector(), ForceMode2D.Impulse);
	}
	
	IEnumerator SaveLastSafePosition() {
		if (!groundData.grounded || groundData.onLedge || wallData.touchingWall || stunned) {
			yield break;
		}

		GameObject currentGround = groundData.groundObject;
		if (currentGround?.GetComponentInParent<GroundProperties>() != null) {
			if (currentGround.GetComponentInParent<GroundProperties>().dangerous) {
				yield break;
			}
		}

		Vector3 savedPos = transform.position;
		Vector3 groundPos = currentGround.transform.position;

		// if the player's about to slide off and hit envirodamage
		yield return new WaitForSeconds(0.5f);

		// get offset, in case it's moving
		Vector3 currentOffset = savedPos - groundPos;
		lastSafeObject = currentGround;
		lastSafeOffset = currentOffset;
	}

	void ReturnToSafety() {
		Vector3 lastPos = transform.position;
		transform.position = lastSafeObject.transform.position + lastSafeOffset;
		// flip so they're looking at the last position
		if (Forward() * (lastPos.x - transform.position.x) < 0) {
			_Flip();
		}
	}
}
