using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 繝薙?繧､繝薙い繝ｼ縲?繝阪ャ繝医?繝ｬ繧､繝､繝ｼ?医ご繧ｹ繝茨ｼ臥畑.
// 繝阪ャ繝医?繧牙女菫｡縺励◆繝??繧ｿ繝ｼ縺ｧ繧ｳ繝ｳ繝医Ο繝ｼ繝ｫ縺吶ｋ莠亥ｮ?
public class chrBehaviorNet : chrBehaviorPlayer {

	public enum STEP {

		NONE = -1,

		MOVE = 0,			// 遘ｻ蜍包ｼ域ｭ｢縺ｾ縺｣縺ｦ繧区凾繧ょ性繧???
		HOUSE_MOVE,			// 縺雁ｼ戊ｶ翫＠.
		OUTER_CONTROL,		// 螟夜Κ蛻ｶ蠕｡.

		WAIT_QUERY,			// 繧ｯ繧ｨ繝ｪ繝ｼ蠕?■.

		NUM,
	};
	Step<STEP>		step = new Step<STEP>(STEP.NONE);

	// ---------------------------------------------------------------- //

	// 3谺｡繧ｹ繝励Λ繧､繝ｳ陬憺俣縺ｧ菴ｿ逕ｨ縺吶ｋ轤ｹ謨ｰ.
	private const int PLOT_NUM = 4;

	// 髢灘ｼ輔″縺吶ｋ蠎ｧ讓吶?繝輔Ξ繝ｼ繝?謨ｰ.
	private const int CULLING_NUM = 10;

	// 迴ｾ蝨ｨ縺ｮ繝励Ο繝?ヨ縺ｮ繧､繝ｳ繝?ャ繧ｯ繧ｹ.
	private int 	m_plotIndex = 0;

	// 髢灘ｼ輔＞縺溷ｺｧ讓吶ｒ菫晏ｭ?
	private List<CharacterCoord>	m_culling = new List<CharacterCoord>();
	// 陬憺俣縺励◆蠎ｧ讓吶ｒ菫晏ｭ?
	private List<CharacterCoord>	m_plots = new List<CharacterCoord>();
	
	// 豁ｩ縺阪Δ繝ｼ繧ｷ繝ｧ繝ｳ.
	private struct WalkMotion {

		public bool		is_walking;
		public float	timer;
	};
	private	WalkMotion	walk_motion;

	private const float	STOP_WALK_WAIT = 0.1f;		// [sec] 豁ｩ縺?-> 遶九■繝｢繝ｼ繧ｷ繝ｧ繝ｳ縺ｫ遘ｻ陦後☆繧九→縺阪?迪ｶ莠域凾髢?

	// ================================================================ //
	// MonoBehaviour 縺九ｉ縺ｮ邯呎価.

	void	Start()
	{
	}
	
	void	Update()
	{
	}

	// ================================================================ //

	public override void	initialize()
	{
		this.walk_motion.is_walking = false;
		this.walk_motion.timer      = 0.0f;
	}

	public override void	start()
	{
		this.controll.balloon.setPriority(-1);

		// 繧ｲ繝ｼ繝?髢句ｧ狗峩蠕後↓ EnterEvent 縺悟ｧ九∪繧九→縲√％縺薙〒 next_step 縺ｫ.
		// OuterControll 縺後そ繝?ヨ縺輔ｌ縺ｦ縺?ｋ縲ゅ◎縺ｮ縺ｨ縺阪↓荳頑嶌縺阪＠縺ｪ縺?ｈ縺?↓縲?
		// next == NONE 縺ｮ繝√ぉ繝?け繧貞?繧後ｋ.
		if(this.step.get_next() == STEP.NONE) {

			this.step.set_next(STEP.MOVE);
		}
	}
	public override	void	execute()
	{
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
					//this.move_target = this.transform.position;
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

	}

	// 遘ｻ蜍輔↓髢｢縺吶ｋ蜃ｦ逅?
	protected void	exec_step_move()
	{
		Vector3		new_position = this.controll.getPosition();
		if(m_plots.Count > 0) {
			CharacterCoord coord = m_plots[0];
			new_position = new Vector3(coord.x, new_position.y, coord.z);
			m_plots.RemoveAt(0);
		}

		// 荳?迸ｬ縺ｨ縺ｾ縺｣縺溘□縺代?縺ｨ縺阪?豁ｩ縺阪Δ繝ｼ繧ｷ繝ｧ繝ｳ縺後→縺ｾ繧峨↑縺?ｈ縺?↓縺吶ｋ.

		bool	is_walking = this.walk_motion.is_walking;

		if(Vector3.Distance(new_position, this.controll.getPosition()) > 0.0f) {

			if(this.step.get_current() == STEP.HOUSE_MOVE) {
	
			} else {

				this.controll.cmdSmoothHeadingTo(new_position);
			}
			this.controll.cmdSetPosition(new_position);

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
			
			this.playWalkMotion();
			
		} else {
			
			this.stopWalkMotion();
		}
	}

	// ================================================================ //

	// 繝ｭ繝ｼ繧ｫ繝ｫ繝励Ξ繧､繝､繝ｼ??
	public override bool		isLocal()
	{
		return(false);
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

		this.step.set_next(STEP.MOVE);
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

	// ================================================================ //

	// STEP.HOUSE_MOVE 縺ｮ蛻晄悄蛹?
	protected void	initialize_step_house_move()
	{
		this.initialize_step_house_move_common();
	}

	// STEP.HOUSE_MOVE 縺ｮ螳溯｡?
	protected void	execute_step_house_move()
	{
		this.execute_step_house_move_common();

		this.exec_step_move();
	}

	// ================================================================ //

	public void		ReceivePointFromNet(Vector3 point)
	{
		CharacterCoord	coord;

		coord.x = point.x;
		coord.z = point.z;

		m_culling.Add(coord);

		SplineData	spline = new SplineData();

		spline.CalcSpline(m_culling, 4);

		m_plots.Clear();

		if(spline.GetPlotNum() > 0) {

			for(int i = 0;i < spline.GetPlotNum();i++) {

				CharacterCoord	plot;

				spline.GetPoint(i, out plot);

				m_plots.Add(plot);
			}
		}
	}

	public void CalcCoordinates(int index, CharacterCoord[] data)
	{
		SplineData	spline = new SplineData();
		
		for (int i = 0; i < data.Length; ++i) {
			int p = index - PLOT_NUM - i + 1;
			if (p < m_plotIndex) {
				m_culling.Add(data[i]);
			}
		}
		
		// 譛?譁ｰ縺ｮ蠎ｧ讓吶ｒ險ｭ螳壹＠縺?
		m_plotIndex = index;
		
		// 繧ｹ繝励Λ繧､繝ｳ譖ｲ邱壹ｒ豎ゅａ縺ｦ陬憺俣縺吶ｋ.	
		spline.CalcSpline(m_culling, CULLING_NUM);
		
		// 豎ゅａ縺溘せ繝励Λ繧､繝ｳ陬憺俣繧貞ｺｧ讓呎ュ蝣ｱ縺ｨ縺励※菫晏ｭ倥☆繧?
		CharacterCoord plot = new CharacterCoord();
		for (int i = 0; i < spline.GetPlotNum(); ++i) {
			spline.GetPoint(i, out plot);
			m_plots.Add(plot);
		}
		
		// 荳?逡ｪ蜿､縺?ｺｧ讓吶ｒ蜑企勁.
		if (m_culling.Count > PLOT_NUM) {
			m_culling.RemoveAt(0);
		}
	}
}
