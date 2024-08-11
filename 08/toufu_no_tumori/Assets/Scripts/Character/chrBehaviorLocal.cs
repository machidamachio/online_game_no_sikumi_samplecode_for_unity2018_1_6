using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GameObjectExtension;
using MathExtension;

// 繝薙?繧､繝薙い繝ｼ縲?繝ｭ繝ｼ繧ｫ繝ｫ繝励Ξ繧､繝､繝ｼ逕ｨ.
// 繝槭え繧ｹ縺ｧ繧ｳ繝ｳ繝医Ο繝ｼ繝ｫ縺吶ｋ.
public class chrBehaviorLocal : chrBehaviorPlayer {

	public static float	MOVE_SPEED = 3.0f;

	private Vector3		move_target;			// 遘ｻ蜍募?縺ｮ菴咲ｽｮ.
	//private string		serif = "";				// 繧ｻ繝ｪ繝包ｼ亥聖縺榊?縺励?荳ｭ縺ｫ陦ｨ遉ｺ縺吶ｋ繝?く繧ｹ繝茨ｼ?

	protected string	move_target_item = "";	// 繧｢繧､繝?Β繧堤岼謖?＠縺ｦ遘ｻ蜍輔＠縺ｦ縺?ｋ縺ｨ縺?

	protected string	collision = "";

	// 髢灘ｼ輔＞縺溷ｺｧ讓吶ｒ菫晏ｭ?
	private List<CharacterCoord>	m_culling = new List<CharacterCoord>();
	// 迴ｾ蝨ｨ縺ｮ繝励Ο繝?ヨ縺ｮ繧､繝ｳ繝?ャ繧ｯ繧ｹ.
	private int 		m_plotIndex = 0;
	// 蛛懈ｭ｢迥ｶ諷九?縺ｨ縺阪?繝??繧ｿ繧帝?∽ｿ｡縺励↑縺?ｈ縺?↓縺吶ｋ.
	private Vector3		m_prev;


	// 3谺｡繧ｹ繝励Λ繧､繝ｳ陬憺俣縺ｧ菴ｿ逕ｨ縺吶ｋ轤ｹ謨ｰ.
	private const int	PLOT_NUM = 4;

	// ---------------------------------------------------------------- //

	public enum STEP {

		NONE = -1,

		MOVE = 0,			// 遘ｻ蜍包ｼ域ｭ｢縺ｾ縺｣縺ｦ繧区凾繧ょ性繧???
		HOUSE_MOVE,			// 縺雁ｼ戊ｶ翫＠.
		OUTER_CONTROL,		// 螟夜Κ蛻ｶ蠕｡.

		WAIT_QUERY,			// 繧ｯ繧ｨ繝ｪ繝ｼ蠕?■.

		NUM,
	};
	Step<STEP>		step = new Step<STEP>(STEP.NONE);

	protected bool				is_within_house_move_event_box = false;		// 縺ｲ縺｣縺薙＠髢句ｧ九う繝吶Φ繝医?繝?け繧ｹ縺ｫ蜈･縺｣縺ｦ繧具ｼ?

	// ================================================================ //
	// MonoBehaviour 縺九ｉ縺ｮ邯呎価.

	void	Start()
	{
	}
	
	void	Update()
	{
	}

	// 繧ｳ繝ｪ繧ｸ繝ｧ繝ｳ縺ｫ繝偵ャ繝医＠縺ｦ縺?ｋ髢謎ｸｭ繧医?繧後ｋ繝｡繧ｽ繝?ラ.
	void 	OnCollisionStay(Collision other)
	{
		if(other.gameObject.tag == "Item" || other.gameObject.tag == "Charactor") {

			this.collision = other.gameObject.name;
		}
	}

	// ================================================================ //

	public override void	initialize()
	{
		this.move_target = this.transform.position;
	}
	public override void	start()
	{
		this.controll.balloon.setPriority(-1);

		// 繧ｲ繝ｼ繝?髢句ｧ狗峩蠕後↓ EnterEvent 縺悟ｧ九∪繧九→縲√％縺薙〒 next_step 縺ｫ
		// OuterControll 縺後そ繝?ヨ縺輔ｌ縺ｦ縺?ｋ縲ゅ◎縺ｮ縺ｨ縺阪↓荳頑嶌縺阪＠縺ｪ縺?ｈ縺?↓縲?		// next == NONE 縺ｮ繝√ぉ繝?け繧貞?繧後ｋ.
		if(this.step.get_next() == STEP.NONE) {

			this.step.set_next(STEP.MOVE);
		}
	}
	public override	void	execute()
	{
		// 繧｢繧､繝?Β繧?く繝｣繝ｩ繧ｯ繧ｿ繝ｼ繧偵け繝ｪ繝?け縺励◆縺ｨ縺?		//
		// 繧ｯ繝ｪ繝?け縺励◆繧｢繧､繝?Β繧?く繝｣繝ｩ繧ｯ繧ｿ繝ｼ縺ｮ繧ｳ繝ｪ繧ｸ繝ｧ繝ｳ縺ｫ縺ｶ縺､縺九▲縺溘ｉ
		// 豁｢縺ｾ繧?
		//
		if(this.move_target_item != "") {

			if(this.move_target_item == this.collision) {

				this.move_target = this.controll.getPosition();
			}
		}

		// ---------------------------------------------------------------- //
		// 隱ｿ蛛懊?邨ゅｏ縺｣縺溘け繧ｨ繝ｪ繝ｼ縺ｮ螳溯｡?

		base.execute_queries();

		// ---------------------------------------------------------------- //
		// 谺｡縺ｮ迥ｶ諷九↓遘ｻ繧九?縺ｩ縺??繧偵?√メ繧ｧ繝?け縺吶ｋ.

		switch(this.step.do_transition()) {

			case STEP.MOVE:
			{
			}
			break;

			case STEP.WAIT_QUERY:
			{
				if(this.controll.queries.Count <= 0) {

					this.step.set_next(STEP.MOVE);
				}
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 迥ｶ諷九′驕ｷ遘ｻ縺励◆縺ｨ縺阪?蛻晄悄蛹?

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {
	
				case STEP.OUTER_CONTROL:
				{
					this.GetComponent<Rigidbody>().Sleep();
				}
				break;

				case STEP.MOVE:
				{
					this.move_target = this.transform.position;
				}
				break;

				case STEP.HOUSE_MOVE:
				{
					this.initialize_step_house_move();
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 蜷?憾諷九〒縺ｮ螳溯｡悟?逅?

		switch(this.step.do_execution(Time.deltaTime)) {

			case STEP.MOVE:
			{
				this.exec_step_move();
			}
			break;

			case STEP.HOUSE_MOVE:
			{
				this.execute_step_house_move();
			}
			break;
		}


		this.collision = "";

		// ---------------------------------------------------------------- //

		GameInput	gi = this.controll.game_input;

		if(gi.serif_text.trigger_on) {

			// 隱槫ｰｾ?郁｣?ｙ繧｢繧､繝?Β縺ｮ迚ｹ蜈ｸ??

			ItemFavor	item_favor = this.controll.getItemFavor();

			gi.serif_text.text += item_favor.term_word;

			this.controll.cmdQueryTalk(gi.serif_text.text, true);
		}

		// ---------------------------------------------------------------- //
		// ?托ｼ舌ヵ繝ｬ繝ｼ繝?縺ｫ?大屓縲∝ｺｧ讓吶ｒ繝阪ャ繝医↓騾√ｋ?医?繝?せ繝茨ｼ?

		{
			do {
	
				if(GameRoot.get().net_player == null) {
	
					break;
				}

				if(this.step.get_current() == STEP.OUTER_CONTROL) {
					break;
				}
	
				m_send_count = (m_send_count + 1)%SplineData.SEND_INTERVAL;
	
				if(m_send_count != 0) {
	
					break;
				}

				// 騾壻ｿ｡逕ｨ蠎ｧ讓咎?∽ｿ｡.
				Vector3 target = this.controll.getPosition() + Vector3.left;
				CharacterCoord coord = new CharacterCoord(target.x, target.z);

				Vector3 diff = m_prev - target;
				if (diff.sqrMagnitude > 0.0001f) {

					m_culling.Add(coord);

					//Debug.Log("SendCharacterCoord[index:" + m_plotIndex + "]");
					CharacterRoot.get().SendCharacterCoord(controll.account_name, m_plotIndex, m_culling); 
					++m_plotIndex;

					if (m_culling.Count >= PLOT_NUM) {

						m_culling.RemoveAt(0);
					}

					m_prev = target;
				}
	
			} while(false);
		}
	}

	protected int	m_send_count = 0;

	// ================================================================ //

	// STEP.MOVE 縺ｮ螳溯｡?
	// 遘ｻ蜍?
	protected void	exec_step_move()
	{
		// ---------------------------------------------------------------- //
		// 遘ｻ蜍包ｼ井ｽ咲ｽｮ蠎ｧ讓吶?陬憺俣??

		Vector3		position  = this.controll.getPosition();

		Vector3		dist = (this.move_target - position).Y(0.0f);

		float		speed = MOVE_SPEED;
		float		speed_per_frame = speed*Time.deltaTime;

		if(dist.magnitude < speed_per_frame) {

			// 逶ｮ讓吩ｽ咲ｽｮ縺後☆縺斐￥霑代＞縺ｨ縺阪?縲∫樟蝨ｨ菴咲ｽｮ?晉岼讓吩ｽ咲ｽｮ縺ｫ縺吶ｋ.
			position = this.move_target;

			// 遶九■豁｢縺ｾ繧翫Δ繝ｼ繧ｷ繝ｧ繝ｳ繧貞?逕溘☆繧?
			this.stopWalkMotion();

		} else {

			// 逶ｮ讓吩ｽ咲ｽｮ縺碁□縺?凾縺ｯ縲∫ｧｻ蜍暮?溷ｺｦ縺ｮ縺ｶ繧薙□縺醍ｧｻ蜍輔☆繧?

			dist *= (speed_per_frame)/dist.magnitude;

			position += dist;

			// 豁ｩ縺阪Δ繝ｼ繧ｷ繝ｧ繝ｳ繧貞?逕溘☆繧?
			this.playWalkMotion();
		}

		position.y = this.controll.getPosition().y;

		this.controll.cmdSetPosition(position);

		if(this.step.get_current() == STEP.HOUSE_MOVE) {

		} else {

			// 蜷代″縺ｮ陬憺俣.
			this.controll.cmdSmoothHeadingTo(this.move_target);
		}

		// ---------------------------------------------------------------- //

		GameInput	gi = this.controll.game_input;

		//繝峨Λ繝?げ縺励※縺?ｋ髢薙?縲∫ｧｻ蜍慕岼讓吩ｽ咲ｽｮ繧呈峩譁ｰ縺吶ｋ.
		if(gi.pointing.current) {

			if(gi.pointing.pointee != GameInput.POINTEE.NONE) {
	
				this.move_target = gi.pointing.position_3d;
			}
		}

		if(gi.pointing.trigger_on) {

			if(gi.pointing.pointee == GameInput.POINTEE.ITEM) {

				if(this.controll.item_carrier.isCarrying()) {

					// 諡ｾ縺?ｸｭ縺ｮ繧｢繧､繝?Β繧偵け繝ｪ繝?け縺励◆縺ｨ縺阪?縲√→繧翫≠縺医★菴輔ｂ縺励↑縺?
					if(gi.pointing.pointee_name == this.controll.item_carrier.item.name) {
	
						this.move_target    = this.controll.getPosition();
						gi.pointing.pointee = GameInput.POINTEE.NONE;
					}
				}
			}

			// 繧｢繧､繝?Β/繧ｭ繝｣繝ｩ繧ｯ繧ｿ繝ｼ繧偵け繝ｪ繝?け縺輔ｌ縺?
			if(gi.pointing.pointee == GameInput.POINTEE.ITEM || gi.pointing.pointee == GameInput.POINTEE.CHARACTER) {

				Vector3		item_pos;
				Vector3		item_size;

				if(gi.pointing.pointee == GameInput.POINTEE.ITEM) {

					this.controll.item_man.getItemPosition(out item_pos, gi.pointing.pointee_name);
					this.controll.item_man.getItemSize(out item_size, gi.pointing.pointee_name);

				} else {

					chrController	chr = CharacterRoot.getInstance().findCharacter(gi.pointing.pointee_name);

					item_pos  = chr.transform.position;

					if(chr.GetComponent<BoxCollider>() != null) {

						item_size = chr.GetComponent<BoxCollider>().size;

					} else {

						item_size = chr.GetComponent<Collider>().bounds.size;
					}
				}

				dist = item_pos - this.transform.position;
				dist.y = 0.0f;

				item_size.y = 0.0f;

				float	distance_to_pick = (this.gameObject.GetComponent<Collider>().bounds.size.x + item_size.magnitude)/2.0f;

				if(this.step.get_current() == STEP.HOUSE_MOVE) {

					// 蠑戊ｶ翫＠荳ｭ縺ｫ螳ｶ繧偵け繝ｪ繝?け縺輔ｌ縺溘ｉ縲√♀縺ｲ縺｣縺薙＠縺翫＠縺ｾ縺?
					if(gi.pointing.pointee_name == this.name) {

						this.controll.cmdQueryHouseMoveEnd();
						this.step.set_next(STEP.WAIT_QUERY);
					}

				} else {

					if(dist.magnitude < distance_to_pick) {
	
						// 霑代＞譎?

						if(gi.pointing.pointee == GameInput.POINTEE.ITEM) {
	
							// 繧｢繧､繝?Β縺ｪ繧画鏡縺?
							this.controll.cmdItemQueryPick(gi.pointing.pointee_name);
							this.step.set_next(STEP.WAIT_QUERY);

						} else if(gi.pointing.pointee == GameInput.POINTEE.CHARACTER) {
							
							// 繧ｭ繝｣繝ｩ繧ｯ繧ｿ繝ｼ.

							// 螳ｶ縺ｮ縺ｨ縺阪?蠑戊ｶ翫＠.
							if(gi.pointing.pointee_name.ToLower().StartsWith("house")) {

								if(this.isEnableHouseMove()) {

									this.controll.cmdQueryHouseMoveStart(gi.pointing.pointee_name);
									this.step.set_next(STEP.WAIT_QUERY);
								}
							}
						}
	
						// 繧｢繧､繝?Β繧呈鏡縺｣縺溽峩蠕後↓遘ｻ蜍輔ｒ蜀埼幕縺励※縺励∪繧上↑縺?ｈ縺???						// 遘ｻ蜍慕岼讓吩ｽ咲ｽｮ繧偵け繝ｪ繧｢繝ｼ縺励※縺翫￥.
						gi.pointing.pointee = GameInput.POINTEE.NONE;
						this.move_target    = this.controll.getPosition();
	
					} else {
	
						// 驕?縺?凾縺ｯ縺昴％縺ｾ縺ｧ遘ｻ蜍?
						this.move_target      = gi.pointing.position_3d;
						this.move_target_item = gi.pointing.pointee_name;
					}
				}
			}
		}
	}

	// 豈弱ヵ繝ｬ繝ｼ繝? LateUpdate() 縺九ｉ繧医?繧後ｋ.
	public override void	lateExecute()
	{
#if false
		GameObject	head = this.gameObject.findDescendant("anim_neck");

		Vector3		camera_position = head.transform.parent.InverseTransformPoint(CameraControl.get().transform.position);
		Vector3		up              = head.transform.parent.InverseTransformDirection(Vector3.up);

		camera_position.Normalize();

		head.transform.localRotation = Quaternion.LookRotation(camera_position, up)*Quaternion.AngleAxis(-90.0f, Vector3.forward);
#endif
	}

	// ================================================================ //

	// 繝ｭ繝ｼ繧ｫ繝ｫ繝励Ξ繧､繝､繝ｼ??
	public override bool		isLocal()
	{
		return(true);
	}

	// 螟夜Κ縺九ｉ縺ｮ繧ｳ繝ｳ繝医Ο繝ｼ繝ｫ繧帝幕蟋九☆繧?
	public override void 	beginOuterControll()
	{
		base.beginOuterControll();

		this.controll.cmdSetMotion("Take 002", 0);
		this.step.set_next(STEP.OUTER_CONTROL);
	}

	// 螟夜Κ縺九ｉ縺ｮ繧ｳ繝ｳ繝医Ο繝ｼ繝ｫ繧堤ｵゆｺ?☆繧?
	public override void		endOuterControll()
	{
		base.endOuterControll();

		this.move_target = this.transform.position;
		this.step.set_next(STEP.MOVE);
	}

	// ================================================================ //
	// 縺翫?縺｣縺薙＠??OUSE_MOVE??

	// 縺ｲ縺｣縺薙＠髢句ｧ九う繝吶Φ繝医?繝?け繧ｹ縺ｫ蜈･縺｣縺溘→縺阪↓蜻ｼ縺ｰ繧後ｋ.
	public void		onEnterHouseMoveEventBox()
	{
		this.is_within_house_move_event_box = true;
	}

	// 縺ｲ縺｣縺薙＠髢句ｧ九う繝吶Φ繝医?繝?け繧ｹ縺九ｉ蜃ｺ縺溘→縺阪↓蜻ｼ縺ｰ繧後ｋ.
	public void		onLeaveHouseMoveEventBox()
	{
		this.is_within_house_move_event_box = false;
	}

	// 縺翫?縺｣縺薙＠髢句ｧ?
	public override void		beginHouseMove(chrBehaviorNPC_House house)
	{
		base.beginHouseMove(house);

		this.step.set_next(STEP.HOUSE_MOVE);
	}

	// 縺翫?縺｣縺薙＠邨ゆｺ??縺ｨ縺阪?蜃ｦ逅?
	public override void		endHouseMove()
	{
		base.endHouseMove();

		this.step.set_next(STEP.MOVE);
	}

	// 縺翫?縺｣縺薙＠荳ｭ??
	public override bool		isNowHouseMoving()
	{
		return(this.step.get_current() == STEP.HOUSE_MOVE);
	}

	// 縺翫?縺｣縺薙＠縺ｧ縺阪ｋ??
	public bool		isEnableHouseMove()
	{
		bool	ret = false;

		do {

			if(!this.is_within_house_move_event_box) {

				break;
			}

			ItemFavor	favor = this.controll.getItemFavor();

			if(favor == null) {

				break;
			}	

			ret = favor.is_enable_house_move;

		} while(false);

		return(ret);
	}

	// STEP.HOUSE_MOVE 縺ｮ蛻晄悄蛹?
	protected void	initialize_step_house_move()
	{
		this.initialize_step_house_move_common();

		this.move_target = this.transform.position;
	}

	// STEP.HOUSE_MOVE 縺ｮ螳溯｡?
	protected void	execute_step_house_move()
	{
		this.execute_step_house_move_common();

		this.exec_step_move();
	}

}
