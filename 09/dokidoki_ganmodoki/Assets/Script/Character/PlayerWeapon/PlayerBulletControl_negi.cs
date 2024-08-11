using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// プレイヤーのうつ弾.
public class PlayerBulletControl_negi : PlayerBulletControl {

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
	}
	
	void	Update()
	{
		bool	is_damage = false;

		do {

			// 壁に当たったら消す
			// 壁の向こうの敵にあたっちゃわないように、先に壁を調べる.
			if(this.collision_results.Exists(x => x.object1.layer ==  LayerMask.NameToLayer("Wall"))) {

				is_damage = true;
				break;
			}

			// 敵に当たったら、ダメージを与える.
			foreach(var result in this.collision_results) {

				if(result.object1 == null) {

					continue;
				}

				int		enemy_layer      = LayerMask.NameToLayer("Enemy");
				int		enemy_lair_layer = LayerMask.NameToLayer("EnemyLair");

				if(result.object1.layer == enemy_layer || result.object1.layer == enemy_lair_layer) {

					if((this.player.behavior as chrBehaviorLocal) != null) {

						result.object1.GetComponent<chrController>().causeDamage(1.0f, this.player.global_index);
					}
					result.object1 = null;
					is_damage = true;
				}
			}
			if(is_damage) {

				break;
			}

			// 画面外に出たら消える.
			if(!this.is_in_screen()) {

				is_damage = true;
				break;
			}

		} while(false);

		this.collision_results.Clear();

		if(is_damage) {

			GameObject.Destroy(this.gameObject);

		} else {

			// 移動.

			float	speed = 0.2f;

			this.transform.Translate(Vector3.forward*speed*(Time.deltaTime/(1.0f/60.0f)));
		}
	}
}
