using UnityEngine;
using System.Collections;

public class MeleeAttack : MonoBehaviour {

	public float	timer = 0.0f;

	protected bool	is_trigger_attack = false;
	protected bool	is_attacking      = false;
	protected bool	is_has_input      = false;			// 移動入力がある？.

	public chrBehaviorBase	behavior = null;

	public struct Result {

		public bool		player_attack;
		public bool		enemy_attack;

		public void		reset()
		{
			this.player_attack = false;
			this.enemy_attack  = false;
		}
	};
	public Result	result;

	public chrBehaviorBase	target = null;

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
		this.result.reset();
	}
	
	void	Update()
	{
	}

	// ================================================================ //

	public void		execute()
	{
		if(this.is_attacking) {

			if(this.behavior.control.getMotion() != "m003_attack") {

				this.is_attacking = false;
			}

		} else {

			if(this.is_trigger_attack) {
	
				this.timer = 0.5f;
				this.is_trigger_attack = false;
				this.is_attacking = true;

				this.behavior.control.cmdSetMotion("m003_attack", 1);
			}
		}

		if(this.is_attacking) {

			// ターゲットの方にむく.
			if(this.target != null) {

				this.behavior.control.cmdSmoothHeadingTo(this.target.control.getPosition());
			}

			do {

				if(this.behavior.control.getMotion() != "m003_attack") {

					break;
				}

				if(!this.behavior.control.isMotionAcrossingTime(0.35f)) {

					break;
				}

				// 攻撃範囲内にいる敵にダメージ.

				int		layer_mask = LayerMask.GetMask("Enemy", "EnemyLair", "Boss");
				var		coliders   = Physics.OverlapSphere(this.behavior.control.getPosition(), this.behavior.control.vital.getAttackDistance(), layer_mask);

				foreach(var colider in coliders) {

					chrController	chr = colider.gameObject.GetComponent<chrController>();

					if(chr == null) {

						continue;
					}
					if(!this.isInAttackRange(chr)) {

						continue;
					}

					chr.causeDamage(this.behavior.control.vital.getAttackPower(), this.behavior.control.global_index);

					// 『近接攻撃がヒットしたときに呼ぶ関数』を呼ぶ.
					this.behavior.onMeleeAttackHitted(chr.behavior);
				}

			} while(false);
		}

		this.timer -= Time.deltaTime;
		this.timer = Mathf.Max(0.0f, this.timer);

	}

	// 相手が攻撃できる範囲にいる？.
	public bool		isInAttackRange(chrController target)
	{
		bool	ret = false;

		do {

			chrController	mine = this.behavior.control;
	
			Vector3		to_enemy = target.getPosition() - mine.getPosition();
	
			to_enemy.y = 0.0f;
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

	// 移動中？をセットする.
	public void		setHasInput(bool is_has_input)
	{
		this.is_has_input = is_has_input;
	}

	// 移動中？.
	public bool		isHasInput()
	{
		return(this.is_has_input);
	}

	// 攻撃できる？.
	public bool		isEnableAttack()
	{
		bool	ret = false;

		do {

			if(this.timer > 0.0f) {

				break;
			}

			ret = true;

		} while(false);

		return(ret);
	}

	public void		attack(bool is_local)
	{
		this.is_trigger_attack = true;

		if (is_local) {
			
			CharacterRoot.get().SendAttackData(PartyControl.get().getLocalPlayer().getAcountID(), 1);
		}
	}

	public bool		isAttacking()
	{
		return(this.is_attacking);
	}

	public void		setTarget(chrBehaviorEnemy target)
	{
		this.target = target;
	}
}
