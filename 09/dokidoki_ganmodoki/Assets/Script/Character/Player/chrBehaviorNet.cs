using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ビヘイビアー　なんちゃってネットプレイヤー（ゲスト）用.
public class chrBehaviorNet : chrBehaviorPlayer {

	protected string	move_target_item = "";	// アイテムを目指して移動しているとき.

	protected string	collision = "";

	public chrBehaviorLocal	local_player          = null;

	// ---------------------------------------------------------------- //

	public enum STEP {

		NONE = -1,

		MOVE = 0,			// 通常時.
		MELEE_ATTACK,		// 近接攻撃.
		OUTER_CONTROL,		// 外部制御.

		NUM,
	};

	//public Step<STEP>	step = new Step<STEP>(STEP.NONE);
	public STEP			step      = STEP.NONE;
	public STEP			next_step = STEP.NONE;
	public float		step_timer = 0.0f;


	// ---------------------------------------------------------------- //
	
	// 3次スプライン補間で使用する点数.
	private const int PLOT_NUM = 4;
	
	// 間引きする座標のフレーム数.
	private const int CULLING_NUM = 10;
	
	// 現在のプロットのインデックス.
	private int 	m_plotIndex = 0;
	
	// 間引いた座標を保存.
	private List<CharacterCoord>	m_culling = new List<CharacterCoord>();
	// 補間した座標を保存.
	private List<CharacterCoord>	m_plots = new List<CharacterCoord>();

	// 攻撃フラグ.
	private	bool	m_shot = false;

	// 歩きモーション.
	private struct WalkMotion {

		public bool		is_walking;
		public float	timer;
	};
	private	WalkMotion	walk_motion;

	private const float	STOP_WALK_WAIT = 0.1f;		// [sec] 歩き -> 立ちモーションに移行するときの猶予時間.

	// ================================================================ //

	// ローカルプレイヤー？.
	public override bool	isLocal()
	{
		return(false);
	}

	// 外部からのコントロールを開始する.
	public override void 	beginOuterControll()
	{
		this.next_step = STEP.OUTER_CONTROL;
	}

	// 外部からのコントロールを終了する.
	public override void		endOuterControll()
	{
		this.next_step = STEP.MOVE;
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


			case "Enemy":
			case "Boss":
			{
				CollisionResult	result = new CollisionResult();
		
				result.object0    = this.gameObject;
				result.object1    = other.gameObject;
				result.is_trigger = false;

				CollisionManager.getInstance().results.Add(result);
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

			case "Item":
			{
				if(GameRoot.get().isNowCakeBiking()) {

					CollisionResult	result = new CollisionResult();
			
					result.object0    = this.gameObject;
					result.object1    = other.gameObject;
					result.is_trigger = true;
	
					this.control.collision_results.Add(result);

				} else {

					// ケーキバイキング以外（通常ゲーム中）のときは、リモートは
					// アイテムは拾えない.
				}
			}
			break;

			case "Door":
			{
				CollisionResult	result = new CollisionResult();
		
				result.object0    = this.gameObject;
				result.object1    = other.gameObject;
				result.is_trigger = true;

				CollisionManager.getInstance().results.Add(result);
			}
			break;
		}
	}

	// ================================================================ //

	public override void	initialize()
	{
		base.initialize();

		this.walk_motion.is_walking = false;
		this.walk_motion.timer      = 0.0f;
	}
	public override void	start()
	{
		base.start();

		this.next_step = STEP.MOVE;
	}
	public override	void	execute()
	{
		base.execute();

		// ケーキバイキングのときだけリモートはアイテムは拾えるのでコリジョンの処理を行います.
		// それ以外(通常ゲーム中)は被ダメージやアイテム取得は行いません.
		this.resolve_collision();

		this.update_item_queries();

		// ---------------------------------------------------------------- //
		// ステップ内の経過時間を進める.
		
		this.step_timer += Time.deltaTime;
		
		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.
		
		if(this.next_step == STEP.NONE) {
			
			switch(this.step) {

				case STEP.MOVE:
				{
				}
				break;
				
				// 2014.10.14 追加.
				case STEP.MELEE_ATTACK:
				{
					if(!this.melee_attack.isAttacking()) {
							
						//this.step.set_next(STEP.MOVE);
						this.next_step = STEP.MOVE;
					}
				}
				break;
			}
		}
		
		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.
		
		while(this.next_step != STEP.NONE) {

			STEP prev	   = this.step;
			this.step      = this.next_step;
			this.next_step = STEP.NONE;
			
			switch(this.step) {
				
				case STEP.OUTER_CONTROL:
				{
					this.GetComponent<Rigidbody>().Sleep();
				}
				break;
				
				case STEP.MOVE:
				{
					if (prev == STEP.OUTER_CONTROL) {
						m_culling.Clear();
						m_plots.Clear();
					}
				}
				break;
				
				case STEP.MELEE_ATTACK:
				{
					this.melee_attack.setHasInput(true);
					this.melee_attack.attack(false);
				}
				break;
			}
			
			this.step_timer = 0.0f;
		}
		
		// ---------------------------------------------------------------- //
		// 各状態での実行処理.
		this.melee_attack.setHasInput(false);

		switch(this.step) {
			
			case STEP.MOVE:
			{
				this.exec_step_move();
			}
			break;

			case STEP.MELEE_ATTACK:
			{
			}
			break;	
		}
	
		if(this.is_shot_enable) {
	
			this.bullet_shooter.execute(m_shot);
		}
		m_shot = false;
		
		this.collision = "";
		
		// ---------------------------------------------------------------- //
	}

	// 移動に関する処理.
	protected void	exec_step_move()
	{
		Vector3		new_position = this.control.getPosition();
		if(m_plots.Count > 0) {
			CharacterCoord coord = m_plots[0];
			new_position = new Vector3(coord.x, new_position.y, coord.z);
			m_plots.RemoveAt(0);
		}

		// 一瞬とまっただけのときは歩きモーションがとまらないようにする.

		bool	is_walking = this.walk_motion.is_walking;

		if(Vector3.Distance(new_position, this.control.getPosition()) > 0.0f) {

			this.control.cmdSmoothHeadingTo(new_position);
			this.control.cmdSetPosition(new_position);

			is_walking = true;

		} else {

			is_walking = false;
		}

		if(this.walk_motion.is_walking && !is_walking) {

			this.walk_motion.timer -= Time.deltaTime;

			if(this.walk_motion.timer <= 0.0f) {

				this.walk_motion.is_walking = is_walking;
				this.walk_motion.timer      = STOP_WALK_WAIT;
			}

		} else {

			this.walk_motion.is_walking = is_walking;
			this.walk_motion.timer      = STOP_WALK_WAIT;
		}

		if(this.walk_motion.is_walking) {
			
			// 歩きモーション.
			this.control.cmdSetMotion("m001_walk", 0);
			
		} else {
			
			// 立ち止まりモーション.
			this.control.cmdSetMotion("m002_idle", 0);
		}
	}

	// ---------------------------------------------------------------- //
	// コリジョンの意味づけ.
	private void	resolve_collision()
	{
		foreach(var result in this.control.collision_results) {

			if(result.object1 == null) {
							
				continue;
			}

			//GameObject		self  = result.object0;
			GameObject		other = result.object1;

			switch(other.tag) {


				case "Item":
				{
					do {

						// ケーキバイキング以外（通常ゲーム中）のときは、リモートは
						// アイテムは拾えない.
						if(!GameRoot.get().isNowCakeBiking()) {

							break;		
						}

						ItemController		item  = other.GetComponent<ItemController>();
	
						this.control.cmdItemQueryPick(item.name, true, true);

					} while(false);
				}
				break;
			}
		}

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

			Debug.Log("Item favor category:" + item.behavior.item_favor.category);
			switch(item.behavior.item_favor.category) {

				case Item.CATEGORY.FOOD:
				{
					this.control.vital.healFull();
					
					this.skin_color_control.startHealing();

					this.cake_count++;
				}
				break;

				case Item.CATEGORY.KEY:
				{
					PartyControl.get().getLocalPlayer().control.consumeKey(item);
				}
				break;	
				
				case Item.CATEGORY.FLOOR_KEY:
				{
					PartyControl.get().getLocalPlayer().control.consumeKey(item);
				}
				break;	

				case Item.CATEGORY.CANDY:
				{
					this.startShotBoost();
				}
				break;

				case Item.CATEGORY.WEAPON:
				{
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

	public override void onDamaged()
	{
		this.control.cmdSetMotion("m005_damage", 1);
		
		EffectRoot.get().createHitEffect(this.transform.position);
	}

	// ================================================================ //

	public void cmdMeleeAttack()
	{
		this.next_step = STEP.MELEE_ATTACK;
		Debug.Log("Command Melee attack.");
	}

	public void cmdShotAttack()
	{
		m_shot = true;
	}

	public void CalcCoordinates(int index, CharacterCoord[] data)
	{
		// 受信した座標を保存.
		do {

			// データーが空っぽ（念のため）.
			if(data.Length <= 0) {

				break;
			}

			// 新しいデーターがない.
			if(index <= m_plotIndex) {
	
				break;
			}

			// m_plotIndex ... m_culling[] の最後の頂点のインデックス.
			// index       ... data[] の最後の頂点のインデックス.
			//
			// index - m_plotIndex ... 今回新たに追加された頂点の数.
			//
			int		s = data.Length - (index - m_plotIndex);

			if(s < 0) {

				break;
			}

			for(int i = s;i < data.Length;i++) {
	
				m_culling.Add(data[i]);
			}

			// m_culling[] の最後の頂点のインデックス.
			m_plotIndex = index;

			// スプライン曲線を求めて補間する.	
			SplineData	spline = new SplineData();
			spline.CalcSpline(m_culling);
			
			// 求めたスプライン補間を座標情報として保存する.
			CharacterCoord plot = new CharacterCoord();
			for (int i = 0; i < spline.GetPlotNum(); ++i) {
				spline.GetPoint(i, out plot);

				m_plots.Add(plot);
			}
			
			// 一番古い座標を削除.
			if (m_culling.Count > PLOT_NUM) {

				m_culling.RemoveRange(0, m_culling.Count - PLOT_NUM);
			}

		} while(false);
	
	}

}
