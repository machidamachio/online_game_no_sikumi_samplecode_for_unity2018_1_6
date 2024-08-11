using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MathExtension;
using GameObjectExtension;

// プレイヤーのうつ弾（ゆずボム）.
public class PlayerBulletControl_yuzu : PlayerBulletControl {

	public GameObject		coli_node = null;
	public GameObject		model_node = null;
	public float			max_radius = 2.0f;

	protected float			coli_sphere_radius = 1.0f;

	//

	protected Vector3	velocity = Vector3.zero;

	protected const float	PEAK_HEIGHT = 5.0f;
	protected const float	REACH = 5.0f;			// 着弾距離.

	protected enum STEP {

		NONE = -1,

		FLYING = 0,
		EXPLODE,
		END,

		NUM,
	};
	protected Step<STEP>		step = new Step<STEP>(STEP.NONE);

	protected GameObject		explode_effect = null;


	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
		float	t = Mathf.Sqrt((2.0f*PEAK_HEIGHT)/Mathf.Abs(Physics.gravity.y));

		float	v_velocity = Mathf.Abs(Physics.gravity.y)*t/2.0f;
		float	h_velocity = REACH/(t*2.0f);

		this.velocity = this.transform.forward*h_velocity + Vector3.up*v_velocity;

		// プレイヤーの移動量を加えておく.
		this.velocity += this.player.getMoveVector().Y(0.0f)/Time.deltaTime;

		//

		var		coli = this.coli_node.GetComponent<SphereCollider>();

		if(coli != null) {

			this.coli_sphere_radius = coli.radius;
		}

		//

		this.step.set_next(STEP.FLYING);
	}

	void	Update()
	{
		this.resolve_collision();

		float	explode_time = 0.5f;

		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			case STEP.FLYING:
			{
				if(this.trigger_damage) {

					this.step.set_next(STEP.EXPLODE);
				}
			}
			break;

			case STEP.EXPLODE:
			{
				//if(this.explode_effect == null) {
				if(this.step.get_time() > explode_time) {

					this.step.set_next(STEP.END);
				}
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {
	
				case STEP.FLYING:
				{
				}
				break;

				case STEP.EXPLODE:
				{
				this.model_node.GetComponent<Renderer>().enabled = false;
					this.explode_effect = EffectRoot.get().createYuzuExplode(this.gameObject.getPosition().Y(0.5f));

					this.coli_node.setLocalScale(Vector3.one*this.max_radius*0.0f);
				}
				break;

				case STEP.END:
				{
					this.gameObject.destroy();
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			case STEP.FLYING:
			{
				// 移動.
				this.velocity += Physics.gravity*Time.deltaTime;
				this.transform.position += this.velocity*Time.deltaTime;
			}
			break;

			case STEP.EXPLODE:
			{
				float	rate = this.step.get_time()/explode_time;

				float	scale = rate*this.max_radius;

				this.coli_node.setLocalScale(Vector3.one*scale);

				// 爆風とのヒット.
				// ゆずボムの爆風がジェネレーターにヒットしにくいみたいなので、自前で調べる.

				RaycastHit[]	hits = Physics.SphereCastAll(this.transform.position + Vector3.up*0.1f, scale*this.coli_sphere_radius, Vector3.down, 0.2f, LayerMask.GetMask("EnemyLair"));

				foreach(RaycastHit hit in hits) {

					CollisionResult	result = new CollisionResult();
		
					result.object0 = this.gameObject;
					result.object1 = hit.collider.gameObject;
					result.is_trigger = true;
		
					this.collision_results.Add(result);
				}

			}
			break;
		}
	}

	// コリジョン結果の処理.
	protected void	resolve_collision()
	{
		do {

			// 壁に当たったら消す.
			// 壁の向こうの敵にあたっちゃわないように、先に壁を調べる.
			if(this.collision_results.Exists(x => x.object1.layer ==  LayerMask.NameToLayer("Wall"))) {

				this.trigger_damage = true;
			}

			// 敵に当たったら、ダメージを与える.
			foreach(var result in this.collision_results) {

				if(result.object1 == null) {

					continue;
				}

				int		enemy_layer      = LayerMask.NameToLayer("Enemy");
				int		enemy_lair_layer = LayerMask.NameToLayer("EnemyLair");

				if(result.object1.layer == enemy_layer || result.object1.layer == enemy_lair_layer) {

					if(this.step.get_current() == STEP.EXPLODE) {

						if((this.player.behavior as chrBehaviorLocal) != null) {

							result.object1.GetComponent<chrController>().causeDamage(5.0f, this.player.global_index);
						}
						result.object1 = null;
					}
					this.trigger_damage = true;
				}
			}

			// 地面に落ちた.
			if(this.transform.position.y < 0.0f) {

				Vector3		p = this.transform.position;

				p.y = 0.0f;
				this.transform.position = p;

				this.trigger_damage = true;
			}

		} while(false);

		this.collision_results.Clear();
	}
	
}
