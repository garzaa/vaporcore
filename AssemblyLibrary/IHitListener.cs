public interface IHitListener {
	void OnHit(HitboxBase attack);

	void OnHitCheck(HitboxBase attack) {}

	bool CanBeHit(HitboxBase attack) {
		return true;
	}
}
