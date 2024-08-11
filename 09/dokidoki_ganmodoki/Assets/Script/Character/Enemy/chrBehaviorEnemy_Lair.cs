using UnityEngine;
using System.Collections;
using System.Collections.Generic;


// 敵の巣（ジェネレーター）.
public class chrBehaviorEnemy_Lair : chrBehaviorEnemy {

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Awake()
	{
		// プレイヤーが押せないように.
		this.GetComponent<Rigidbody>().isKinematic = true;
		//this.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
	}

	void	Start()
	{
	}
	
	void	Update()
	{
	}

	// スポーンするエネミーの情報.
	public class SpawnEnemy {

		public string	enemy_name = "Enemy_Obake";		// 敵の種類（"Enemy_Obake", "Enemy_Kumasan" 等）.

		public Enemy.BEHAVE_KIND				behave_kind = Enemy.BEHAVE_KIND.UROURO;		// 行動パターン.
		public Character.ActionBase.DescBase	behave_desc = null;							// Action 生成時のオプション.

		public float	frequency = 1.0f;				// 発生確率.
	}
	protected List<SpawnEnemy>	spawn_enemies = new List<SpawnEnemy>();

	// スポーンするエネミーの種類を追加する.
	public SpawnEnemy		resisterSpawnEnemy()
	{
		var	spawn = new SpawnEnemy();

		this.spawn_enemies.Add(spawn);

		return(spawn);
	}

	// ================================================================ //

	public override void	initialize()
	{
		base.initialize();

		this.unique_action = new Character.HakoAction();
		this.unique_action.create(this);

		this.basic_action.unique_action = this.unique_action;
	}
	public override void	start()
	{
		base.start();
		this.control.vital.hit_point = 5.0f;
	}


	public override	void	execute()
	{
		base.execute();
		this.basic_action.execute();

		this.is_attack_motion_finished = false;
	}

	// ================================================================ //

	// 敵をすぽ～んとスポーンする（LevelControl、ホスト用）.
	public void		spawnEnemy()
	{
		Character.HakoAction	hako_action = this.unique_action as Character.HakoAction;

		if(hako_action != null) {

			hako_action.step.set_next(Character.HakoAction.STEP.SPAWN);
		}
	}

	// 敵をすぽ～んとスポーンする（Action 、ホスト用）.
	public void		create_enemy_internal()
	{
		// 登録されたエネミーの中からランダムに選ぶ.

		if(this.spawn_enemies.Count == 0) {

			this.spawn_enemies.Add(new SpawnEnemy());
		}

		float	sum = 0.0f;

		foreach(var se in this.spawn_enemies) {

			sum += se.frequency;
		}

		SpawnEnemy	spawn_enemy = this.spawn_enemies[0];

		float	rand = Random.Range(0.0f, sum);

		foreach(var se in this.spawn_enemies) {

			rand -= se.frequency;

			if(rand <= 0.0f) {

				spawn_enemy = se;
				break;
			}
		}

		//
		dbwin.console().print("Spawn LairName:" + this.name);
		dbwin.console ().print("Create enemy:" + spawn_enemy.enemy_name);

		//Debug.Log("Spawn LairName:" + this.name);
		//Debug.Log("Create enemy:" + spawn_enemy.enemy_name);

		chrBehaviorEnemy	enemy = LevelControl.get().createCurrentRoomEnemy<chrBehaviorEnemy>(spawn_enemy.enemy_name);

		if(enemy != null) {

			enemy.setBehaveKind(spawn_enemy.behave_kind, spawn_enemy.behave_desc);
			enemy.beginSpawn(this.transform.position + Vector3.up*3.0f, this.transform.forward);

			if (GameRoot.get().isHost()) {
				// この文字列をいっしょにゲストに送ってください.
				string		pedigree = enemy.name + "." + spawn_enemy.behave_kind;
				
				// リモートへの送信.
				EnemyRoot.get().RequestSpawnEnemy(this.name, pedigree);

				//Debug.Log(pedigree);
			}
		}
	}

	// 敵をすぽ～んとスポーンする（LevelControl、ゲスト用）.
	public void		spawnEnemyFromPedigree(string pedigree)
	{
		Character.HakoAction	hako_action = this.unique_action as Character.HakoAction;

		if(hako_action != null) {

			hako_action.pedigree = pedigree;
			hako_action.step.set_next(Character.HakoAction.STEP.SPAWN);

			dbwin.console().print("*Spawn LairName:" + this.name);
			dbwin.console ().print ("*Create enemy:" + pedigree);	
			//Debug.Log("*Spawn LairName:" + this.name);
			//Debug.Log("*Create enemy:" + pedigree);
		}
	}

	// 敵をすぽ～んとスポーンする（Action 、ゲスト用）.
	public void		create_enemy_internal_pedigree(string pedigree)
	{
		// 登録されたエネミーの中からランダムに選ぶ.

		do {

			string[]	tokens = pedigree.Split('.');

			if(tokens.Length < 3) {

				break;
			}

			string	enemy_name = tokens[0] + "." + tokens[1];

			if(!System.Enum.IsDefined(typeof(Enemy.BEHAVE_KIND), tokens[2])) {

				break;
			}

			Enemy.BEHAVE_KIND	behave = (Enemy.BEHAVE_KIND)System.Enum.Parse(typeof(Enemy.BEHAVE_KIND), tokens[2]);

			chrBehaviorEnemy	enemy = LevelControl.get().createCurrentRoomEnemy<chrBehaviorEnemy>(enemy_name);

			if(enemy == null) {

				break;
			}

			enemy.name = enemy_name;
			enemy.setBehaveKind(behave, null);
			enemy.beginSpawn(this.transform.position + Vector3.up*3.0f, this.transform.forward);

		} while(false);

	}

	// ================================================================ //

	// ダメージをうけたときに呼ばれる.
	public override void		onDamaged()
	{
		if(this.control.vital.hit_point <= 0.0f) {

			Character.HakoAction	action = this.unique_action as Character.HakoAction;

			if(action.step.get_current() != Character.HakoAction.STEP.PECHANCO) {

				action.step.set_next(Character.HakoAction.STEP.PECHANCO);
			}
		}
	}

	// ================================================================ //
	// アニメーションのイベント.

	// 死亡アニメーション終了のイベントをアニメーションから受け取る.
	public void NotifyFinishedDeathAnimation()
	{
		Character.HakoAction	action = this.unique_action as Character.HakoAction;

		action.is_death_motion_finished = true;
	}

	// 敵生成モーションの「ぺっ！」のタイミングで呼ばれる.
	public void		evEnemy_Lair_Pe()
	{
		Character.HakoAction	action = this.unique_action as Character.HakoAction;

		action.is_trigger_pe = true;
	}


}

// ==================================================================== //
//																		//
//																		//
//																		//
// ==================================================================== //
namespace Character {

// 敵ジェネレーターのアクション.
public class HakoAction : ActionBase {

	// ---------------------------------------------------------------- //

	public enum STEP {

		NONE = -1,

		IDLE = 0,
		READY,				// 待機中.
		SPAWN,				// 敵スポーン.
		PECHANCO,			// つぶれる.

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	// ---------------------------------------------------------------- //

	public bool		is_death_motion_finished;		// やられアニメーションの再生が終わった？.
													// （アニメーションのイベント内でセットされる）.

	public bool		is_trigger_pe;					// 敵出現アニメーションの出現タイミング.

	public string 	pedigree = "";					// ゲスト用。ホストで生成した敵の名まえと行動パターン.

	// ================================================================ //

	public override void	start()
	{
		this.step.set_next(STEP.READY);
	}

	public override void	execute()
	{
		BasicAction	basic_action = this.behavior.basic_action;

		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			// 待機中.
			case STEP.READY:
			{
				/*do {

					if(!LevelControl.get().canCreateCurrentRoomEnemy()) {

						break;
					}

					if(!Input.GetMouseButtonDown(1)) {
					//if(!this.step.is_acrossing_cycle(5.0f)) {
	
						break;
					}

					this.step.set_next(STEP.SPAWN);

				} while(false);*/
			}
			break;

			// 敵スポーン.
			case STEP.SPAWN:
			{
				// スポーンアニメーションが終わったら待機状態に戻る.
				do {

					// アニメーションの遷移中なら、戻らない.
					// （これをいれておかないと、"idle" -> "generate" の
					// 　遷移中に次の if 文が true になっちゃう）.
					if(basic_action.animator.IsInTransition(0)) {

						break;
					}
					if(basic_action.animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Generate")) {

						break;
					}

					this.step.set_next(STEP.READY);

				} while(false);
			}
			break;

			// つぶれる.
			case STEP.PECHANCO:
			{
				if(this.is_death_motion_finished) {

					this.step.set_next_delay(STEP.IDLE, 0.5f);
				}
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {

				case STEP.IDLE:
				{
					// 親階層のアクションに復帰する.
					if(this.parent != null) {

						this.parent.resume();
					}
				}
				break;

				// 待機中.	
				case STEP.READY:
				{
				}
				break;

				// 敵スポーン.
				case STEP.SPAWN:
				{
					this.is_trigger_pe = false;
					basic_action.animator.SetTrigger("Generate");
				}
				break;

				// つぶれる.
				case STEP.PECHANCO:
				{
					basic_action.animator.SetTrigger("Death");
					this.is_death_motion_finished = false;

					this.control.cmdEnableCollision(false);
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			// 敵スポーン.
			case STEP.SPAWN:
			{
				// モーションのタイミングに合わせて敵をぺっと吐き出す.
				if(this.is_trigger_pe) {

					chrBehaviorEnemy_Lair	behave_lair = this.behavior as chrBehaviorEnemy_Lair;

					if(this.pedigree != "") {

						behave_lair.create_enemy_internal_pedigree(this.pedigree);
						this.pedigree = "";

					} else {

						behave_lair.create_enemy_internal();
					}

					this.is_trigger_pe = false;
				}
			}
			break;

		}

		// ---------------------------------------------------------------- //

	}
}

}
