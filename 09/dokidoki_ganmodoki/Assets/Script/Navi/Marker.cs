using UnityEngine;
using System.Collections;
using MathExtension;
using GameObjectExtension;

// プレイヤーの位置をさすマーカー.
public class Marker : MonoBehaviour {

	public enum STEP {

		NONE = -1,

		ENTER = 0,		// 画面外から登場.
		STAY,			// ループ表示.
		LEAVE,			// 画面外へ退場.

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	// ---------------------------------------------------------------- //

	public Texture	karada_texture;
	public Texture	ude_texture;
	public Texture	under_texture;

	protected Sprite2DControl	under_sprite;
	protected Sprite2DControl	karada_sprite;
	protected Sprite2DControl	ude_sprite;

	protected Vector2	offset = new Vector2(-43.0f, -16.0f);			// 腕のスプライトの中心位置.
	protected Vector2	rotation_center = new Vector2(38.0f, 0.0f);		// 腕のスプライトの回転位置.

	protected float	timer = 0.0f;

	protected Vector2	base_position_stay = new Vector2(110.0f,  90.0f);
	protected Vector2	base_position_start = new Vector2(700.0f,  90.0f);

	protected Vector2	base_position;

	protected SimpleSplineObject	enter_curve;
	protected SimpleSpline.Tracer	tracer = new SimpleSpline.Tracer();
	protected ipModule.FCurve		enter_fcurve = new ipModule.FCurve();

	protected SimpleSplineObject	leave_curve;
	protected ipModule.FCurve		leave_fcurve = new ipModule.FCurve();

	protected Vector2	enter_curve_offset;
	protected Vector2	leave_curve_offset;

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Awake()
	{
		this.base_position_stay = new Vector2(110.0f,  90.0f);

		this.enter_curve = this.transform.Find("enter_spline").gameObject.GetComponent<SimpleSplineObject>();
		this.enter_curve.createControlVertices();
		this.enter_curve_offset = this.base_position_stay - this.enter_curve.curve.cvs.back().position.xz()*480.0f/2.0f;

		this.leave_curve = this.transform.Find("leave_spline").gameObject.GetComponent<SimpleSplineObject>();
		this.leave_curve.createControlVertices();
		this.leave_curve_offset = this.base_position_stay - this.leave_curve.curve.cvs.front().position.xz()*480.0f/2.0f;
	}

	void	Start()
	{
		this.step.set_next(STEP.ENTER);
	}

	void 	Update()
	{
		float	enter_time = 1.5f;
		float	stay_time  = 3.0f;
		float	leave_time = 1.0f;

		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			// 画面外から登場.
			case STEP.ENTER:
			{
				if(this.step.get_time() > enter_time) {

					this.step.set_next(STEP.STAY);
				}
			}
			break;

			// ループ表示.
			case STEP.STAY:
			{
				if(this.step.get_time() > stay_time) {

					this.step.set_next(STEP.LEAVE);
				}
			}
			break;

			// 画面外へ退場.
			case STEP.LEAVE:
			{
				if(this.step.get_time() > leave_time) {

					this.destroy();
				}
			}
			break;
		}


		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {

				// 画面外から登場.
				case STEP.ENTER:
				{
					this.enter_fcurve.setSlopeAngle(70.0f, 5.0f);
					this.enter_fcurve.setDuration(enter_time);
					this.enter_fcurve.start();

					this.tracer.attach(this.enter_curve.curve);
				}
				break;

				// 画面外へ退場.
				case STEP.LEAVE:
				{
					this.leave_fcurve.setSlopeAngle(10.0f, 70.0f);
					this.leave_fcurve.setDuration(leave_time);
					this.leave_fcurve.start();

					this.tracer.attach(this.leave_curve.curve);
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			// 画面外から登場.
			case STEP.ENTER:
			{
				this.enter_fcurve.execute(Time.deltaTime);

				this.tracer.proceedToDistance(this.enter_fcurve.getValue()*this.tracer.curve.calcTotalDistance());

				this.base_position = this.tracer.getCurrent().position.xz()*480.0f/2.0f + this.enter_curve_offset;
			}
			break;

			// 画面外へ退場.
			case STEP.LEAVE:
			{
				this.leave_fcurve.execute(Time.deltaTime);

				this.tracer.proceedToDistance(this.leave_fcurve.getValue()*this.tracer.curve.calcTotalDistance());

				this.base_position = this.tracer.getCurrent().position.xz()*480.0f/2.0f + this.leave_curve_offset;
			}
			break;
		}

		// ---------------------------------------------------------------- //

		this.set_position();

		//

		this.timer += Time.deltaTime;
	}

	// ================================================================ //

	// つくる.
	public void		create()
	{
		// 下敷き（腕のまわりの青いふち）.
		this.under_sprite = Sprite2DRoot.get().createSprite(this.under_texture, true);
		this.under_sprite.setSize(new Vector2(this.under_texture.width, this.under_texture.height)/4.0f);

		// 体.
		this.karada_sprite = Sprite2DRoot.get().createSprite(this.karada_texture, true);
		this.karada_sprite.setSize(new Vector2(this.karada_texture.width, this.karada_texture.height)/4.0f);

		// うで.
		this.ude_sprite = Sprite2DRoot.get().createSprite(this.ude_texture, true);
		this.ude_sprite.setSize(new Vector2(this.ude_texture.width, this.ude_texture.height)/4.0f);

		// 位置をセットしておく.

		this.base_position = this.base_position_start;
		this.set_position();
	}

	// 削除する.
	public void		destroy()
	{
		this.under_sprite.destroy();
		this.karada_sprite.destroy();
		this.ude_sprite.destroy();

		this.gameObject.destroy();
	}

	// 位置をセットする.
	public void		set_position()
	{
		Vector2	position = this.base_position;

		float	rate = Mathf.Repeat(this.timer, 4.0f);

		rate = Mathf.InverseLerp(0.0f, 4.0f, rate);

		position.y += 16.0f*Mathf.Sin(rate*Mathf.PI*2.0f);

		this.karada_sprite.setPosition(position);

		// 腕のアングルを求める.
		// プレイヤーをねぎ指すように.

		Vector2		player_position = new Vector2(0.0f, 32.0f);

		Vector2		v = player_position - (position + this.offset + this.rotation_center);

		float	angle = Mathf.Atan2(v.y, v.x)*Mathf.Rad2Deg;

		angle = MathUtility.snormDegree(angle + 180.0f);

		this.set_arm_position_angle(position, angle);

	}

	// 腕のスプライトの位置と角度を求める.
	protected void	set_arm_position_angle(Vector2 position, float angle)
	{
		// 回転の中心を肩の位置にする.

		Vector2		shift = this.rotation_center;

		shift -= (Quaternion.AngleAxis(angle, Vector3.forward)*this.rotation_center).xy();

		this.under_sprite.setPosition(position + offset + shift);
		this.ude_sprite.setPosition(position + offset + shift);	

		this.under_sprite.setAngle(angle);
		this.ude_sprite.setAngle(angle);	
	}
}
