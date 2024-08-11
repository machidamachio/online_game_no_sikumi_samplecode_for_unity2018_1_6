using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MathExtension;

//! 体力とか、攻撃力とか.
public class Vital {

	public Vital()
	{
		this.hit_point  = 10.0f;
		this.shot_power   = 1.0f;
		this.attack_power = 10.0f;
		this.attack_distance = 2.0f;
	}

	// 体力まんたんにする.
	public void		healFull()
	{
		this.hit_point = 100.0f;
		SoundManager.getInstance().playSE(Sound.ID.DDG_SE_SYS04);
	}

	// 体力まんたんにする　演出無し.
	public void		healFullInternal()
	{
		this.hit_point = 100.0f;
	}

	// ダメージをあたえる.
	public void		causeDamage(float damage)
	{
		this.hit_point -= damage;
		this.hit_point = Mathf.Max(0.0f, this.hit_point);
	}

	// 飛び道具の攻撃力をゲットする.
	public float	getShotPower()
	{
		return(this.shot_power);
	}

	// 近接攻撃の攻撃力をゲットする.
	public float	getAttackPower()
	{
		return(this.attack_power);
	}
	// 近接攻撃の攻撃力をセットする.
	public void		setAttackPower(float power)
	{
		this.attack_power = power;
	}

	// 近接攻撃のとどく距離をゲットする.
	public float	getAttackDistance()
	{
		return(this.attack_distance);
	}
	// 近接攻撃のとどく距離をセットする.
	public void		setAttackDistance(float distance)
	{
		this.attack_distance = distance;
	}

	// ヒットポイントをセットする.
	public void		setHitPoint(float hp)
	{
		this.hit_point = hp;
	}

	// ヒットポイントをゲットする.
	public float	getHitPoint()
	{
		return(this.hit_point);
	}

	public float		hit_point;				// 体力.
	public float		shot_power;				// 飛び道具の攻撃力.
	protected float		attack_power;			// 近接攻撃の攻撃力.

	protected float		attack_distance;		// 近接攻撃のとどく距離.
};


public class chrController : MonoBehaviour {

	public int		global_index = -1;							// グローバルでユニークなアカウントの id.
	public int		local_index  = -1;							// この PC 内でのインデックス（ローカルプレイヤーが０）.

	protected Vector3	previous_position;						// 目のフレームの位置.
	protected Vector3	move_vector = Vector3.zero;				// [m/dt] 前のフレームからの位置の移動ベクトル.


	public chrBehaviorBase		behavior = null;				/// ビヘイビアー　マウスでの制御、NPC の AI など.

	public chrBalloon			balloon = null;					// ふきだし.

	public Vital				vital = new Vital();			// 体力とか、攻撃力とか.

	public bool					is_accept_damage = true;		// ダメージを受ける？.

	public float				damage_after_timer = 0.0f;
	public bool					trigger_damage = false;

	public DamageEffect			damage_effect = null;

	public List<CollisionResult>	collision_results = new List<CollisionResult>();

	protected struct Motion {

		public string	name;
		public int		layer;
		public float	previous_time;				// [sec] 前のフレームの再生時刻.

	};

	protected	Animation	anim_player = null;				// アニメーション.
	protected	Motion		current_motion = new Motion();

	public Animation	getAnimationPlayer()
	{
		return(this.anim_player);
	}

	// ================================================================ //
	// このクラスを継承するクラスのためのインターフェイス.

	// デフォルトプロパティの変更に.
	protected virtual void _awake()
	{
	}

	// 最初の１フレームのアップデート前に.
	protected virtual void _start()
	{
	}
	
	// 毎描画フレームよばれる.
	protected virtual void	execute()
	{
	}

	// 固定アップデート.
	protected virtual void fixedExecute()
	{
	}

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
		// 吹き出し.
		this.balloon = this.gameObject.AddComponent<chrBalloon>();

		// ダメージくらったときのエフェクト（仮）.
		this.damage_effect = new DamageEffect(this.gameObject);

		// アニメーションコンポーネントを探しておく.
		this.anim_player = this.transform.GetComponentInChildren<Animation>();
		if (this.anim_player != null) {
			this.anim_player.cullingType = AnimationCullingType.AlwaysAnimate;
		}

		this.current_motion.name  = "";
		this.current_motion.layer = -1;

		this._start();

		this.behavior.start();

		this.previous_position = this.transform.position;
	}
	
	void FixedUpdate()
	{
		// ---------------------------------------------------------------- //
		// 速度をクリアーしておく.

		if(!this.GetComponent<Rigidbody>().isKinematic) {

			this.GetComponent<Rigidbody>().velocity = this.GetComponent<Rigidbody>().velocity.XZ(0.0f, 0.0f);
			this.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
		}

		this.fixedExecute();

		this.collision_results.Clear();
	}

	void 	Awake()
	{
		_awake();
	}

	void	Update()
	{
		this.damage_after_timer = Mathf.Max(0.0f, this.damage_after_timer - Time.deltaTime);

		if(this.trigger_damage) {
	
			this.damage_effect.startDamage();

			this.damage_after_timer = 1.0f;
		}

		this.move_vector       = this.getPosition() - this.previous_position;
		this.previous_position = this.getPosition();

		this.trigger_damage = false;

		// ---------------------------------------------------------------- //

		if(this.current_motion.name != "" && this.anim_player != null) {

			if(!this.anim_player.IsPlaying(this.current_motion.name)) {
	
				this.current_motion.name  = "";
				this.current_motion.layer = -1;
			}
		}

		// ---------------------------------------------------------------- //
		// 継承クラスのコントローラの実行.
		//
		this.execute();

		// ---------------------------------------------------------------- //
		// ビヘイビアーの実行.
		//
		// （マウスの移動（ローカル）、ネットから受信したデーターで移動（ネット））.
		//

		this.behavior.execute();

		this.damage_effect.execute();

		// ---------------------------------------------------------------- //
		// ふきだしの位置/いろ.

		if(this.balloon.text != "") {

			Camera	camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();

			Vector3		on_screen_position = camera.WorldToScreenPoint(this.transform.position + Vector3.up*0.0f);

			this.balloon.position = new Vector2(on_screen_position.x, Screen.height - on_screen_position.y);


			AccountData account_data = AccountManager.get().getAccountData(this.getAccountID());
			this.balloon.setColor(account_data.favorite_color);
		}

		// ---------------------------------------------------------------- //

		this.current_motion.previous_time = this.getMotionCurrentTime();
	}

	// ================================================================ //

	// 表示/非表示をセットする.
	public void			setVisible(bool is_visible)
	{
		Transform		model = this.transform.Find("model");

		if(model != null) {

			model.gameObject.SetActive(is_visible);

		} else {

			for(int i = 0;i < this.transform.childCount;i++) {

				this.transform.GetChild(i).gameObject.SetActive(is_visible);
			}
		}
	}

	// １フレーム前の位置をゲットする.
	public Vector3		getPreviousPosition()
	{
		return(this.previous_position);
	}

	// 位置をゲットする.
	public Vector3		getPosition()
	{
		return(this.transform.position);
	}

	// [degree] キャラの向きをゲットする.
	public float		getDirection()
	{
		return(this.transform.rotation.eulerAngles.y);
	}

	// [m/dt] 前のフレームからの位置の移動ベクトルをゲットする.
	public Vector3	getMoveVector()
	{
		return(this.move_vector);
	}

	// アカウント名をゲットする.
	public string	getAccountID()
	{
		return(AccountManager.get().getAccountData(this.global_index).account_id);
	}

	// コリジョン半径(XZ).
	public float	getCollisionRadius()
	{
		float	radius = -1.0f;

		do {

			Collider	coli = this.gameObject.transform.GetComponent<Collider>();

			if(coli == null) {

				break;
			}

			Vector3		scale = this.gameObject.transform.localScale;

			radius = new Vector2(coli.bounds.size.x*scale.x, coli.bounds.size.z*scale.z).magnitude/2.0f;

		} while(false);

		return(radius);
	}

	// ================================================================ //
	// ビヘイビアーの使うコマンド.

	// 位置をセットする.
	public void		cmdSetPosition(Vector3 position)
	{
		if(this.GetComponent<Rigidbody>() != null && !this.GetComponent<Rigidbody>().IsSleeping()) {

			this.GetComponent<Rigidbody>().MovePosition(position);

		} else {

			this.transform.position = position;
		}
	}

	// 位置をすぐにセットする.
	public void		cmdSetPositionAnon(Vector3 position)
	{
		this.transform.position = position;
	}

	// 向きをセットする.
	public void		cmdSetDirection(float angle)
	{
		if(this.GetComponent<Rigidbody>() != null && !this.GetComponent<Rigidbody>().IsSleeping()) {

			this.GetComponent<Rigidbody>().MoveRotation(Quaternion.AngleAxis(angle, Vector3.up));

		} else {

			this.transform.rotation = Quaternion.AngleAxis(angle, Vector3.up);
		}
	}

	// 向きをすぐにセットする.
	public void		cmdSetDirectionAnon(float angle)
	{
		this.transform.rotation = Quaternion.AngleAxis(angle, Vector3.up);
	}

	// ターゲットの方に少しだけ回転する.
	// （毎フレーム呼ぶと、スムーズにターゲットの方を向く）.
	public float		cmdSmoothHeadingTo(Vector3 target, float turn_rate = 0.1f)
	{
		float	cur_dir = this.getDirection();

		do {

			Vector3		dir_vector = target - this.transform.position;

			dir_vector.y = 0.0f;

			dir_vector.Normalize();

			if(dir_vector.magnitude == 0.0f) {

				break;
			}

			float	tgt_dir = Quaternion.LookRotation(dir_vector).eulerAngles.y;

			cur_dir = Mathf.LerpAngle(cur_dir, tgt_dir, turn_rate);

			this.cmdSetDirection(cur_dir);

		} while(false);

		return(cur_dir);
	}
	// 指定の方向に少しだけ回転する.
	// （毎フレーム呼ぶと、スムーズにターゲットの方を向く）.
	public float		cmdSmoothDirection(float target_dir, float turn_rate = 0.1f)
	{
		float	cur_dir = this.getDirection();

		float	dir_diff = target_dir - cur_dir;

		if(dir_diff > 180.0f) {

			dir_diff = dir_diff - 360.0f;

		} else if(dir_diff < -180.0f) {

			dir_diff = dir_diff + 360.0f;
		}

		if(Mathf.Abs(dir_diff*(1.0f - turn_rate)) < 1.0f) {

			cur_dir = target_dir;

		} else {

			dir_diff *= turn_rate;
			cur_dir += dir_diff;
		}

		this.cmdSetDirection(cur_dir);

		return(cur_dir);
	}

	// ================================================================ //
	// ビヘイビアーの使うコマンド.
	// モーション系.

	// モーションをセットする.
	public void		cmdSetMotion(string motion_name, int layer)
	{
		do {

			if(this.anim_player == null) {

				break;
			}
			if(this.current_motion.layer > layer) {

				break;
			}
			if(motion_name == this.current_motion.name) {

				break;
			}

			this.current_motion.name  = motion_name;
			this.current_motion.layer = layer;
			this.current_motion.previous_time = -Time.deltaTime;

			this.anim_player.Play(this.current_motion.name);

		} while(false);

	}

	// 再生中のモーションを取得する.
	public string	getMotion()
	{
		return(this.current_motion.name);
	}

	// [sec] モーションの現在の再生時刻をゲットする.
	public float	getMotionCurrentTime()
	{
		float	time = 0.0f;

		do {

			if(this.current_motion.name == "") {

				break;
			}

			if(this.anim_player[this.current_motion.name] == null) {

				break;
			}

			time = this.anim_player[this.current_motion.name].time;

		} while(false);

		return(time);
	}

	// [sec] 前のフレームのモーションの再生時刻をゲットする.
	public float	getMotionPreviousTime()
	{
		return(this.current_motion.previous_time);
	}

	// モーションが指定の再生時刻をまたいだ瞬間？.
	public bool		isMotionAcrossingTime(float time)
	{
		return(this.getMotionPreviousTime() < time && time <= this.getMotionCurrentTime());
	}

	// モーションの再生情報をリセットする.
	public void		resetMotion()
	{
		this.current_motion.name  = "";
		this.current_motion.layer = -1;
	}

	// ================================================================ //
	// ビヘイビアーの使うコマンド.
	// アイテム系.

	// アイテムを作る.
	public void		cmdItemCreate(string type)
	{
		ItemManager.getInstance().createItem(type, type, AccountManager.get().getAccountData(this.global_index).avator_id);
	}

	// アイテムの位置をセットする.
	public void		cmdItemSetPosition(string item_id, Vector3 position)
	{
		ItemManager.getInstance().setPositionToItem(item_id, position);
	}

	// アイテムを拾う.
	public ItemController		cmdItemPick(QueryItemPick query, string item_id)
	{
		ItemController	item = null;

		do {

			string account_id = PartyControl.get().getLocalPlayer().getAcountID();
			item = ItemManager.getInstance().pickItem(query, account_id, item_id);

			if(item == null) {

				break;
			}

			item.gameObject.transform.parent        = this.gameObject.transform;
			item.gameObject.transform.localPosition = Vector3.up*300.0f;
			item.gameObject.GetComponent<Rigidbody>().isKinematic   = true;

			Debug.Log("cmdItemPick:" + item_id);
			dbwin.console().print("cmdItemPick:" + item_id);

		} while(false);

		return(item);
	}

	// アイテムを捨てる.
	public void		cmdItemDrop(string item_id, bool is_local = true)
	{
		ItemManager.getInstance().cmdDropItem(this.getAccountID(), item_id, is_local);
	}

	// 自分に対してアイテムを使う.
	public void		cmdUseItemSelf(int slot, Item.Favor item_favor, bool is_local)
	{
		// こちらは ItemWindow などから『アイテムを使いたい』ときに
		// 呼び出すメソッドです.

		string	account_id = AccountManager.get().getAccountData(this.global_index).account_id;

		ItemManager.getInstance().useItem(slot, item_favor, account_id, account_id, is_local);
	}

	// 仲間に対してアイテムを使う.
	public void		cmdUseItemToFriend(Item.Favor item_favor, int friend_global_index, bool is_local)
	{
		// こちらは ItemWindow などから『アイテムを使いたい』ときに
		// 呼び出すメソッドです.

		string	account_id        = AccountManager.get().getAccountData(this.global_index).account_id;
		string	friend_account_id = AccountManager.get().getAccountData(friend_global_index).account_id;

		ItemManager.getInstance().useItem(-1, item_favor, account_id, friend_account_id, is_local);
	}

	// 問い合わせ　アイテムを拾える？.
	public QueryItemPick	cmdItemQueryPick(string item_id, bool is_local, bool force_pickup)
	{
		QueryItemPick	query = null;

		do {


			query = ItemManager.get().queryPickItem(this.getAccountID(), item_id, is_local, force_pickup);

			if(query == null) {

				break;
			}

		} while(false);

		return(query);
	}

	// ================================================================ //
	// ビヘイビアーの使うコマンド.
	// 吹き出し系.
	
	// 吹き出しを表示する.
	public void		cmdDispBalloon(string text)
	{
		this.balloon.setText(text, 8.0f);
	}
	
	// 定型文を使って吹き出しを表示する.
	public void		cmdDispBalloon(int text_id)
	{
		this.balloon.text = this.behavior.getPresetText(text_id);
	}
	
	// 問い合わせ　おしゃべり（ふきだし）.
	public QueryTalk	cmdQueryTalk(string words, bool local = false)
	{
		QueryTalk	query = CharacterRoot.get().queryTalk(this.getAccountID(), words, local);

		return(query);
	}

	// ================================================================ //


	// 退場する.
	public void		cmdBeginVanish()
	{
		// アニメーションを止める.
		Animation[] animations = this.gameObject.GetComponentsInChildren<Animation>();
		
		foreach(var animation in animations) {
			
			animation.Stop();
		}
		
		this.damage_effect.startVanish();

		this.GetComponent<Rigidbody>().Sleep();
		this.GetComponent<Collider>().enabled = false;
	}

	// コリジョンを ON/OFF する.
	public void		cmdEnableCollision(bool is_enable)
	{
		if(this.GetComponent<Rigidbody>() != null) {

			if(is_enable) {
	
				this.GetComponent<Rigidbody>().WakeUp();
	
			} else {
	
				this.GetComponent<Rigidbody>().Sleep();
			}
		}

		if(this.GetComponent<Collider>() != null) {

			this.GetComponent<Collider>().enabled = is_enable;
		}
	}

	// ダメージを受ける/受けないをセットする.
	public void		cmdSetAcceptDamage(bool is_accept)
	{
		this.is_accept_damage = is_accept;
	}

	// ================================================================ //

	// ダメージ.
	// attacker_gidx ...	ダメージを与えたプレイヤーの global_index
	//						エネミーがプレイヤーにダメージを与えたときは -1
	public void		causeDamage(float damage, int attacker_gidx, bool is_local = true)
	{
		if(!this.is_accept_damage && is_local) {

			return;
		}

		//string log = "[CLIENT][" + ((is_local)? "Local" : "Remote") + "]causeDamage called:" + damage + "[" + attacker_gidx + "]";
		//Debug.Log(log);

		if (!is_local && attacker_gidx == GlobalParam.get().global_account_id) {
			// 自分が攻撃した情報の時はすでに反映済みなので何もしない.
			return;
		}

		this.trigger_damage = true;

		this.vital.causeDamage(damage);

		this.behavior.onDamaged();

		if (is_local) {
			//Debug.Log("[CLIENT][Local]Send damage data:" + this.name + "[" + damage + "]");

			if (this.name.StartsWith("Player")) {	
				CharacterRoot.get().NotifyHitPoint(this.behavior.name, this.vital.hit_point);
			}
			else {
				CharacterRoot.get().NotifyDamage(this.behavior.name, GlobalParam.get().global_account_id, damage);
			}
		}
	}

	// HP をセットする.
	public void		setHitPoint(float hp)
	{
		this.vital.setHitPoint(hp);
		
		this.behavior.onDamaged();
	}

	// アイテムを使う（自分で自分に）.
	public void		onUseItemSelf(int slot_index, Item.Favor favor)
	{
		// こちらは実際にアイテムを使用するときに、アイテムマネージャーから
		// 呼び出されるメソッドです.

		var	player = this.behavior as chrBehaviorPlayer;
		
		switch(favor.category) {

			case Item.CATEGORY.SODA_ICE:
			{
				if(player != null) {

					player.onUseItem(slot_index, favor);
				}
			}
			break;
		}
	}
	// アイテムを使ってもらった（仲間が自分に）.
	//
	// in:	friend	アイテムを使った仲間.
	//
	public void		onUseItemByFriend(Item.Favor favor, chrController friend)
	{
		// こちらは実際にアイテムを使用するときに、アイテムマネージャーから
		// 呼び出されるメソッドです.

		var	player = this.behavior as chrBehaviorPlayer;

		switch(favor.category) {

			case Item.CATEGORY.SODA_ICE:
			{
				//this.vital.healFull();

				if(player != null) {

					player.onUseItemByFriend(favor, friend.behavior as chrBehaviorPlayer);
				}
			}
			break;
		}
	}

	// ================================================================ //
	
	public void	consumeKey(ItemController item)
	{
		Debug.Log ("consumeKey");

		if(item.type == "key04") {

			// フロアー移動キー.
			Debug.Log("UNLOCK FLOOR DOOR!!");
			MapCreator.get().UnlockBossDoor();

		} else {

			RoomController	room = MapCreator.get().getRoomFromPosition(transform.position);

			room.OnConsumedKey(item.type);
		}
	}
}
