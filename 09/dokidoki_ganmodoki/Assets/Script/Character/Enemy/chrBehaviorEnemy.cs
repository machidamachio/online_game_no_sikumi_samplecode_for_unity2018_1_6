using UnityEngine;
using System.Collections;
using MathExtension;

// 敵のビヘイビアー基底クラス.
public class chrBehaviorEnemy : chrBehaviorBase {


	public bool		is_attack_motion_finished = false;		// 攻撃モーションが終わった？.
	public bool		is_attack_motion_impact = false;

	// ---------------------------------------------------------------- //

	public Character.BasicAction	basic_action  = new Character.BasicAction();		// 敵共通の基本アクション.

	public Enemy.BEHAVE_KIND		behave_kind   = Enemy.BEHAVE_KIND.BOTTACHI;
	public Character.ActionBase		unique_action = new Character.BottachiAction();

	// 敵を倒したときに出現するアイテム.
	protected struct RewardItem {

		public string		type;		// タイプ.
		public string		name;		// 名まえ.
		public object		option0;	// オプション引数０
	}
	protected RewardItem	reward_item;

	// ================================================================ //

	/// <summary>
	/// 真のとき、エネミーの思考・判断が停止する.
	/// </summary>
	protected bool isPaused;

	public RoomController	room;		// 自分がいる部屋.

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Awake()
	{
	}

	void	Start()
	{
	}
	
	void	Update()
	{
	}

	void 	OnCollisionEnter(Collision other)
	{
		switch(other.gameObject.tag) {

			case "Player":
			case "Wall":
			{
				CollisionResult	result = new CollisionResult();
		
				result.object0    = this.gameObject;
				result.object1    = other.gameObject;
				result.is_trigger = false;

				if(other.contacts.Length > 0) {

					result.option0 = (object)other.contacts[0];
				}

				this.control.collision_results.Add(result);

			}
			break;
		}
	}

	// ================================================================ //

	// 生成直後に呼ばれる.
	public override void	initialize()
	{
		this.reward_item.type = "";
		this.reward_item.name = "";

		this.GetComponent<Rigidbody>().useGravity = false;
		
		this.basic_action.create(this);
	}

	// ゲーム開始時に一回だけ呼ばれる.
	public override void	start()
	{
		this.basic_action.start();
	}

	// 毎フレームよばれる.
	public override void	execute()
	{
	}

	// AIの種類（行動パターン）をセットする.
	public void		setBehaveKind(Enemy.BEHAVE_KIND behave_kind, Character.ActionBase.DescBase desc_base = null)
	{
		Character.ActionBase	unique_action = null;

		switch(behave_kind) {

			default:
			case Enemy.BEHAVE_KIND.BOTTACHI:		unique_action = new Character.BottachiAction();			break;
			case Enemy.BEHAVE_KIND.OUFUKU:			unique_action = new Character.OufukuAction();			break;
			case Enemy.BEHAVE_KIND.UROURO:			unique_action = new Character.UroUroAction();			break;
			case Enemy.BEHAVE_KIND.TOTUGEKI:		unique_action = new Character.TotugekiAction();			break;
			case Enemy.BEHAVE_KIND.SONOBA_DE_FIRE:	unique_action = new Character.SonobaDeFireAction();		break;
			case Enemy.BEHAVE_KIND.WARP_DE_FIRE:	unique_action = new Character.WarpDeFireAction();		break;
			case Enemy.BEHAVE_KIND.JUMBO:			unique_action = new Character.JumboAction();			break;
			case Enemy.BEHAVE_KIND.GOROGORO:		unique_action = new Character.GoroGoroAction();			break;
		}

		if(unique_action != null) {

			if(desc_base == null) {

				desc_base = new Character.ActionBase.DescBase();
			}

			this.behave_kind = behave_kind;

			this.unique_action = unique_action;
			this.unique_action.create(this, desc_base);

			this.basic_action.unique_action = this.unique_action;
		}
	}

	// ================================================================ //

	// ダメージ.
	public virtual void		causeDamage()
	{
	}

	// やられたことにする.
	public virtual void		causeVanish()
	{
		this.basic_action.step.set_next(Character.BasicAction.STEP.VANISH);
	}

	// ================================================================ //

	// 自分が近接攻撃.
	public virtual void		onMeleeAttack(chrBehaviorPlayer player)
	{
	}
	// やられたときに呼ばれる.
	public override void		onVanished()
	{
	}

	// 削除する直前に呼ばれる.
	public override void		onDelete()
	{
		base.onDelete();

		// アイテムを生成する.
		if(this.reward_item.type != "" && this.reward_item.name != "") {

			string	local_player_id = PartyControl.get().getLocalPlayer().getAcountID();
	
			ItemManager.get().createItem(this.reward_item.type, this.reward_item.name, local_player_id);
			ItemManager.get().setPositionToItem(this.reward_item.name, this.control.getPosition());

			var		item = ItemManager.get().findItem(this.reward_item.name);

			if(item != null) {

				item.behavior.setFavorOption(this.reward_item.option0);
				item.behavior.beginSpawn();
			}
		}

		if(this.room != null) {

			LevelControl.get().onDeleteEnemy(this.room, this.control);
		}
	}

	// ダメージをうけたときに呼ばれる.
	public override void		onDamaged()
	{
		if(this.control.vital.hit_point <= 0.0f) {

			if(this.basic_action.step.get_current() != Character.BasicAction.STEP.VANISH) {

				this.basic_action.step.set_next(Character.BasicAction.STEP.VANISH);
			}
		}
	}

	// その場で立ち止まる.
	public void		beginStill()
	{
		this.basic_action.step.set_next(Character.BasicAction.STEP.STILL);
	}

	// その場で立ち止まりを解除する.
	public void		endStill(float delay = 0.0f)
	{
		this.basic_action.step.set_next_delay(Character.BasicAction.STEP.UNIQUE, delay);
	}

	// ================================================================ //
	// 外部からコールされるメソッド.
		
	/// <summary>
	/// ビヘイビアの一時停止を設定します.
	/// </summary>
	/// <param name="newPause">真のときポーズが働き、偽のときポーズが解除されます</param>
	public void SetPause(bool newPause)
	{
		isPaused = newPause;
	}

	// スポーン（箱から飛び出す）アクションを開始する.
	public void		beginSpawn(Vector3 start, Vector3 dir_vector)
	{
		this.basic_action.beginSpawn(start, dir_vector);
	}

	// 敵を倒したときに出現するアイテムをセットする.
	public void		setRewardItem(string type, string name, object favor_option0)
	{
		this.reward_item.type    = type;
		this.reward_item.name    = name;
		this.reward_item.option0 = favor_option0;
	}

	// 生成されたルームをセットする.
	public void		setRoom(RoomController room)
	{
		this.room = room;
	}

	// 自分を削除する.
	public void		deleteSelf()
	{
		this.onDelete();

		EnemyRoot.getInstance().deleteEnemy(this.control);
	}

	// ================================================================ //

	// 攻撃するプレイヤーを選ぶ.
	public chrBehaviorPlayer	selectTargetPlayer(float distance_limit, float angle_limit)
	{
		chrBehaviorPlayer	target = null;

		// 攻撃可能範囲にいて、かつ一番近く、正面にいるプレイヤーを.
		// 探す.

		var players = PartyControl.get().getPlayers();

		float	min_score = -1.0f;

		foreach(var player in players) {

			// 距離を調べる.

			float	distance = (this.control.getPosition() - player.control.getPosition()).Y(0.0f).magnitude;

			if(distance >= distance_limit) {
					
					continue;
			}

			// 正面からどれくらい方向がずれてる？

			float	angle = MathUtility.calcDirection(this.control.getPosition(), player.control.getPosition());

			angle = Mathf.Abs(MathUtility.snormDegree(angle - this.control.getDirection()));

			if(angle >= angle_limit) {

				continue;
			}

			//

			float	score = distance*MathUtility.remap(0.0f, angle_limit, angle, 1.0f, 2.0f);

			if(target == null) {

				target    = player;
				min_score = score;

			} else {

				if(score < min_score) {

					target    = player;
					min_score = score;
				}
			}
		}

		return(target);
	}

	// 相手が攻撃できる範囲にいる？.
	public bool		isInAttackRange(chrController target)
	{
		bool	ret = false;

		do {
	
			chrController	mine = this.control;

			Vector3		to_enemy = target.getPosition() - mine.getPosition();
	
			to_enemy.y = 0.0f;
	
			if(to_enemy.magnitude >= mine.vital.getAttackDistance()) {

				break;
			}
			to_enemy.Normalize();

			Vector3		heading = Quaternion.AngleAxis(mine.getDirection(), Vector3.up)*Vector3.forward;
	
			heading.y = 0.0f;
			heading.Normalize();
	
			float	dp = Vector3.Dot(to_enemy, heading);
	
			if(dp < Mathf.Cos(Mathf.PI/4.0f)) {
	
				break;
			}

			ret = true;

		} while(false);

		return(ret);
	}
}

