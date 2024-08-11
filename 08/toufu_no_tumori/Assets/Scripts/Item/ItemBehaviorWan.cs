using UnityEngine;
using System.Collections;

// アイテムのビヘイビアー　犬のなきごえ用.
public class ItemBehaviorWan : ItemBehaviorBase {

	// ================================================================ //

	public enum STEP {

		NONE = -1,

		READY = 0,			// 非表示で登場（dispatch)を待ってる.
		APPEAR,				// 登場.
		IDLE,
		ATTACHED,			// 拾われ中.

		NUM,
	};

	Step<STEP>		step = new Step<STEP>(STEP.NONE);

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
	}
	
	void	Update()
	{
	}

	// ================================================================ //

	protected struct StepAppear {

		public Vector3	velocity;
		public Vector3	position;
		public Vector3	direction;

		public bool		is_freezed;
	}

	protected StepAppear	step_appear;

	protected chrBehaviorNPC_Dog	chr_dog = null;			// いぬ（このアイテムのはっせいさせる犬）.

	// 生成直後に呼ばれる.
	public override void	initialize_item()
	{
		this.item_favor.term_word = "チョ";
	}

	// ゲーム開始時に一回だけ呼ばれる.
	public override void	start()
	{
		this.chr_dog = CharacterRoot.get().findCharacter<chrBehaviorNPC_Dog>("Dog");

		this.chr_dog.setItemWan(this);

		this.controll.setBillboard(true);
		this.controll.cmdSetVisible(false);

		this.step.set_next(STEP.READY);
	}

	// 毎フレームよばれる.
	public override void	execute()
	{
		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.


		switch(this.step.do_transition()) {
	
			case STEP.APPEAR:
			{
				if(this.step_appear.is_freezed) {

					this.step.set_next(STEP.IDLE);
				}
			}
			break;

			case STEP.IDLE:
			{
				if(this.step.get_time() > 3.0f) {

					//this.step.set_next(STEP.APPEAR);
				}
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {
	
				case STEP.READY:
				{
					this.controll.cmdSetPickable(false);
					this.controll.cmdSetVisible(false);
				}
				break;

				case STEP.APPEAR:
				{
					this.controll.cmdSetPickable(false);

					float		h    = 1.0f;
					float		dist = 0.8f;

					this.step_appear.velocity   = this.step_appear.direction;
					this.step_appear.velocity.y = Mathf.Sqrt(Mathf.Abs(2.0f*Physics.gravity.y*h));
				
					float	t = Mathf.Sqrt(2.0f*(2.0f*h)/Mathf.Abs(Physics.gravity.y));

					this.step_appear.velocity.x *= dist/t;
					this.step_appear.velocity.z *= dist/t;
				
					this.step_appear.is_freezed = false;
				}
				break;
	
				case STEP.IDLE:
				{
					this.controll.cmdSetPickable(true);
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			case STEP.APPEAR:
			{

				this.step_appear.velocity += Physics.gravity*Time.deltaTime;
				this.step_appear.position += this.step_appear.velocity*Time.deltaTime;

				if(this.step_appear.position.y < 0.0f) {

					this.step_appear.position.y  = 0.0f;

					this.step_appear.velocity.y *= -0.5f;
					this.step_appear.velocity.x *=  0.5f;
					this.step_appear.velocity.z *=  0.5f;

					if(this.step_appear.velocity.y < Mathf.Abs(Physics.gravity.y*Time.deltaTime)) {

						this.step_appear.is_freezed = true;
					}
				}

				this.controll.transform.position = this.step_appear.position;		

			}
			break;
		}

		// ---------------------------------------------------------------- //
	}

	// 拾われたときに呼ばれる.
	public override void	onPicked()
	{
		this.step.set_next(STEP.ATTACHED);
	}

	// リスポーンしたときに呼ばれる.
	// （吹き出しの場合は捨てられたとき）.
	public override void		onRespawn()
	{
		//this.controll.cmdSetPickable(false);
		this.step.set_next(STEP.READY);
	}

	// 吹き出しが犬から飛び出る.
	public void		beginDispatch(Vector3 position, Vector3 direction)
	{
		this.controll.cmdSetVisible(true);

		this.step_appear.position = position;
		this.step_appear.direction = direction;
		this.step_appear.direction.y = 0.0f;
		this.step_appear.direction.Normalize();

		this.step.set_next(STEP.APPEAR);
	}

	// 出現(dispatch)できる？.
	public bool		isEnableDispatch()
	{
		return(this.step.get_current() == STEP.READY);
	}
}
