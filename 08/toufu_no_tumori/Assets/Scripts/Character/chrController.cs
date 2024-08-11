using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MathExtension;

public class chrController : MonoBehaviour {

	public static float		CARRIED_ITEM_HEIGHT = 2.0f;			// 謖√■驕九?荳ｭ縺ｮ繧｢繧､繝?Β縺ｮ鬮倥＆.

	// ================================================================ //

	public chrBehaviorBase		behavior = null;				// 繝薙?繧､繝薙い繝ｼ縲?繝槭え繧ｹ縺ｧ縺ｮ蛻ｶ蠕｡縲?PC 縺ｮ AI 縺ｪ縺ｩ.
	public ChattyBalloon		balloon = null;					// 縺ｵ縺阪□縺?
	public GameObject			model = null;					// 繝｢繝?Ν.

	public int		global_index = -1;							// 繧ｰ繝ｭ繝ｼ繝舌Ν縺ｧ繝ｦ繝九?繧ｯ縺ｪ繧｢繧ｫ繧ｦ繝ｳ繝医? id.
	public int		local_index  = -1;							// 縺薙? PC 蜀?〒縺ｮ繧､繝ｳ繝?ャ繧ｯ繧ｹ?医Ο繝ｼ繧ｫ繝ｫ繝励Ξ繧､繝､繝ｼ縺鯉ｼ撰ｼ?

	public ItemManager			item_man;						// 繧｢繧､繝?Β繝槭ロ繝ｼ繧ｸ繝｣繝ｼ.
	public AcountManager		account_man;					// 繧｢繧ｫ繧ｦ繝ｳ繝医?繝阪?繧ｸ繝｣繝ｼ
	public GameInput			game_input;						// 繝槭え繧ｹ縺ｪ縺ｩ縺ｮ蜈･蜉?

	public	string				account_name = "";				// 繧｢繧ｫ繧ｦ繝ｳ繝亥錐.
	public	AcountData			account_data = null;			// 繧｢繧ｫ繧ｦ繝ｳ繝域ュ蝣ｱ.

	public Vector3				prev_position;

	protected struct Motion {

		public string	name;
		public int		layer;
	};

	protected	Animation	anim_player = null;		// 繧｢繝九Γ繝ｼ繧ｷ繝ｧ繝ｳ.
	protected	Motion		current_motion = new Motion();
	protected	bool		is_player = true;

	public ItemCarrier		item_carrier = null;	// 謖√■驕九?荳ｭ縺ｮ繧｢繧､繝?Β.

	public	List<QueryBase>	queries = new List<QueryBase>();

	// ================================================================ //
	// MonoBehaviour 縺九ｉ縺ｮ邯呎価.

	void 	Awake()
	{
		this.balloon = BalloonRoot.get().createBalloon();
		
		this.item_man    = ItemManager.getInstance();
		
		this.item_carrier = new ItemCarrier(this);
	}

	void	Start()
	{
		if(this.is_player) {

			this.account_data = AcountManager.get().getAccountData(account_name);
		}

		// 繧｢繝九Γ繝ｼ繧ｷ繝ｧ繝ｳ繧ｳ繝ｳ繝昴?繝阪Φ繝医ｒ謗｢縺励※縺翫￥.
		this.anim_player = this.transform.GetComponentInChildren<Animation>();

		this.current_motion.name  = "";
		this.current_motion.layer = -1;

		this.behavior.start();
	}

	void	LateUpdate()
	{
		this.behavior.lateExecute();
	}

	void	Update()
	{
		// ---------------------------------------------------------------- //

		if(this.current_motion.name != "") {

			if(!this.anim_player.isPlaying) {
	
				this.current_motion.name  = "";
				this.current_motion.layer = -1;
			}
		}

		// 謖√■驕九?荳ｭ縺ｮ繧｢繧､繝?Β.
		//
		this.item_carrier.execute();

		// ---------------------------------------------------------------- //
		// 繝薙?繧､繝薙い繝ｼ縺ｮ螳溯｡?
		//
		// ?医?繧ｦ繧ｹ縺ｮ遘ｻ蜍包ｼ医Ο繝ｼ繧ｫ繝ｫ?峨?√ロ繝?ヨ縺九ｉ蜿嶺ｿ｡縺励◆繝??繧ｿ繝ｼ縺ｧ遘ｻ蜍包ｼ医ロ繝?ヨ?会ｼ?
		//

		this.behavior.execute();

		// ---------------------------------------------------------------- //
		// 騾溷ｺｦ繧偵け繝ｪ繧｢繝ｼ縺励※縺翫￥.

		if(!this.GetComponent<Rigidbody>().isKinematic) {

			this.GetComponent<Rigidbody>().velocity = this.GetComponent<Rigidbody>().velocity.XZ(0.0f, 0.0f);
			this.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
		}

		// ---------------------------------------------------------------- //
		// 縺ｵ縺阪□縺励?菴咲ｽｮ/縺?ｍ.

		if(this.balloon.getText() != "") {

			Vector3		on_screen_position = Camera.main.WorldToScreenPoint(this.transform.position + Vector3.up*3.0f);

			this.balloon.setPosition(new Vector2(on_screen_position.x, Screen.height - on_screen_position.y));

			if(this.account_data != null) {

				this.balloon.setColor(this.account_data.favorite_color);
			}
		}


		// ---------------------------------------------------------------- //

		// 螳溯｡後′邨ゅｏ縺｣縺溘け繧ｨ繝ｪ繝ｼ繧貞炎髯､縺吶ｋ.
		this.queries.RemoveAll(x => x.isExpired());
	}

	// ================================================================ //

	public Vector3		getPosition()
	{
		return(this.transform.position);
	}
	public float		getDirection()
	{
		return(this.transform.rotation.eulerAngles.y);
	}

	// 陬?ｙ繧｢繧､繝?Β繧偵ご繝?ヨ縺吶ｋ.
	public T	getCarriedItem<T>() where T : ItemBehaviorBase
	{
		T	item_behavior = null;

		do {

			ItemController	item = this.item_carrier.item;

			if(item == null) {

				break;
			}

			if(item.behavior.GetType() != typeof(T)) {

				break;
			}

			item_behavior = item.behavior as T;

		} while(false);

		return(item_behavior);
	}

	// 陬?ｙ繧｢繧､繝?Β縺ｮ迚ｹ蜈ｸ繧偵?縺医☆.
	public ItemFavor	getItemFavor()
	{
		ItemFavor	favor = null;

		do {

			ItemController	item = this.item_carrier.item;

			if(item == null) {

				break;
			}

			if(item.behavior.item_favor == null) {

				break;
			}

			favor = item.behavior.item_favor;

		} while(false);

		if(favor == null) {

			favor = new ItemFavor();
		}

		return(favor);
	}

	// 莉悶?繧ｭ繝｣繝ｩ繧ｯ繧ｿ繝ｼ縺ｫ繧ｿ繝?メ縺輔ｌ縺滂ｼ医◎縺ｰ縺ｧ繧ｯ繝ｪ繝?け?峨→縺阪↓蜻ｼ縺ｰ繧後ｋ.
	public void		touchedBy(chrController toucher)
	{
		this.behavior.touchedBy(toucher);
	}
	
	public void		setPlayer(bool is_player)
	{
		this.is_player = is_player;
	}

	// 陦ｨ遉ｺ/髱櫁｡ｨ遉ｺ繧偵そ繝?ヨ縺吶ｋ.
	public void		setVisible(bool is_visible)
	{
		Renderer[]	renderers = this.model.gameObject.GetComponentsInChildren<Renderer>();

		foreach(var renderer in renderers) {

			renderer.enabled = is_visible;
		}

		// 縺ｾ繧句ｽｱ繧?
		Projector[]	projectors = this.gameObject.GetComponentsInChildren<Projector>();

		foreach(var projector in projectors) {

			projector.enabled = is_visible;
		}
	}

	// ================================================================ //
	// 繝薙?繧､繝薙い繝ｼ縺ｮ菴ｿ縺?さ繝槭Φ繝?

	// 菴咲ｽｮ繧偵そ繝?ヨ縺吶ｋ.
	public void		cmdSetPosition(Vector3 position)
	{
		this.transform.position = position;
	}

	// 蜷代″繧偵そ繝?ヨ縺吶ｋ.
	public void		cmdSetDirection(float angle)
	{
		this.transform.rotation = Quaternion.AngleAxis(angle, Vector3.up);
	}

	// 繧ｿ繝ｼ繧ｲ繝?ヨ縺ｮ譁ｹ縺ｫ蟆代＠縺?縺大屓霆｢縺吶ｋ.
	// ?域ｯ弱ヵ繝ｬ繝ｼ繝?蜻ｼ縺ｶ縺ｨ縲√せ繝?繝ｼ繧ｺ縺ｫ繧ｿ繝ｼ繧ｲ繝?ヨ縺ｮ譁ｹ繧貞髄縺擾ｼ?
	public float		cmdSmoothHeadingTo(Vector3 target)
	{
		float	cur_dir = this.getDirection();

		do {

			Vector3		dir_vector = target - this.transform.position;

			dir_vector.y = 0.0f;

			dir_vector.Normalize();

			if(dir_vector.magnitude == 0.0f) {

				break;
			}

			float	tgt_dir = Quaternion.LookRotation(dir_vector).eulerAngles.y + 180.0f;

			cur_dir = Mathf.LerpAngle(cur_dir, tgt_dir, 0.1f);

			this.cmdSetDirection(cur_dir);

		} while(false);

		return(cur_dir);
	}

	// ================================================================ //
	// 繝薙?繧､繝薙い繝ｼ縺ｮ菴ｿ縺?さ繝槭Φ繝?
	// 繝｢繝ｼ繧ｷ繝ｧ繝ｳ邉ｻ.

	// 繝｢繝ｼ繧ｷ繝ｧ繝ｳ縺ｮ蜀咲函繧帝幕蟋九☆繧?
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

			this.anim_player[motion_name].speed = 1.0f;
			this.anim_player[motion_name].time  = 0.0f;
			this.anim_player.CrossFade(this.current_motion.name, 0.1f);

		} while(false);

	}

	// 繝｢繝ｼ繧ｷ繝ｧ繝ｳ繧帝???逕溘☆繧具ｼ域怙邨ゅヵ繝ｬ繝ｼ繝?縺九ｉ騾?髄縺阪↓??
	public void		cmdSetMotionRewind(string motion_name, int layer)
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

			this.anim_player[motion_name].speed = -1.0f;
			this.anim_player[motion_name].time  = this.anim_player[motion_name].length - 0.1f;
			this.anim_player.Play(this.current_motion.name);

		} while(false);

	}

	// 繧ｫ繝ｬ繝ｳ繝医Δ繝ｼ繧ｷ繝ｧ繝ｳ繧偵ご繝?ヨ縺吶ｋ.
	public string	cmdGetMotion()
	{
		return(this.current_motion.name);
	}

	// 繝｢繝ｼ繧ｷ繝ｧ繝ｳ蜀咲函荳ｭ??
	public bool		isMotionPlaying()
	{
		return(this.anim_player.isPlaying);
	}

	// ================================================================ //
	// 繝薙?繧､繝薙い繝ｼ縺ｮ菴ｿ縺?さ繝槭Φ繝?
	// 蜷ｹ縺榊?縺礼ｳｻ.

	// 蜷ｹ縺榊?縺励ｒ陦ｨ遉ｺ縺吶ｋ.
	public void		cmdDispBalloon(string text)
	{
		this.balloon.setText(text);
	}

	// 螳壼梛譁?ｒ菴ｿ縺｣縺ｦ蜷ｹ縺榊?縺励ｒ陦ｨ遉ｺ縺吶ｋ.
	public void		cmdDispBalloon(int text_id)
	{
		this.balloon.setText(this.behavior.getPresetText(text_id));
	}


	// ================================================================ //
	// 繝薙?繧､繝薙い繝ｼ縺ｮ菴ｿ縺?さ繝槭Φ繝?
	// 繧｢繧､繝?Β邉ｻ.

	// 繧｢繧､繝?Β繧剃ｽ懊ｋ.
	public string		cmdItemCreate(string type)
	{
		return(this.item_man.createItem(type, this.account_name));
	}
	// 繧｢繧､繝?Β縺ｮ菴咲ｽｮ繧偵そ繝?ヨ縺吶ｋ.
	public void		cmdItemSetPosition(string item_id, Vector3 position)
	{
		this.item_man.setPositionToItem(item_id, position);
	}

	// 繧｢繧､繝?Β繧呈鏡縺?
	public bool		cmdItemPick(QueryItemPick query, string owner_id, string item_id)
	{
		bool	ret = false;
	
		do {

			if(this.item_carrier.isCarrying()) {

				break;
			}

			ItemController	item = this.item_man.pickItem(query, owner_id, item_id);

			if(item == null) {

				break;
			}

			if(query.is_anon) {

				this.item_carrier.beginCarryAnon(item);

			} else {

				this.item_carrier.beginCarry(item);
			}

			ret = true;

		} while(false);

		return(ret);
	}
	// 繧｢繧､繝?Β繧呈昏縺ｦ繧?
	public bool		cmdItemDrop(string owner_id)
	{
		bool	ret = false;
	
		do {

			ItemController	item = this.item_carrier.item;

			if(item == null) {

				break;
			}

			// 繧｢繧､繝?Β繝槭ロ繝ｼ繧ｸ繝｣縺ｫ謐ｨ縺ｦ縺溘％縺ｨ繧帝?夂衍縺吶ｋ.
			if(owner_id == this.account_name) {

				this.item_man.dropItem(owner_id, item);
			}

			item.gameObject.GetComponent<Rigidbody>().isKinematic   = true;
			item.gameObject.transform.localPosition += Vector3.left*1.0f;
			item.gameObject.transform.parent        = this.item_man.transform;

			// 繝ｪ繧ｹ繝昴?繝ｳ縺吶ｋ.
			//
			// 迴ｾ迥ｶ縺ｮ莉墓ｧ倥〒縺ｯ縲∵昏縺ｦ繧峨ｌ縺溘い繧､繝?Β縺ｯ譛?蛻昴?菴咲ｽｮ縺ｫ繝ｪ繧ｹ繝昴?繝ｳ縺吶ｋ.
			// ?医?弱◎縺ｮ蝣ｴ縺ｫ繝峨Ο繝??縲上?縺励↑縺?ｼ?			//
			item.startRespawn();

			this.item_carrier.endCarry();

			ret = true;

		} while(false);

		return(ret);

	}

	// 蝠上＞蜷医ｏ縺帙??繧｢繧､繝?Β繧呈鏡縺医ｋ??
	public QueryItemPick	cmdItemQueryPick(string item_id, bool local = true, bool force = false)
	{
		QueryItemPick	query = null;
	
		do {

			query = this.item_man.queryPickItem(this.account_name, item_id, local, force);

			if(query == null) {

				break;
			}

			this.queries.Add(query);

		} while(false);

		return(query);
	}

	// 蝠上＞蜷医ｏ縺帙??繧｢繧､繝?Β繧呈昏縺ｦ縺ｦ繧ゅ＞縺?ｼ?
	public QueryItemDrop	cmdItemQueryDrop(bool local = true)
	{
		QueryItemDrop	query = null;
	
		do {

			if(!this.item_carrier.isCarrying()) {

				break;
			}

			query = this.item_man.queryDropItem(this.account_name, this.item_carrier.item, local);

			if(query == null) {

				break;
			}

			this.queries.Add(query);

		} while(false);

		return(query);
	}

	// 蝠上＞蜷医ｏ縺帙??縺翫＠繧?∋繧奇ｼ医?縺阪□縺暦ｼ?
	public QueryTalk	cmdQueryTalk(string words, bool local = false)
	{
		QueryTalk	query = null;
	
		do {

			query = CharacterRoot.get().queryTalk(words, local);

			if(query == null) {

				break;
			}

			this.queries.Add(query);

		} while(false);

		return(query);
	}

	// 蝠上＞蜷医ｏ縺帙??縺ｲ縺｣縺薙＠蟋九ａ縺ｦ繧ゅ＞縺?ｼ?
	public QueryHouseMoveStart	cmdQueryHouseMoveStart(string house_name, bool local = true)
	{
		QueryHouseMoveStart	query = null;
	
		do {

			query = CharacterRoot.get().queryHouseMoveStart(house_name, local);

			if(query == null) {

				break;
			}

			this.queries.Add(query);

		} while(false);

		return(query);
	}

	// 蝠上＞蜷医ｏ縺帙??縺ｲ縺｣縺薙＠邨ゅｏ縺｣縺ｦ繧ゅ＞縺?ｼ?
	public QueryHouseMoveEnd	cmdQueryHouseMoveEnd(bool local = true)
	{
		QueryHouseMoveEnd	query = null;
	
		do {

			query = CharacterRoot.get().queryHouseMoveEnd(local);

			if(query == null) {

				break;
			}

			this.queries.Add(query);

		} while(false);

		return(query);
	}

}
