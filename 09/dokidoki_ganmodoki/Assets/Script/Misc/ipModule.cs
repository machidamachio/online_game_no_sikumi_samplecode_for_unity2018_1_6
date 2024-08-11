using UnityEngine;
using System.Collections;

namespace ipModule {

public class Base {

	protected bool		is_started = false;
	protected bool		is_done    = false;
	protected bool		is_trigger_done = false;

	// ================================================================ //

	public Base()
	{
	}

	// リセット（パラメーターをデフォルト値に戻す）.
	public virtual void		reset()
	{
		this.is_started = false;
		this.is_done    = false;
	}
	public void		start()
	{
		this.is_started = true;
		this.is_done    = false;
	}
	public virtual void		cancel()
	{
		this.is_started = false;
		this.is_done    = false;
	}

	public virtual void		setDelay(float delay)
	{
	}

	public bool		isStarted()
	{
		return(this.is_started);
	}
	public bool		isMoving()
	{
		return(this.is_started && !this.is_done);
	}
	public bool		isDone()
	{
		return(this.is_started && this.is_done);
	}

	public bool		isTriggerDone()
	{
		return(this.is_trigger_done);
	}
}

// ふわふわ.
public class Hover : Base {


	public float	accel_scale = 2.0f;

	public float	zero_level =  3.0f;
	public float	height     =  3.0f;
	public float	velocity   =  0.0f;
	public float	gravity    = -9.8f;
	public float	accel      = 0.0f;

	public bool		trigger_down_peak = false;

	public struct Range {

		public Range(float max, float min)
		{
			this.max = max;
			this.min = min;
		}
		public float	max;
		public float	min;
	};

	public Range	soft_limit = new Range(0.3f, -0.3f);
	public Range	hard_limit = new Range(1.0f, -1.0f);

	// ================================================================ //

	public void		execute(float delta_time)
	{
		float	accel_base = Mathf.Abs(this.gravity)*this.accel_scale;

		float	high = this.zero_level + this.soft_limit.max;
		float	low  = this.zero_level + this.soft_limit.min;

		float	previous_velocity = this.velocity;

		if(this.velocity >= 0.0f) {

			if(this.height > high) {

				this.accel = 0.0f;

			} else {

				float	s = this.velocity*this.velocity/(2.0f*Mathf.Abs(this.gravity));

				if(s > Mathf.Abs(high - this.height)) {

					this.accel = 0.0f;

				} else {

					this.accel = accel_base;
				}
			}

		} else {

			if(this.height < low) {

				this.accel = accel_base;

			} else {

				float	s = this.velocity*this.velocity/(2.0f*Mathf.Abs(accel_base + this.gravity));

				if(s > Mathf.Abs(low - this.height)) {

					this.accel = accel_base;

				} else {

					this.accel = 0.0f;
				}
			}
		}

		this.accel    += this.gravity;
		this.velocity += this.accel*delta_time;	

		this.height += this.velocity*delta_time;
		this.height = Mathf.Clamp(this.height, this.zero_level + this.hard_limit.min, this.zero_level + this.hard_limit.max);

		//

		this.trigger_down_peak = false;

		if(previous_velocity < 0.0f && 0.0f <= this.velocity) {

			this.trigger_down_peak = true;
		}
	}

}

// ばね.
public class Spring : Base {

	public float		position;
	public float		velocity;
	public float		k = 100.0f;
	public float		reduce = 1.0f;

	public float		max_epsilon = 1.0f;		// [sec] 分割計算するときの、１回あたりの経過時間.
	public float		max_delta   = 1.0f;

	// ================================================================ //

	public Spring()
	{
		this.max_epsilon = (1.0f/60.0f)*1.0f;
		this.max_delta   = this.max_epsilon*10.0f;
	}
	public void start(float position)
	{
		base.start();

		this.position = position;
		this.velocity = 0.0f;
	}

	// 毎フレームの実行処理.
	public void	execute(float delta_time)
	{
		delta_time = Mathf.Min(delta_time, this.max_delta);

		int		n = Mathf.FloorToInt(delta_time/this.max_epsilon);

		for(int i = 0;i < n;i++) {

			this.execute_sub(this.max_epsilon);
		}

		this.execute_sub(Mathf.Max(0.0f, delta_time - (float)n*this.max_epsilon));
	}

	public void	execute_sub(float delta_time)
	{
		this.velocity *=  this.reduce;
		this.velocity += -this.k*this.position*delta_time;
		this.position +=  this.velocity*delta_time;
	}
}

// ジャンプ.
public class Jump : Base {

	public Vector3		position;
	public Vector3		velocity;

	public Vector3		gravity;

	public Vector3		goal;

	public float		t0;			// [sec] ジャンプの頂点に到達するまでの時間.
	public float		t1;			// [sec] ジャンプの頂点から着地するまでの時間.

	public Vector3		bounciness = Vector3.zero;		// 跳ね返り係数.

	public bool			is_trigger_bounce = false;		// 着地した瞬間？.
	public int			bounce_count = 0;				// 着地した回数.

	// ================================================================ //

	public void		setBounciness(Vector3 bounciness)
	{
		this.bounciness = bounciness;
	}

	public Vector3	xz_velocity()
	{
		return(new Vector3(this.velocity.x, 0.0f, this.velocity.z));
	}

	public Jump()
	{
		this.position = Vector3.zero;
		this.velocity = Vector3.zero;

		this.bounciness = Vector3.zero;
		this.gravity    = Physics.gravity;
	}

	public void	start(Vector3 start, Vector3 goal, float peak_height)
	{
		base.start();

		peak_height = Mathf.Max(peak_height, start.y);
		peak_height = Mathf.Max(peak_height, goal.y);

		float	vy = Mathf.Max(0.0f, 2.0f*Mathf.Abs(this.gravity.y)*(peak_height - start.y));

		vy = Mathf.Sqrt(vy);

		this.t0 = vy/Mathf.Abs(this.gravity.y);
		this.t1 = Mathf.Sqrt(2.0f*(peak_height - goal.y)/Mathf.Abs(this.gravity.y));

		Vector3		vxz = goal - start;

		vxz.y = 0.0f;
		vxz  /= (this.t0 + this.t1);

		if(float.IsNaN(vxz.x) || float.IsNaN(vxz.y) || float.IsNaN(vxz.z)) {
			vxz = Vector3.zero;
		}

		this.position = start;
		this.velocity = new Vector3(vxz.x, vy, vxz.z);

		this.goal = goal;

		//

		this.is_trigger_bounce = false;
		this.bounce_count      = 0;
	}

	public void	forceFinish()
	{
		this.velocity = Vector3.zero;
		this.position = this.goal;
		this.is_trigger_bounce = false;	
		this.is_done = true;
	}

	// 毎フレームの実行処理.
	public void	execute(float delta_time)
	{
		do {

			this.is_trigger_bounce = false;	

			if(this.is_done) {

				break;
			}

			this.velocity += this.gravity*delta_time;
			this.position += this.velocity*delta_time;

			// 終了チェック.
			do {

				// 地面にあたった？.
				if(this.velocity.y >= 0.0f) {

					break;
				}	
				if(this.position.y > this.goal.y) {

					break;
				}

				this.is_trigger_bounce = true;	
				this.bounce_count++;

				// 跳ね返り.
				this.velocity.x *= this.bounciness.x;
				this.velocity.y *= this.bounciness.y;
				this.velocity.z *= this.bounciness.z;

				// はねかえった後の速度が十分小さかったら着地.
				//	（速度が重力より小さい
				//	　＝次のフレームでもすぐ速度がマイナスになる
				//	　＝連続して地面に当たる
				//	　なので、着地したとみなせる）.	
				if(Mathf.Abs(this.velocity.y) > Mathf.Abs(this.gravity.y)*Time.deltaTime) {

					break;
				}

				this.velocity   = Vector3.zero;
				this.position.y = this.goal.y;
				this.is_done    = true;

			} while(false);

		} while(false);
	}
}

// 二点間の補間.
public class Simple2Points : Base {

	public static float	CLOSEST_DISTANCE = 0.01f;		// 超近いとみなす距離.

	// 位置.
	public struct Positions {

		public Vector3	start;
		public Vector3	goal;

		public Vector3	current;
	};
	public Positions	position;

	// 速度.
	protected struct Velocities {

		public float	max;
		public float	current;
	};
	protected Velocities	velocity;

	public Vector3	velocity_vector = Vector3.one;	// 正規化した速度ベクトル.

	public float	accel_time = 1.0f;				// [sec] 加減速にかける時間.

	// ================================================================ //

	public Simple2Points()
	{
		this.position.start   = Vector3.zero;
		this.position.goal    = Vector3.zero;
		this.position.current = Vector3.zero;

		this.velocity.max     = 1.0f;
		this.velocity.current = 1.0f;
	}

	public void		startConstantVelocity(float velocity_magnitude)
	{
		base.start();

		this.velocity_vector = this.position.goal - this.position.start;
		this.velocity_vector.Normalize();

		this.velocity.max     = velocity_magnitude;
		this.velocity.current = 0.0f;

		this.position.current = this.position.start;

		if(this.velocity.max < CLOSEST_DISTANCE) {

			// 最初から超近かったら、何もしないでおしまい.
			this.is_done = true;

		} else {

			this.is_done = false;
		}
	}

	public new void		start()
	{
		base.start();

		this.startConstantVelocity(Vector3.Distance(this.position.start, this.position.goal));
	}

	// 毎フレームの更新.
	public void		execute(float delta_time)
	{
		do {

			if(!this.is_started) {

				break;
			}
			if(this.is_done) {

				break;
			}

			//

			Vector3		left = this.position.goal - this.position.current;

			// 加速度.
			float	accel = this.velocity.max/this.accel_time;

			if(float.IsInfinity(accel)) {

				accel = float.MaxValue;
			}
			// ぴったり止まるための減速度.
			float	stop_accel = (this.velocity.current*this.velocity.current)/(2.0f*left.magnitude);

			if(stop_accel >= accel) {

				accel = -stop_accel;
			}

			this.velocity.current += accel*delta_time;
			this.velocity.current = Mathf.Clamp(this.velocity.current, 0.0f, this.velocity.max);

			// 終了した？　判定.
			do {

				this.is_done = true;

				if(left.magnitude <= this.velocity.current*delta_time) {

					break;
				}
				if(this.velocity.current == 0.0f) {

					break;
				}

				this.is_done = false;

			} while(false);


			if(!this.is_done) {

				Vector3		velocity = this.velocity_vector*this.velocity.current;

				this.position.current += velocity*delta_time;

			} else {

				this.position.current = this.position.goal;
			}

		} while(false);
	}

};

// 制御点ふたつのイーズイン/アウト.
public class FCurve : Base {

	public struct Time {

		public float	duration;
		public float	current;
		public float	delay;
	};

	public float	dy_dx0 = 1.0f;
	public float	dy_dx1 = 1.0f;
	public int		feedback = 0;

	public Time	time;

	public float	y = 0.0f;

	protected float[]	konst = new float[4];

	// ================================================================ //

	public FCurve()
	{
		this.reset();
	}

	// リセット（パラメーターをデフォルト値に戻す）.
	public override void	reset()
	{
		base.reset();
		this.time.duration = 1.0f;
		this.time.delay = -1.0f;
	}

	// スタート.
	public new void		start()
	{
		base.start();

		this.y = 0.0f;
		this.time.current = 0.0f;

		this.konst[0] = dy_dx0 + dy_dx1 - 2.0f;
		this.konst[1] = -2.0f*dy_dx0 - dy_dx1 + 3.0f;
		this.konst[2] = dy_dx0;
		this.konst[3] = 0.0f;
	}

	// 毎フレームの更新処理.
	public void		execute(float delta_time)
	{
		do {

			this.is_trigger_done = false;

			if(this.is_done) {

				break;
			}

			if(this.time.delay > 0.0f) {

				this.time.delay -= delta_time;

				if(this.time.delay <= 0.0f) {

					this.time.delay = -1.0f;
				}
				this.y = 0.0f;
				break;
			}

			this.time.current += delta_time;

			if(this.time.current >= this.time.duration) {

				this.time.current    = this.time.duration;
				this.is_done         = true;
				this.is_trigger_done = true;
			}

			float	x = Mathf.InverseLerp(0.0f, this.time.duration, this.time.current);

			for(int i = 0;i < this.feedback + 1;i++) {

				x = this.calc_y(x);
			}

			this.y = x;

		} while(false);
	}

	public float	getValue()
	{
		return(this.y);
	}

	// [sec] 時間の長さをセットする.
	public void		setDuration(float duration)
	{
		this.time.duration = duration;
	}

	public override void	setDelay(float delay)
	{
		this.time.delay = delay;
	}

	// [degree] 開始、終了のスロープの角度をセットする.
	public void		setSlopeAngle(float start, float end)
	{
		this.dy_dx0 = Mathf.Tan(start*Mathf.Deg2Rad);
		this.dy_dx1 = Mathf.Tan(  end*Mathf.Deg2Rad);
	}

	// ================================================================ //

	protected float	calc_y(float x)
	{
		float	y = this.konst[0]*x*x*x + this.konst[1]*x*x + this.konst[2]*x + this.konst[3];

		y = Mathf.Min(y, 1.0f);

		return(y);
	}

};

} // namespace ipModule

