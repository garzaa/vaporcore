using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnvironmentHitbox : HitboxBase {
	override protected void Start() {
		base.Start();
		singleHitPerActive = false;
	}
}
