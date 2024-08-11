using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GameObjectExtension;

// ビヘイビアー　ローカルプレイヤー用.
// マウスでコントロールする.
public class chrBehaviorLocal : chrBehaviorPlayer {

	private Vector3		move_target;				// 移動先の位置.
	private Vector3		heading_target;				// 向く先.

	protected chrBehaviorEnemy	melee_target;		// 近接攻撃する相手.

	// ---------------------------------------------------------------- //

	public enum STEP {

		NONE = -1,

		MOVE = 0,			// 通常時.
		MELEE_ATTACK,		// 近接攻撃.
		USE_ITEM,			// アイテム使用.

		BLOW_OUT,			// ダメージを受けて吹き飛ばされてる中.

		BATAN_Q,			// ばたんきゅー（体力０）.
		WAIT_RESTART,		// リスタートまち.

		OUTER_CONTROL,		// 外部制御.

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	protected StepUseItem	step_use_item = new StepUseItem();		// アイテム使用中の制御.


	// 間引いた座標を保存.
	private List<CharacterCoord>	m_culling = new List<CharacterCoord>();
	// 現在のプロットのインデックス.
	private int 		m_plotIndex = 0;
	// 停止状態のときはデータを送信しないようにする.
	private Vector3		m_prev;


	// 3次スプライン補間で使用する点数.
	private const int	PLOT_NUM = 4;

	// 送信回数.
	private int	m_send_count = 0;

	// ================================================================ //

	// 外部からのコントロールを開始する.
	public override void 	beginOuterControll()
	{
		base.beginOuterControll();

		this.step.set_next(STEP.OUTER_CONTROL);
	}

	// 外部からのコントロールを終了する.
	public override void		endOuterControll()
	{
		base.endOuterControll();

		this.step.set_next(STEP.MOVE);
	}

	// ダメージを受けて吹き飛ばされるを開始する.
	public override void		beginBlowOut(Vector3 center, float radius)
	{
		this.step_blow_out.begin(center, radius);
		this.step.set_next(STEP.BLOW_OUT);
	}

	// ダメージを受けて吹き飛ばされるを開始する（横方向限定）.
	public override void		beginBlowOutSide(Vector3 center, float radius, Vector3 direction)
	{
		this.step_blow_out.begin(center, radius, direction);
		this.step.set_next(STEP.BLOW_OUT);
	}

	// アイテムを使ったときによばれる.
	public override void		onUseItem(int slot, Item.Favor favor)
	{
		base.onUseItem(slot, favor);

		this.step_use_item.player     = this;
		this.step_use_item.slot_index = slot;
		this.step_use_item.item_favor = favor;

		this.step.set_next(STEP.USE_ITEM);
	}

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
	}
	
	void	Update()
	{
	}

	// コリジョンにヒットしている間中よばれるメソッド.
	void 	OnCollisionStay(Collision other)
	{
		switch(other.gameObject.tag) {

			case "Item":
			case "Enemy":
			case "EnemyLair":
			case "Boss":
			{
				CollisionResult	result = new CollisionResult();
		
				result.object0    = this.gameObject;
				result.object1    = other.gameObject;
				result.is_trigger = false;

				this.control.collision_results.Add(result);
			}
			break;
		}
	}

	// トリガーにヒットした瞬間だけよばれるメソッド.
	void	OnTriggerEnter(Collider other)
	{

		this.on_trigger_common(other);
	}
	// トリガーにヒットしている間中よばれるメソッド.
	void	OnTriggerStay(Collider other)
	{
		this.on_trigger_common(other);
	}

	protected	void	on_trigger_common(Collider other)
	{
		switch(other.gameObject.tag) {

			case "Door":
			case "Item":
			{
				CollisionResult	result = new CollisionResult();
		
				result.object0    = this.gameObject;
				result.object1    = other.gameObject;
				result.is_trigger = true;

				this.control.collision_results.Add(result);
			}
			break;
		}
	}

	// ================================================================ //

	public override void	initialize()
	{
		base.initialize();

		this.move_target = this.transform.position;
	}
	public override void	start()
	{
		base.start();

		// ゲーム開始直後に EnterEvent が始まると、ここで next_step に.
		// OuterControll がセットされている。そのときに上書きしないように、.
		// next == NONE のチェックを入れる.
		if(this.step.get_next() == STEP.NONE) {

			this.step.set_next(STEP.MOVE);
		}

		this.control.cmdSetAcceptDamage(true);

		this.GetComponent<Rigidbody>().WakeUp();
	}

	public override	void	execute()
	{
		base.execute();

		this.resolve_collision();

		this.update_item_queries();

		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.


		switch(this.step.do_transition()) {

			// 通常時.
			case STEP.MOVE:
			{
				if(this.control.vital.getHitPoint() <= 0.0f) {

					this.step.set_next(STEP.BATAN_Q);
				}
			}
			break;

			// 近接攻撃.
			case STEP.MELEE_ATTACK:
			{
				if(!this.melee_attack.isAttacking()) {

					this.step.set_next(STEP.MOVE);
				}
			}
			break;

			// アイテム使用.
			case STEP.USE_ITEM:
			{
				if(this.step_use_item.transition_check()) {

					this.ice_timer = ICE_DIGEST_TIME;
				}
			}
			break;

			// ダメージを受けて吹き飛ばされてる中.
			case STEP.BLOW_OUT:
			{
				// 一定距離進むか時間切れで終了.

				float	distance = MathUtility.calcDistanceXZ(this.control.getPosition(), this.step_blow_out.center);

				if(distance >= this.step_blow_out.radius || this.step.get_time() > BLOW_OUT_TIME) {

					this.control.cmdSetAcceptDamage(true);
					this.step.set_next(STEP.MOVE);
				}
			}
			break;

			// ばたんきゅー（体力０）.
			case STEP.BATAN_Q:
			{
				if(this.control.getMotion() == "") {

					this.control.cmdSetMotion("m007_out_lp", 1);
					this.step.set_next_delay(STEP.WAIT_RESTART, 1.0f);
				}
			}
			break;

		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {

				// 通常時.	
				case STEP.MOVE:
				{
				this.GetComponent<Rigidbody>().WakeUp();
					this.move_target = this.transform.position;
					this.heading_target = this.transform.TransformPoint(Vector3.forward);
				}
				break;

				// 近接攻撃.
				case STEP.MELEE_ATTACK:
				{
					this.melee_attack.setTarget(this.melee_target);
					this.melee_attack.attack(true);
					this.melee_target = null;
				}
				break;

				// アイテム使用.
				case STEP.USE_ITEM:
				{
					int		slot_index = this.step_use_item.slot_index;

					if(this.ice_timer > 0.0f) {

						// アイスを短い間隔で連続して使ったとき.
						// 頭がじんじんして、回復はしない.
					
						// アイテムを削除する.
						ItemWindow.get().clearItem(Item.SLOT_TYPE.MISC, slot_index);
						this.item_slot.miscs[slot_index].initialize();

						this.startJinJin();
						this.step.set_next(STEP.MOVE);

						SoundManager.getInstance().playSE(Sound.ID.DDG_SE_SYS06);

					} else {

						this.item_slot.miscs[slot_index].is_using = true;

						this.control.cmdSetAcceptDamage(false);

						this.step_use_item.initialize();
					}
				}
				break;

				// ダメージを受けて吹き飛ばされてる中.
				case STEP.BLOW_OUT:
				{
					this.GetComponent<Rigidbody>().Sleep();
					this.control.cmdSetAcceptDamage(false);
				}
				break;

				// ばたんきゅー（体力０）.
				case STEP.BATAN_Q:
				{
					this.GetComponent<Rigidbody>().Sleep();
					this.control.cmdSetAcceptDamage(false);
					this.control.cmdSetMotion("m006_out", 1);
				}
				break;

				// リスタートまち.
				case STEP.WAIT_RESTART:
				{
					this.step_batan_q.tears_effect.destroy();
				}
				break;

				// 外部制御.
				case STEP.OUTER_CONTROL:
				{
					this.GetComponent<Rigidbody>().Sleep();
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		// 近接攻撃.
		// 一度 false にしておいて、移動入力があったときのみ
		// true にする.
		this.melee_attack.setHasInput(false);

		GameInput	gi = GameInput.getInstance();

		switch(this.step.do_execution(Time.deltaTime)) {

			// 通常時.
			case STEP.MOVE:
			{
				this.exec_step_move();

				// ショット.
				if(this.is_shot_enable) {

					this.bullet_shooter.execute(gi.shot.current);

					if(gi.shot.current) {
	
						CharacterRoot.get().SendAttackData(PartyControl.get().getLocalPlayer().getAcountID(), 0);
					}
				}

				// 体力回復直後（レインボーカラー中）は無敵.
				if(this.skin_color_control.isNowHealing()) {

					this.control.cmdSetAcceptDamage(false);

				} else {

					this.control.cmdSetAcceptDamage(true);
				}
			}
			break;

			// 近接攻撃.
			case STEP.MELEE_ATTACK:
			{
				Vector3		position = this.control.getPosition();

				position.y = 0.0f;

				this.control.cmdSetPosition(position);
			}
			break;

			// アイテム使用.
			case STEP.USE_ITEM:
			{
				this.step_use_item.execute();
			}
			break;

			// ダメージを受けて吹き飛ばされてる中.
			case STEP.BLOW_OUT:
			{
				this.exec_step_blow_out();
			}
			break;

			// ばたんきゅー（体力０）.
			case STEP.BATAN_Q:
			{
				if(this.step.is_acrossing_time(4.0f)) {

					this.step_batan_q.tears_effect = EffectRoot.get().createTearsEffect(this.control.getPosition());

					this.step_batan_q.tears_effect.setParent(this.gameObject);
					this.step_batan_q.tears_effect.setLocalPosition(Vector3.up);
				}
			}
			break;

		}

		// ---------------------------------------------------------------- //

		if(gi.serif_text.trigger_on) {

			this.control.cmdQueryTalk(gi.serif_text.text, true);
		}

		// ---------------------------------------------------------------- //
		// １０フレームに１回、座標をネットに送る.
		
		{
			do {

				if(this.step.get_current() == STEP.OUTER_CONTROL) {

					break;
				}
				
				m_send_count = (m_send_count + 1)%SplineData.SEND_INTERVAL;
				
				if(m_send_count != 0 && m_culling.Count < PLOT_NUM) {
					
					break;
				}
				
				// 通信用座標送信.
				Vector3 target = this.control.getPosition();
				CharacterCoord coord = new CharacterCoord(target.x, target.z);
				
				Vector3 diff = m_prev - target;
				if (diff.sqrMagnitude > 0.0001f) {
					
					m_culling.Add(coord);

					AccountData	account_data = AccountManager.get().getAccountData(GlobalParam.getInstance().global_account_id);

					CharacterRoot.get().SendCharacterCoord(account_data.avator_id, m_plotIndex, m_culling); 
					++m_plotIndex;
	
					if (m_culling.Count >= PLOT_NUM) {
						
						m_culling.RemoveAt(0);
					}
					
					m_prev = target;
				}
				
			} while(false);
		}

	}

	// ダメージを受けたときに呼ばれる.
	public override void		onDamaged()
	{
		this.control.cmdSetMotion("m005_damage", 1);

		EffectRoot.get().createHitEffect(this.transform.position);
	}

	// ================================================================ //

	// コリジョンの意味づけ.
	protected void	resolve_collision()
	{
		foreach(var result in this.control.collision_results) {

			if(result.object1 == null) {
							
				continue;
			}

			//GameObject		self  = result.object0;
			GameObject		other = result.object1;

			// プレイヤーがボスのコリジョンに埋まって動けなくならないように.
			// ルームの中心方向に押し出す.
			if(other.tag == "Boss") {

				if(this.force_push_out(result)) {

					continue;
				}
			}

			switch(other.tag) {

				case "Enemy":
				case "EnemyLair":
				case "Boss":
				{
					do {

						chrBehaviorEnemy	enemy = other.GetComponent<chrBehaviorEnemy>();

						if(enemy == null) {

							break;
						}
						if(!this.melee_attack.isInAttackRange(enemy.control)) {

							//break;
						}
						if(this.step.get_current() == STEP.MELEE_ATTACK) {

							break;
						}
						if(!this.melee_attack.isHasInput()) {

							break;
						}

						this.melee_target = enemy;
						this.step.set_next(STEP.MELEE_ATTACK);

					} while(false);

					result.object1 = null;
				}
				break;

				case "Door":
				{
					this.cmdNotiryStayDoorBox(other.gameObject.GetComponent<DoorControl>());
				}
				break;

				case "Item":
				{
					do {

						ItemController		item  = other.GetComponent<ItemController>();

						// アイテムを拾えるか、調べる.
						bool	is_pickable = true;

						switch(item.behavior.item_favor.category) {
			
							case Item.CATEGORY.CANDY:
							{
								is_pickable = this.item_slot.candy.isVacant();
							}
							break;
			
							case Item.CATEGORY.SODA_ICE:
							case Item.CATEGORY.ETC:
							{
								int		slot_index = this.item_slot.getEmptyMiscSlot();
		
								// スロットがいっぱい＝もう持てないとき.	
								if(slot_index < 0) {
	
									is_pickable = false;
								}
							}
							break;

							case Item.CATEGORY.WEAPON:
							{
								// 使用中のショットと同じだったら拾えない.
								SHOT_TYPE	shot_type = Item.Weapon.getShotType(item.name);
			
								is_pickable = (this.shot_type != shot_type);
							}
							break;
						}
						if(!is_pickable) {

							break;
						}
			
						this.control.cmdItemQueryPick(item.name, true, false);

					} while(false);
				}
				break;
			}
		}

	}

	// プレイヤーがボスのコリジョンに埋まって動けなくならないように.
	// ルームの中心方向に押し出す.
	protected bool	force_push_out(CollisionResult result)
	{
		bool	is_pushouted = false;


		do {

			if(result.is_trigger) {

				break;
			}

			GameObject	other = result.object1;

			chrControllerEnemyBoss	control_other = other.GetComponent<chrControllerEnemyBoss>();

			if(control_other == null) {

				break;
			}

			// これ以上近かったら、強制押し出しをする.
			float		distance_limit = 2.0f*control_other.getScale();

			if(Vector3.Distance(other.transform.position, this.control.getPosition()) >= distance_limit) {

				break;
			}

			Vector3		room_center = MapCreator.get().getRoomCenterPosition(PartyControl.get().getCurrentRoom().getIndex());

			Vector3 	v = room_center - other.transform.position;

			v.Normalize();

			if(v.magnitude == 0.0f) {

				v = Vector3.forward;
			}

			this.control.cmdSetPositionAnon(other.transform.position + v*distance_limit*2.0f);

			is_pushouted = true;

		} while(false);

		return(is_pushouted);
	}

	// ---------------------------------------------------------------- //
	// アイテムのクエリーを更新する.

	private void	update_item_queries()
	{
		List<QueryBase>		done_queries = QueryManager.get().findDoneQuery(this.control.getAccountID());

		foreach(var query in done_queries) {

			switch(query.getType()) {

				case "item.pick":
				{
					dbwin.console().print("item query done.");
					this.resolve_pick_item_query(query as QueryItemPick);
				}
				break;
			}
		}
	}

	private void	resolve_pick_item_query(QueryItemPick query)
	{
		do {

			if(!query.isSuccess()) {

				break;
			}

			// アイテムの効能だけコピーして、削除する.

			ItemController	item = this.control.cmdItemPick(query, query.target);

			if(item == null) {

				break;
			}

			// エフェクト.
			EffectRoot.get().createItemGetEffect(this.control.getPosition());

			SoundManager.get().playSE(Sound.ID.DDG_SE_SYS02);

			switch(item.behavior.item_favor.category) {

				case Item.CATEGORY.CANDY:
				{
					// アイテムウインドウにアイコンを表示.
					this.item_slot.candy.favor = item.behavior.item_favor.clone();

					ItemWindow.get().setItem(Item.SLOT_TYPE.CANDY, 0, this.item_slot.candy.favor);

					// ショットの一定時間パワーアップ.
					this.startShotBoost();
				}
				break;

				case Item.CATEGORY.SODA_ICE:
				case Item.CATEGORY.ETC:
				{
					// 空きスロットにアイテムをセット.
					int		slot_index = this.item_slot.getEmptyMiscSlot();

					if(slot_index >= 0) {

						this.item_slot.miscs[slot_index].item_id = query.target;
						this.item_slot.miscs[slot_index].favor   = item.behavior.item_favor.clone();

						// アイテムウインドウにアイコンを表示.
						ItemWindow.get().setItem(Item.SLOT_TYPE.MISC, slot_index, this.item_slot.miscs[slot_index].favor);
					}
				}
				break;

				case Item.CATEGORY.FOOD:
				{
					// 体力回復.
					if(GameRoot.get().isNowCakeBiking()) {

						this.control.vital.healFullInternal();

					} else {

						this.control.vital.healFull();

						// レインボーカラーエフェクト.
						this.skin_color_control.startHealing();
					}

					// 体力回復を通知.
					CharacterRoot.get().NotifyHitPoint(this.getAcountID(), this.control.vital.getHitPoint());

					// アイテム破棄を通知.
					this.control.cmdItemDrop(query.target);

					// ケーキをとった数こうしん（ケーキバイキング用）.
					this.cake_count++;
				}
				break;

				// ルームキー.
				case Item.CATEGORY.KEY:
				{
					PartyControl.get().getLocalPlayer().control.consumeKey(item);

					Item.KEY_COLOR	key_color = Item.Key.getColorFromInstanceName(item.name);

					// アイテムウインドウにアイコンを表示.
					if(key_color != Item.KEY_COLOR.NONE) {

						ItemWindow.get().setItem(Item.SLOT_TYPE.KEY, (int)key_color, item.behavior.item_favor);
					}
				}
				break;

				// フロアー移動ドアのカギ.
				case Item.CATEGORY.FLOOR_KEY:
				{
					MapCreator.getInstance().UnlockBossDoor();

					// アイテムウインドウにアイコンを表示.
					ItemWindow.get().setItem(Item.SLOT_TYPE.FLOOR_KEY, 0, item.behavior.item_favor);
				}
				break;

				case Item.CATEGORY.WEAPON:
				{
					// ショットを変更（ねぎバルカン/ゆずボム）.
					SHOT_TYPE	shot_type = Item.Weapon.getShotType(item.name);

					if(shot_type != SHOT_TYPE.NONE) {

						this.changeBulletShooter(shot_type);
					}
				}
				break;
			}

			item.vanish();

		} while(false);

		query.set_expired(true);
	}

	// ================================================================ //

	// STEP.MOVE の実行.
	// 移動.
	protected void	exec_step_move()
	{
		GameInput	gi = GameInput.getInstance();

		// ---------------------------------------------------------------- //
		//移動目標位置を更新する.

		if(gi.pointing.current) {

			switch(gi.pointing.pointee) {


				case GameInput.POINTEE.CHARACTOR:
				case GameInput.POINTEE.NONE:
				{
				}
				break;

				default:
				{
					if(GameRoot.getInstance().controlable[this.control.local_index]) {

						this.move_target = gi.pointing.position_3d;
					}
				}
				break;
			}

			// 近接攻撃.
			this.melee_attack.setHasInput(true);

		} else {

			this.move_target = this.control.getPosition();
		}

		if(gi.shot.current) {

			if(gi.shot.pointee != GameInput.POINTEE.NONE) {

				this.heading_target = gi.shot.position_3d;
			}

		} else if(gi.pointing.current) {

			if(gi.pointing.pointee != GameInput.POINTEE.NONE) {

				this.heading_target = gi.pointing.position_3d;
			}
		}

		// ---------------------------------------------------------------- //
		// 移動（位置座標の補間）.

		Vector3		position  = this.control.getPosition();
		Vector3		dist      = this.move_target - position;

		dist.y = 0.0f;

		float		speed = 5.0f;
		float		speed_per_frame = speed*Time.deltaTime;

		if(dist.magnitude < speed_per_frame) {

			// 立ち止まる.
			this.control.cmdSetMotion("m002_idle", 0);

			dist = Vector3.zero;

		} else {

			// 歩く.
			this.control.cmdSetMotion("m001_walk", 0);

			dist *= (speed_per_frame)/dist.magnitude;
		}

		position += dist;
		//position.y = this.control.getPosition().y;
		position.y = 0.0f;

		this.control.cmdSetPosition(position);

		// 向きの補間.

		float	turn_rate = 0.1f;

		if(!gi.pointing.current && gi.shot.trigger_on) {

			turn_rate = 1.0f;
		}

		this.control.cmdSmoothHeadingTo(this.heading_target, turn_rate);
	}

	// ================================================================ //

	// アイテム使用中の制御.
	protected class StepUseItem {

		public int					slot_index;		// 使用中のアイテムが入っているスロット.
		public Item.Favor			item_favor;
		public chrBehaviorLocal		player;

		public static float	use_motion_delay  = 0.4f;
		public static float	heal_effect_delay = 0.9f;

		// ================================================================ //

		// スタート.
		public void		initialize()
		{
			EffectRoot.get().createHealEffect(this.player.control.getPosition());
		}

		// 終わりチェック.
		public bool		transition_check()
		{
			bool	is_transit = false;
	
			if(this.player.step.get_time() >= use_motion_delay && this.player.control.getMotion() != "m004_use") {

				ItemWindow.get().clearItem(Item.SLOT_TYPE.MISC, this.slot_index);

				bool	is_atari = (bool)this.item_favor.option0;

				if(is_atari) {

					// 当たりイベント.
					EventIceAtari	event_atari = EventRoot.get().startEvent<EventIceAtari>();

					event_atari.setItemSlotAndFavor(this.slot_index, this.item_favor);

					this.player.step.set_next(STEP.MOVE);

				} else {

					this.player.step.set_next(STEP.MOVE);
				}

				// アイテムを削除する.
				if(this.slot_index >= 0) {

					if(is_atari) {

						// あたりのときはアイテムを削除しない.
						// はずれに戻す.
						this.player.item_slot.miscs[this.slot_index].favor.option0 = false;

					} else {

						this.player.item_slot.miscs[this.slot_index].initialize();
	
						this.slot_index = -1;
					}
				}

				is_transit = true;
			}

			return(is_transit);
		}

		// 毎フレームの実行.
		public void		execute()
		{
			// エフェクトに少し遅れてモーション開始.
			if(this.player.step.is_acrossing_time(use_motion_delay)) {

				this.player.control.cmdSetMotion("m004_use", 1);
			}

			// ねぎを上にあげたタイミングでアイテムの効果を発動.
			if(this.player.step.is_acrossing_time(heal_effect_delay)) {

				switch(this.item_favor.category) {

					case Item.CATEGORY.SODA_ICE:
					{
						this.player.control.vital.healFull();
						this.player.skin_color_control.startHealing();

						// 近くにいた仲間も体力回復.
						for(int i = 0;i < PartyControl.get().getFriendCount();i++) {

							chrBehaviorPlayer	friend = PartyControl.get().getFriend(i);

							float	distance = (friend.control.getPosition() - this.player.control.getPosition()).magnitude;

							if(distance > chrBehaviorPlayer.CHARITY_SPHERE_RADIUS) {

								continue;
							}

							this.player.control.cmdUseItemToFriend(this.item_favor, friend.control.global_index, true);
						}
					}
					break;
				}
			}
		}
	}

}
