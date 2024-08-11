using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 敵の撃つ弾.
public class EnemyBulletControl : MonoBehaviour {

	public chrController	owner = null;

	public bool		trigger_damage = false;

	public List<CollisionResult>	collision_results = new List<CollisionResult>();

	// 弾の生存時間. この時間を超えたら消す.
	public float lifeSpan = 10.0f;

	// lifeSpan までの時間を計るタイマー.
	private float lifeTimer = 0.0f;
	
	/// <summary>
	/// ポーズを命令されているかどうか.
	/// </summary>
	protected bool isPaused = false;

	// ================================================================ //
	// MonoBehaviour からの継承.

	public void		causeDamage()
	{
		this.trigger_damage = true;
	}

	void	Start()
	{
	}
	
	void	Update()
	{
		bool	is_damaged = false;
		
		if (isPaused)
		{
			return;
		}

		lifeTimer += Time.deltaTime;

		// コリジョンチェック.
		do {

			// 壁に当たったら消す.
			// 壁の向こうの敵にあたっちゃわないように、先に壁を調べる.
			if(this.collision_results.Exists(x => x.object1.layer ==  LayerMask.NameToLayer("Wall"))) {

				is_damaged = true;
				break;
			}

			// プレイヤーに当たったら、ダメージを与える.
			foreach(var result in this.collision_results) {

				if(result.object1 == null) {

					continue;
				}

				if(result.object1.tag != "Player") {

					continue;
				}

				chrBehaviorPlayer	behavior = result.object1.GetComponent<chrController>().behavior as chrBehaviorPlayer;

				if(behavior == null) {

					continue;
				}
				if(!behavior.isLocal()) {

					// リモートプレイヤーにはダメージを与えない.
					continue;
				}
				result.object1.GetComponent<chrController>().causeDamage(this.owner.vital.getShotPower(), -1);
				result.object1 = null;
				is_damaged = true;
			}
			if(is_damaged) {

				break;
			}

			// ライフタイマーが超過していたら消える.
			if (lifeTimer >= lifeSpan) {

				is_damaged = true;
				break;
			}

		} while(false);

		this.collision_results.Clear();

		if(is_damaged) {

			GameObject.Destroy(this.gameObject);

		} else {

			// 移動.

			float	speed = 12.0f;

			Vector3		move_to = this.transform.position + this.transform.forward*speed*Time.deltaTime;

			this.GetComponent<Rigidbody>().MovePosition(move_to);				
		}
	}

	void 	OnTriggerEnter(Collider other)
	{
		// 余分なものとは当たらない設定なので、とりあえず積んでしまう.
		CollisionResult	result = new CollisionResult();
		
		result.object0 = this.gameObject;
		result.object1 = other.gameObject;
		result.is_trigger = false;
		
		this.collision_results.Add(result);
	}
	
	//=================================================================================//
	// 外からコールされるメソッド.
	public void SetPause(bool newPause)
	{
		isPaused = newPause;
	}
}
