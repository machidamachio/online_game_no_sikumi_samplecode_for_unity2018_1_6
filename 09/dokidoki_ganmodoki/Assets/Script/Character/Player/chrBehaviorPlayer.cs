using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GameObjectExtension;

// プレイヤー.
// ローカルプレイヤーとネットプレイヤー共通の処理.
// (chrBehaviorLocal と chrBehaviorNet の）.
public class chrBehaviorPlayer : chrBehaviorBase {

	public DoorControl			door = null;

	protected BulletShooter		bullet_shooter = null;
	protected SHOT_TYPE			shot_type      = SHOT_TYPE.NONE;

	public Vector3				position_in_formation = Vector3.zero;

	public const float		CHARITY_SPHERE_RADIUS = 3.5f;				// 体力回復アイテムの効果が届く距離.
	public const float		ICE_DIGEST_TIME       = 2.0f;				// [sec] これ以上短い時間で連続してアイスを使うと、食べ過ぎ（頭じんじん）.
	public const float		SHOT_BOOST_TIME       = 10.0f;				// [sec] キャンディーでショットがパワーアップする時間.
	public const float		MELEE_ATTACK_POWER    = 10.0f;				// 近接攻撃の攻撃力.

	protected const float	BLOW_OUT_TIME = 0.1f;						// [sec] 吹き飛ばされの時間.

	// ---------------------------------------------------------------- //
	
	public Item.SlotArray			item_slot = new Item.SlotArray();	// プレイヤーが拾えるアイテム.

	protected	Vector3		initial_local_position_model;				// モデルのローカルポジション（初期状態での）.

	// アイス食べ過ぎで頭痛い状態タイマー.
	public class JinJinTimer {

		public float	current  = -1.0f;
		public float	duration = -1.0f;
	};

	public JinJinTimer	jin_jin_timer  = new JinJinTimer();				// アイス食べ過ぎで頭痛い状態.
	public GameObject	jin_jin_effect = null;

	protected MeleeAttack	melee_attack;

	public SkinColorControl	skin_color_control = null;					// モデルのカラーチェンジ（体力回復、アイス食べ過ぎなど）.

	protected int		cake_count = 0;									// ケーキをとった数.
	protected float		ice_timer = -1.0f;								// [sec] アイスを使ってからの経過時間.

	protected float		shot_boost_timer = 0.0f;						// [sec] ショットがパワーアップする用タイマー.

	protected bool		is_shot_enable = true;							// ショット撃てる？.

	protected int		melee_count = 0;								// 近接攻撃で倒した敵の数.

	// 体力が０になったとき演出.
	protected class StepBatanQ {

		public GameObject	tears_effect;		// 涙エフェクト.
	};
	protected StepBatanQ	step_batan_q = new StepBatanQ();

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Awake()
	{
		this.initial_local_position_model = this.getModel().transform.localPosition;
	}

	void	Start()
	{
	}

	void	Update()
	{
	}

	// ================================================================ //

	public override void	initialize()
	{
		// ショット.
		this.changeBulletShooter(SHOT_TYPE.NEGI);

		// 近接攻撃.
		this.melee_attack = this.gameObject.AddComponent<MeleeAttack>();
		this.melee_attack.behavior = this;

		this.skin_color_control = new SkinColorControl();
		this.skin_color_control.create(this);
	}

	public override void	start()
	{
		this.control.vital.healFullInternal();
		this.control.resetMotion();
	}

	public override	void	execute()
	{
		this.door = null;

		// ショットのパワーアップタイマー.
		// （キャンディーをとったとき）.
		if(this.shot_boost_timer > 0.0f) {

			this.shot_boost_timer -= Time.deltaTime;

			if(this.shot_boost_timer <= 0.0f) {

				// 時間切れで効果が切れる.

				this.shot_boost_timer = 0.0f;

				ItemWindow.get().clearItem(Item.SLOT_TYPE.CANDY, 0);

				this.item_slot.candy.initialize();
			}
		}

		this.melee_attack.execute();

		if(this.ice_timer >= 0.0f) {

			this.ice_timer -= Time.deltaTime;
		}

		this.executeJinJin();

		this.skin_color_control.execute();

		this.execute_queries();
	}

	// ローカルプレイヤー？.
	public virtual bool	isLocal()
	{
		return(true);
	}

	// 外部からのコントロールを開始する.
	public virtual void		beginOuterControll()
	{
	}

	// 外部からのコントロールを終了する.
	public virtual void		endOuterControll()
	{
	}

	// アイテムを使ったときによばれる.
	public virtual void		onUseItem(int slot, Item.Favor favor)
	{
	}

	//  アイテムを使ってもらった（仲間が自分に）ときによばれる.
	public virtual void		onUseItemByFriend(Item.Favor favor, chrBehaviorPlayer friend)
	{
		switch(favor.category) {

			case Item.CATEGORY.SODA_ICE:
			{
				this.skin_color_control.startHealing();
			}
			break;
		}
	}

	// 近接攻撃がヒットしたときに呼ばれる.
	public override void		onMeleeAttackHitted(chrBehaviorBase other)
	{
		// 近接攻撃で敵を１０匹倒すごとに、キャンディーが出る.
		do {

			if(other.control.vital.getHitPoint() > 0) {

				break;
			}

			this.melee_count++;
			
			if(this.melee_count%10 != 0) {

				break;
			}

			chrBehaviorEnemy	enemy = other as chrBehaviorEnemy;

			if(enemy == null) {

				break;
			}

			enemy.setRewardItem("candy00", "candy00", null);

		} while(false);
	}

	// ダメージを受けて吹き飛ばされるを開始する.
	public virtual void		beginBlowOut(Vector3 center, float radius)
	{
	}

	// ダメージを受けて吹き飛ばされるを開始する（横方向限定）.
	public virtual void		beginBlowOutSide(Vector3 center, float radius, Vector3 direction)
	{
	}

	// ================================================================ //
	// ビヘイビアーの使うコマンド.
	// イベントボックス.

	// ドアの前にいる間、呼ぶ.
	public void		cmdNotiryStayDoorBox(DoorControl door)
	{
		this.door = door;
	}

	// ================================================================ //

	// ショット撃てる/撃てないをセットする？.
	public void		setShotEnable(bool is_enable)
	{
		this.is_shot_enable = is_enable;
	}

	// モデルのゲームオブジェクトを取得する.
	public GameObject	getModel()
	{
		return(this.gameObject.transform.Find("model").gameObject);
	}

	// モデルの初期状態でのローカルポジションを取得する.
	public Vector3	getInitialLocalPositionModel()
	{
		return(this.initial_local_position_model);
	}

	// アカウント名を取得する.
	public string	getAcountID()
	{
		AccountData		account_data = AccountManager.get().getAccountData(this.control.global_index);

		return(account_data.account_id);
	}

	// グローバルインデックスを取得する.
	public int		getGlobalIndex()
	{
		return(this.control.global_index);
	}

	// ショットタイプをゲットする.
	public SHOT_TYPE	getShotType()
	{
		return(this.shot_type);
	}

	// ショットタイプを変更する.
	public void		changeBulletShooter(SHOT_TYPE shot_type)
	{
		if(shot_type != this.shot_type) {

			if(this.bullet_shooter != null) {

				GameObject.Destroy(this.bullet_shooter);
			}

			this.shot_type = shot_type;
			this.bullet_shooter = BulletShooter.createShooter(this.control, this.shot_type);
		}
	}

	// ショットのパワーアップタイムを開始する.
	public void		startShotBoost()
	{
		this.shot_boost_timer = SHOT_BOOST_TIME;
	}

	// ショットのパワーアップ中？.
	public bool		isShotBoosted()
	{
		return(this.shot_boost_timer > 0.0f);
	}

	// ケーキをとった数を取得する.
	public int	getCakeCount()
	{
		return(this.cake_count);
	}

	// ================================================================ //

	// 頭じんじん状態（アイス食べ過ぎ）を始める.
	public void		startJinJin()
	{
		this.jin_jin_timer.current = 0.0f;
		this.jin_jin_timer.duration = 5.0f;

		if(this.jin_jin_effect == null) {

			this.jin_jin_effect = EffectRoot.get().createJinJinEffect(this.control.getPosition() + new Vector3(0.0f, 2.5f, -0.5f));
		}

		this.skin_color_control.startJinJin();
	}

	// 頭じんじん状態（アイス食べ過ぎ）を終わる.
	public void		stopJinJin()
	{
		this.jin_jin_timer.current  = -1.0f;
		this.jin_jin_timer.duration = -1.0f;

		if(this.jin_jin_effect != null) {

			this.jin_jin_effect.destroy();
			this.jin_jin_effect         = null;
		}

		this.skin_color_control.stopJinJin();
	}

	// 頭じんじん状態（アイス食べ過ぎ）の実行.
	public void		executeJinJin()
	{
		if(this.jin_jin_timer.current >= 0.0f) {

			this.jin_jin_timer.current += Time.deltaTime;

			if(this.jin_jin_timer.current >= this.jin_jin_timer.duration) {

				this.stopJinJin();
			}
		}

		if(this.jin_jin_timer.current >= 0.0f) {

			// エフェクトの位置をプレイヤーに追従させる.
			if(this.jin_jin_effect != null) {
	
				this.jin_jin_effect.transform.position = this.control.getPosition() + new Vector3(0.0f, 2.5f, -0.5f);
			}
		}
	}

	// 頭じんじん状態？.
	public bool		isNowJinJin()
	{
		return(this.jin_jin_timer.current >= 0.0f);
	}

	// ---------------------------------------------------------------- //

	// クリームまみれを始める.
	public void		startCreamy()
	{
		this.skin_color_control.startCreamy();
	}

	// クリームまみれを終わる.
	public void		stopCreamy()
	{
		this.skin_color_control.stopCreamy();
	}

	// クリームまみれ中？.
	public bool		isNowCreamy()
	{
		return(this.skin_color_control.isNowCreamy());
	}

	// ---------------------------------------------------------------- //

	// 体力回復中を始める.
	public void		startHealing()
	{
		this.skin_color_control.startHealing();
	}

	// 体力回復中を終わる.
	public void		stopHealing()
	{
		this.skin_color_control.stopHealing();
	}

	// 体力回復中？.
	public bool		isNowHealing()
	{
		return(this.skin_color_control.isNowHealing());
	}

	public void		execute_queries()
	{
		// 調停の終わったクエリーを探す.
		List<QueryBase> queries = QueryManager.get().findDoneQuery(this.control.getAccountID());

		foreach(QueryBase query in queries) {

			switch(query.getType()) {
				
				case "talk":
				{
				Debug.Log("query talk: " + PartyControl.get().getLocalPlayer().getAcountID());

					if(query.isSuccess()) {
						
						QueryTalk		query_talk = query as QueryTalk;
						
						this.control.cmdDispBalloon(query_talk.words);
					}
					query.set_expired(true);
				}
				break;
			}
			
			// 用済みになったので、削除する.
			query.set_expired(true);
			
			if(!query.isSuccess()) {
				
				continue;
			}
		}

	}
	// ================================================================ //

	// ダメージを受けて吹き飛ばされてる中.
	protected class StepBlowOut {

		public Vector3	center;
		public float	radius;

		public bool		is_side_only;		// 横方向のみ.
		public Vector3	direction;

		public void		begin(Vector3 center, float radius)
		{
			this.center       = center;
			this.radius       = radius;
			this.is_side_only = false;
			this.direction    = Vector3.zero;
		}

		public void		begin(Vector3 center, float radius, Vector3 direction)
		{
			this.center       = center;
			this.radius       = radius;
			this.is_side_only = true;
			this.direction    = direction;
	
			this.direction.y = 0.0f;
			this.direction.Normalize();

			if(this.direction.magnitude == 0.0f) {

				this.is_side_only = false;
			}
		}
	}
	protected StepBlowOut	step_blow_out = new StepBlowOut();

	protected void		exec_step_blow_out()
	{
		Vector3		distance_vector = this.control.getPosition() - this.step_blow_out.center;

		distance_vector.y = 0.0f;

		if(this.step_blow_out.is_side_only) {

			Vector3		parallel = Vector3.Dot(distance_vector, this.step_blow_out.direction)*this.step_blow_out.direction;

			distance_vector -= parallel;
		}

		Rect	room_rect = MapCreator.get().getRoomRect(PartyControl.get().getCurrentRoom().getIndex());

		// ルーム端の壁に近い時は、逆方向に吹き飛ばす.

		if(room_rect.min.y - this.step_blow_out.center.z > -this.step_blow_out.radius) {

			if(distance_vector.z < 0.0f) {

				distance_vector.z *= -1.0f;
			}

		} else if(room_rect.max.y - this.step_blow_out.center.z < this.step_blow_out.radius) {

			if(distance_vector.z > 0.0f) {

				distance_vector.z *= -1.0f;
			}
		}

		if(room_rect.min.x - this.step_blow_out.center.x > -this.step_blow_out.radius) {

			if(distance_vector.x < 0.0f) {

				distance_vector.x *= -1.0f;
			}

		} else if(room_rect.max.x - this.step_blow_out.center.x < this.step_blow_out.radius) {

			if(distance_vector.x > 0.0f) {

				distance_vector.x *= -1.0f;
			}
		}

		//

		float		base_speed = this.step_blow_out.radius/BLOW_OUT_TIME;
		float		speed = base_speed*(Mathf.Max(0.0f, this.step_blow_out.radius - distance_vector.magnitude) + 1.0f)/this.step_blow_out.radius;

		distance_vector.Normalize();

		if(distance_vector.magnitude == 0.0f) {

			distance_vector = Vector3.forward;
		}

		distance_vector *= speed*Time.deltaTime;

		this.control.cmdSetPosition(this.control.getPosition() + distance_vector);
		this.control.cmdSmoothHeadingTo(this.step_blow_out.center, 0.5f);
	}

}
