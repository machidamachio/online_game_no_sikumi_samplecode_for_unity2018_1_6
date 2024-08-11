using UnityEngine;
using System.Collections;

public class chrControllerEnemyBase : chrController {

	// FIXME ステート文法のない言語だから、ここに公倍数的なステートリストを作るしかないか？.
	public enum EnemyState
	{
		MAIN,
		VANISH,

		JUMP_PREACTION,	// 飛び上がり前の予備動作.
		JUMPING,		// ジャンプ中（＝アニメーション表示中）

		CHARGE_PREACTION,
		CHARGING,
		CHARGE_FINISHED,

		QUICK_ATTACK,
	};
	
	public EnemyState state = EnemyState.MAIN;

	protected float localStateTimer;

	public bool			damage_trigger = false;

	//========================================================================
	// キャラクターメトリクス
	public float		life = 5.0f;		// ライフ.
	public float		maxSpeed = 2.0f;	// 速度.

	protected float		floorY = 0.48f;		// ピボットの高さ.

	//========================================================================
	// ローカルな移動で処理するためのパラメータ.
	public float		move_dir = 0.0f;					// 移動方向.
	public float		velocity = 0.0f;					// 移動速度.
	public float		acceleration = 0.0f;				// 移動加速度.
	
	protected bool		isPaused = false;					// 演出的な理由で一時停止を命令されているかどうか.

	//========================================================================
	protected RoomController	room;							//< reference to room.

	//===================================================================
	// アニメーション周り
	protected Animator animator;

	//========================================================================
	// 一時的にmaxSpeedを高める場合のモディファイア.
	protected virtual float getMaxSpeedModifier()
	{
		return 1.0f;
	}

	// 敵制御用のテンプレートメソッド.
	// 着地を検出した際にコールされる.（or コールする）.
	protected virtual void landed()
	{
	}
	
	// ライフなどのデフォルトプロパティの変更など.
	override protected void _awake()
	{
		base._awake();
		
		animator = GetComponent<Animator>();
	}

	override protected void _start()
	{
		base._start();
		
		// RigidBodyのY軸をフリーズ.
		GetComponent<Rigidbody>().constraints |= RigidbodyConstraints.FreezePositionY;
	}
	
	override protected void execute()
	{
		if (isPaused) {
			return;
		}
	
		localStateTimer += Time.deltaTime;

		// FIXME: フレームレートが落ちると、ダメージの処理量が変わってしまうが、、、.
		if (state != EnemyState.VANISH) {
			if(this.damage_trigger) {
				this.life -= 1.0f;
				if(this.life <= 0.0f) {
					goToVanishState(true);
				}
				else {
					playDamage();
				}
			}
		}

		switch (state) {
		case EnemyState.MAIN:
			exec_step_move();
			break;

		case EnemyState.VANISH:
			exec_step_vanish();
			break;
		default:
			break;
		}
		
		this.damage_trigger = false;

		updateAnimator();
	}

	protected virtual void exec_step_vanish()
	{
		if (this.damage_effect.isVacant())
		{
			if (room != null) {
			}

			(this.behavior as chrBehaviorEnemy).onDelete();

			EnemyRoot.getInstance().deleteEnemy(this);
		}
	}

	protected void changeState(EnemyState newState)
	{
        Debug.Log(newState);
        Debug.Log(state);
		if (state != newState)
		{
			state = newState;
			localStateTimer = 0.0f;
		}
	}

	public virtual void		causeDamage()
	{
		// テイクダメージが有効なステートかどうかをチェックしてから処理.
		if (this.state != EnemyState.VANISH)
		{
			this.damage_trigger = true;
		}
	}
	
	public virtual void		playDamage()
	{
		this.damage_effect.startDamage();
		if (animator != null)
		{
			animator.SetTrigger("Damage");
		}
		SoundManager.getInstance().playSE(Sound.ID.DDG_SE_ENEMY01);
	}
	
	public virtual void		causeVanish(bool is_local = true)
	{
		goToVanishState(is_local);
	}
	
	protected virtual void goToVanishState(bool is_local)
	{
		// 移行を許すステート元かをチェックして状態遷移実行.
		if (this.state != EnemyState.VANISH)
		{
			this.behavior.onVanished();

			playDying();

			this.state = EnemyState.VANISH;
			this.localStateTimer = 0.0f;
			this.damage_effect.startVanish();

			// ヒットを抜く
			this.GetComponent<Collider>().enabled = false;
			this.GetComponent<Rigidbody>().Sleep();

			// シーン読み込み等により, 開始時刻がずれてしまう場合があるため,
			// 念のためボスの死亡通知を送信します.
			if (is_local) {
				EnemyRoot.get().RequestBossDead(this.behavior.name);
			}
		}
	}

	// 死亡演出
	protected virtual void playDying()
	{
		SoundManager.getInstance().playSE(Sound.ID.DDG_SE_ENEMY02);
	}

	// STEP.MOVE の実行.
	// 移動... FIXME  We have to call this from FixedUpdate().
	protected void	exec_step_move()
	{
		// ---------------------------------------------------------------- //
		// 移動（位置座標の補間）.
		
		Vector3		position  = this.getPosition();
		float		cur_dir   = this.getDirection();

		velocity += acceleration * Time.deltaTime;
		velocity = Mathf.Clamp(velocity, 0.0f, maxSpeed * getMaxSpeedModifier());
		float		speed_per_frame = velocity * Time.deltaTime;
		
		Vector3		move_vector = Quaternion.AngleAxis(this.move_dir, Vector3.up)*Vector3.forward;

		if (speed_per_frame > 0.0f) {
			position += move_vector*speed_per_frame;
		
			// 向きの補間.
		
			float	dir_diff = this.move_dir - cur_dir;
			
			if(dir_diff > 180.0f) {
				
				dir_diff = dir_diff - 360.0f;
				
			} else if(dir_diff < -180.0f) {
				
				dir_diff = dir_diff + 360.0f;
			}
			
			dir_diff *= 0.1f;
			
			if(Mathf.Abs(dir_diff) < 1.0f) {
				
				cur_dir = this.move_dir;
				
			} else {
				
				cur_dir += dir_diff;
			}
			
			// FIXME 動的なフロアコンタクトにするべき.
			position.y = floorY;
		}

		this.cmdSetPosition(position);
		this.cmdSetDirection(cur_dir);
	}

	protected virtual void updateAnimator()
	{
		if (animator != null)
		{
			animator.SetFloat("Motion_Speed", velocity / maxSpeed);
		}
	}
	
	//======================================================================
	//
	// 外からコールされるメソッド.
	//

	// このエネミーとルームを紐付ける.	
	public void SetRoom(RoomController a_room)
	{
		this.room = a_room;
	}
	
	/// <summary>
	/// キャラクター制御の一時停止を設定します
	/// </summary>
	/// <param name="newPause">真のときポーズが働き、偽のときポーズが解除されます</param>
	public void SetPause(bool newPause)
	{
		isPaused = newPause;
		if (animator != null)
		{
			animator.speed = isPaused ? 0.0f : 1.0f;
		}
	}	
}
