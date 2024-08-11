using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class chrControllerEnemyBoss : chrControllerEnemyBase {

	private float	JUMP_PREACTION_DURATION         = 1.5f;
	private float	CHARGE_PREACTION_DURATION       = 1.5f;
	private float	FINISH_CHARGE_REACTION_DURATION = 1.0f;

    private const float JUMP_ATTACK_POWER = 3.0f;   //ジャンプ攻撃力.
    private float rangeAttackArea;                  //範囲攻撃での有効範囲(円半径).


	private float	angularMaxSpeed = 90.0f;	// 回頭スピード.

	// FIXME 三次元的な動きも含めるための、コード改修用.
	private Vector3 TotalVelocity;


	// 目標の角度.
	protected float desired_dir;

	//===================================================================
	// 突進攻撃関係のプロパティ.

	protected GameObject	targetToCharge;
    private Vector3 		chargeStartPoisition;			//突撃を開始したときの位置.
    private const float 	CHARGE_DISTANCE = 13.0f;		//突撃する最大距離.


	//===================================================================
	// エフェクト周りのプロパティ.
	private float		footSmokeRespawnTimer;			// 今回はアニメーションに打たず、プログラムで打つ. エフェクト再生成をカウントするタイマー. 
	public float		chargeFootSmokeRespawnDuration  = 1.0f / 10.0f;		// けっこういっぱい.
	public float		typhoonFootSmokeRespawnDuration = 1.0f / 10.0f;		// けっこういっぱい.

	private GameObject	chargeAura;						// 突撃オーラをみにつける.

	public Transform	neckSocket;						// 首の骨.
	public Transform	neckEndSocket;					// 鼻の先.
	private	float		snortSpawnTimer;				// 鼻息の間隔を計算するタイマー.
	private float		snortSpawnInterval;				// 鼻息を出す実際の間隔。生成毎に、snortSpawnIntervalMinとsnortSpawnIntervalMaxのローカルランダム値で決まる。.
	public float		snortSpawnIntervalMin = 0.2f;	// 鼻息を出す間隔の最小値.
	public float		snortSpawnIntervalMax = 0.4f;	// 鼻息を出す間隔の最大値.
	private bool		enableSpawnSnortEffects;		// 鼻息を出すかどうかを内部的に制御するフラグ.

	public Transform	headSocket;						// 怒りマークを出す座標を決めるためのトランスフォーム参照.

	protected float		model_scale = 0.5f;

	public float		getScale()
	{
		return(this.model_scale);
	}

	//===================================================================
	// アニメーション周り.
	private float		chargingSpeedModifier = 2.0f;
	private float		chargingForce = 10.0f;			// 突撃時の加速度.

	private chrBehaviorBase getBehavior()
	{
		if (behavior == null)
		{
			behavior = GetComponent<chrBehaviorBase>();
		}

		return behavior;
	}

	// AIヒント.
	public bool CanBeControlled()
	{
		return state == EnemyState.MAIN;
	}

	//===========================================================================
	//
	// ビヘイビアからのプロパティへの書き込み.
	//
	//
	public void SetMoveDirection(float newDir)
	{
		// 方向転換入力を受け付けるステートは限られている.
		if (state == EnemyState.MAIN)
		{
			move_dir = newDir;
		}
	}

	override protected float getMaxSpeedModifier()
	{
		switch (state)
		{
			case EnemyState.CHARGING:
				return chargingSpeedModifier;
			default:
				return base.getMaxSpeedModifier();
		}
	}

	// ライフなどのデフォルトプロパティの変更など.
	override protected void _awake()
	{
		base._awake();

		this.transform.localScale = Vector3.one*model_scale;

		// FIXME: 本来ならプレハブに任せたい.
		life = 100.0f;
		maxSpeed = 2.0f;
		floorY = -0.02f;
	}

	override protected void execute()
	{
		base.execute ();

		// 鼻息を出す.
		if (enableSpawnSnortEffects)
		{
			snortSpawnTimer += Time.deltaTime;
			if (snortSpawnTimer >= snortSpawnInterval)
			{
				snortSpawnTimer = 0.0f;
				snortSpawnInterval = Random.Range(snortSpawnIntervalMin, snortSpawnIntervalMax);
				createSnortEffect ();
			}
		}

		localStateTimer += Time.deltaTime;

		//dbPrint.setLocate(20, 10);
		//dbPrint.print(state.ToString(), 0.0f);

		switch (state)
		{
	        //ジャンプ.
			case EnemyState.JUMP_PREACTION:
				acceleration = 0.0f;
				velocity = 0.0f;
				if (localStateTimer >= JUMP_PREACTION_DURATION)
				{
					goToJumping();
				}
				break;
			case EnemyState.JUMPING:
				execJump();
				break;

	        //突撃.
			case EnemyState.CHARGE_PREACTION:
				execChargePreaction(Time.deltaTime);
				break;
			case EnemyState.CHARGING:
				execCharge(Time.deltaTime);
				break;
			case EnemyState.CHARGE_FINISHED:
				if (localStateTimer >= FINISH_CHARGE_REACTION_DURATION)
				{
					getBehavior().SendMessage ("NotifyFinishedCharging");
					changeState(EnemyState.MAIN);
				}
				break;

			// クイック攻撃.
			case EnemyState.QUICK_ATTACK:
				this.executeQuickAttack();
				break;

			// あぼーん.
			case EnemyState.VANISH:
				enableSpawnSnortEffects = false;
				break;

			default:
				break;
		}

		updateAnimator();
	}

	override protected void updateAnimator()
	{
		base.updateAnimator();
		if (animator != null)
		{
			animator.SetBool("Motion_Is_Charging", state == EnemyState.CHARGING);
		}
	}

	protected void exec_rotate(float deltaTime, float alpha = 1.0f)
	{
		float		cur_dir   = this.getDirection();
		float		dir_diff  = this.desired_dir - cur_dir;

		if(dir_diff > 180.0f) {
			
			dir_diff = dir_diff - 360.0f;
			
		} else if(dir_diff < -180.0f) {
			
			dir_diff = dir_diff + 360.0f;
		}

		float		angular_velocity = dir_diff >= 0.0f ? angularMaxSpeed : -angularMaxSpeed;
		float		delta_dir = angular_velocity * deltaTime * alpha;

		if (Mathf.Abs(delta_dir) > Mathf.Abs(dir_diff)) {

			cur_dir = desired_dir;
		}
		else {

			cur_dir += delta_dir;
		}

		this.cmdSetDirection(cur_dir);
	}

	//===========================================================================
	//
	//  ジャンプ周り / アニメーション制御で空中に行って、着地する.
	//
	//
	public void PlayJump(float power, float attackRange)
	{
		if (state == EnemyState.MAIN)
		{
			changeState (EnemyState.JUMP_PREACTION);

            rangeAttackArea = attackRange;  //攻撃範囲設定.
            vital.setAttackPower(JUMP_ATTACK_POWER);
		}
	}

	protected void goToJumping()
	{
		animator.SetTrigger("Jump");
		this.cmdEnableCollision(false);

		if(this.state != EnemyState.VANISH) {

			changeState(EnemyState.JUMPING);
		}
	}

	// 完全にアニメーションで動かしているので、特に何もしない.
	protected void execJump()
	{
		// FIXME 各端末で問題が起きた場合は、ルートモーションの位置を同期させる必要があるかも知れません.

		// アニメーターのイベントの通知が来ない？　ことがあるようなので、保険.
		do {

			if(this.animator.IsInTransition(0)) {

				break;
			}
			if(!this.animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Idle")) {

				break;
			}

			this.NotifyLanded();

		} while(false);
	}
	
	// アニメーションからコールされる攻撃判定の発生タイミング.
	public void NotifyImpactLanded()
	{
		this.cmdEnableCollision(true);

		// 当たり判定：範囲円の中にいればダメージとする.
        List<chrBehaviorPlayer> targets = PartyControl.getInstance().getPlayers();
        foreach (var t in targets) {
            Vector3 toTarget = t.transform.position - transform.position;
            if (toTarget.magnitude < rangeAttackArea*this.getScale()) {
                //攻撃範囲内にいるのでダメージ対象とする.
                if (t.isLocal()) {
                    t.control.causeDamage(getBehavior().control.vital.getAttackPower(), -1);
                }
                else {
                    // リモートプレイヤーにはダメージを与えない.
                }
				// 後ろに吹き飛ばす.
				t.beginBlowOut(this.transform.position, 3.0f*this.getScale());
            }
        }

		// 演出用の煙をたくさん出す.
		for (float degree = 0; degree < 360; degree += 30.0f)
		{
			this.createFootSmoke(degree, 10.0f);
		}
	}

	// アニメーションから呼び出される一連のジャンプ行動の終了（通常行動へ移行可能）.
	public void		NotifyLanded()
	{
		Debug.Log("NotifyLanded");
		// ビヘイビアに通知する（AIヒント）.
		getBehavior().SendMessage ("NotifyFinishedJumping");

		if(this.state != EnemyState.VANISH) {

			changeState(EnemyState.MAIN);
		}
	}

	//===========================================================================
	//
	// 突撃周り.
	//
	//

	protected List<chrBehaviorPlayer>	tackled_players = new List<chrBehaviorPlayer>();	// すでにダメージを与えたプレイヤー.

	public void PlayCharge(GameObject target_player, float attack_power)
	{
		if(this.state != EnemyState.VANISH) {

			targetToCharge = target_player;

			changeState (EnemyState.CHARGE_PREACTION);

			footSmokeRespawnTimer = 0.0f;
			enableSnortSpawnEffects();
			createAngryMarkEffect();
	
	        chargeStartPoisition = transform.position;  //突撃開始位置を記憶しておく.
	        vital.setAttackPower(attack_power);
	
			this.tackled_players.Clear();
		}
	}

	// 突撃前の処理. できるだけ突撃対象のキャラの方向を向く.
	protected void execChargePreaction(float deltaTime)
	{
		acceleration = 0.0f;
		velocity = 0.0f;
		
		updateDesiredDir(targetToCharge);
		exec_rotate(deltaTime);
		
		if (localStateTimer >= CHARGE_PREACTION_DURATION)
		{
			goToCharging();
		}
	}
	
	protected void goToCharging()
	{
		acceleration = chargingForce;

		if(this.state != EnemyState.VANISH) {

			changeState(EnemyState.CHARGING);
		}
	}
	
	protected void finishedCharging()
	{
		Destroy (chargeAura);
		chargeAura = null;
		enableSpawnSnortEffects = false;

		if(this.state != EnemyState.VANISH) {

			changeState (EnemyState.CHARGE_FINISHED);
		}
	}

	protected void execCharge(float deltaTime)
	{
        // 一定距離移動しているなら突撃終了とする.
        if ((transform.position - chargeStartPoisition).magnitude > CHARGE_DISTANCE) {
            finishedCharging();
            return;
        }

		// ---------------------------------------------------------------- //
		// 移動（位置座標の補間）.
		
		Vector3		position  = this.getPosition();
		float		rotateAlpha = 1.0f;	// どの程度強力に回頭するか. スピードが乗ると回頭しにくくなる.
		float		currentMaxSpeed = maxSpeed * getMaxSpeedModifier();

		velocity += acceleration * deltaTime;
		velocity = Mathf.Clamp(velocity, 0.0f, maxSpeed * getMaxSpeedModifier());
		float		speed_per_frame = velocity * Time.deltaTime;
		
		Vector3		move_vector = this.transform.forward;
		
		position += move_vector * speed_per_frame * velocity;

		position.y = floorY;

		this.cmdSetPosition(position);

		// スピードが出ていないうちは、ターゲットの方向へ向く.
		rotateAlpha = 1.0f - velocity / currentMaxSpeed;
		updateDesiredDir(targetToCharge);
		exec_rotate(deltaTime, rotateAlpha);
		
		//  演出の処理.

		// 足煙の処理.
		footSmokeRespawnTimer += deltaTime;
		if (footSmokeRespawnTimer >= chargeFootSmokeRespawnDuration)
		{
			footSmokeRespawnTimer = 0.0f;
			// ここのランダムは演出用.
			createFootSmoke(transform.rotation * Quaternion.AngleAxis(Random.Range(-30.0f, 30.0f), Vector3.up) * Vector3.back * 5.0f);
		}

		// FIXME. とりあえず, オーラーは最高速になってから出す.
		if (chargeAura == null && Mathf.Abs (velocity - maxSpeed * getMaxSpeedModifier ()) <= float.Epsilon)
		{
			// オーラの処理.
			chargeAura = EffectRoot.getInstance ().createChargeAura ();
			chargeAura.transform.parent = transform;
			chargeAura.transform.localPosition = new Vector3 (0.0f, 3.6f, 5.0f);
			chargeAura.transform.localRotation = Quaternion.identity;
			
			// 代わりに鼻息はもう出さない.
			enableSpawnSnortEffects = false;
		}

		float	speed_rate = velocity/currentMaxSpeed;

		foreach(var result in this.collision_results) {

			if(result.object1 == null) {

				continue;
			}

			GameObject other = result.object1;

			// ヒットしたプレイヤーにダメージを与える.
			if(other.tag == "Player") {

				chrBehaviorPlayer	player = chrBehaviorBase.getBehaviorFromGameObject<chrBehaviorPlayer>(other);

				if(player == null) {

					continue;
				}
				if(this.tackled_players.Contains(player)) {

					continue;
				}
				if(speed_rate < 0.5f) {

					continue;
				}

				if(player.isLocal()) {

					player.control.causeDamage(this.vital.getAttackPower(), -1);
					player.beginBlowOutSide(this.getPosition(), 3.0f*this.getScale(), this.transform.forward);

				}

				this.tackled_players.Add(player);

				finishedCharging();
				break;
			}
		}
	}

	//===========================================================================
	//
	// クイック攻撃周り.
	//
	//
	public void PlayQuickAttack(GameObject target_player, float attack_power)
	{
		//targetToCharge = target_player;

		changeState(EnemyState.QUICK_ATTACK);
		animator.SetTrigger("QuickAttack");

		//footSmokeRespawnTimer = 0.0f;
		//enableSnortSpawnEffects();
		//createAngryMarkEffect();

       // chargeStartPoisition = transform.position;  //突撃開始位置を記憶しておく.
       vital.setAttackPower(attack_power);
	}

	protected void		executeQuickAttack()
	{
		// アニメーターのイベントの通知が来ない？　ことがあるようなので、保険.
		do {

			if(this.animator.IsInTransition(0)) {

				break;
			}
			if(!this.animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Idle")) {

				break;
			}

			this.NotifyQuickAttack_End();

		} while(false);
	}

	// アニメーションから呼び出されるイベント　クイック攻撃がヒットした瞬間.
	public void 	NotifyQuickAttack_Impact()
	{
		chrBehaviorLocal	player = PartyControl.get().getLocalPlayer();

		if(Vector3.Distance(player.control.getPosition(), this.getPosition()) < 6.0f*this.getScale()) {

			player.control.causeDamage(vital.getAttackPower(), -1);
			player.beginBlowOut(this.getPosition(), 3.0f*this.getScale());
		}
	}

	// アニメーションから呼び出されるイベント　クイック攻撃モーション終わり.
	public void		NotifyQuickAttack_End()
	{
		// ビヘイビアに通知する（AIヒント）.

		chrBehaviorEnemyBoss	behavior = this.behavior as chrBehaviorEnemyBoss;

		if(behavior != null) {

			behavior.NotifyFinishedQuickAttack();
		}

		if(this.state != EnemyState.VANISH) {

			changeState(EnemyState.MAIN);
		}
	}

	//===========================================================================
	//
	// エフェクト.
	//
	//

	// 足煙の生成.
	protected void createFootSmoke(float yawDegree, float speed)
	{
		this.createFootSmoke (Quaternion.AngleAxis (yawDegree, Vector3.up) * Vector3.forward * speed);
	}

	// 足煙の生成.
	protected void createFootSmoke(Vector3 velocity)
	{
		Vector3 effectPosition = transform.position;
		effectPosition.y = floorY;
		createFootSmoke (effectPosition, velocity);
	}

	// 足煙の生成.
	protected void createFootSmoke(Vector3 worldPosition, Vector3 velocity)
	{
		GameObject go;
		go = EffectRoot.get().createBossFootSmokeEffect (worldPosition);
		go.GetComponent<FootSmokeEffectControl>().velocity = velocity;
	}

	protected void enableSnortSpawnEffects()
	{
		enableSpawnSnortEffects = true;
		snortSpawnTimer = snortSpawnIntervalMax;
	}
	
	// 鼻息の生成.
	protected void createSnortEffect()
	{
		if (neckSocket != null && neckEndSocket != null)
		{
			Quaternion effectRot = Quaternion.LookRotation (neckEndSocket.position - neckSocket.position);
			GameObject effect = EffectRoot.getInstance ().createSnortEffect (neckEndSocket.position, effectRot);
			effect.transform.parent = neckEndSocket;
		}
	}

	// 怒りマークの生成.
	protected void createAngryMarkEffect()
	{
		if (headSocket != null)
		{
			GameObject effect = EffectRoot.getInstance ().createAngryMarkEffect(headSocket.position);
			effect.transform.parent = headSocket;
		}
	}

	//===========================================================================
	//
	// ヒット周り.
	//
	//	

    // 壁とのヒット.
	protected void hitWall(Vector3 hitLocation, Vector3 hitNormal)
	{
		if (state == EnemyState.CHARGING)
		{
			finishedCharging();
		}
	}

	
	// ================================================================ //
	// 
	//  ヒューマンプレイヤーではなくAIプレイヤーやコントローラの性能として使うことを想定したキャラ制御メソッド.
	// 
	//

	// target の方向へ向く。ロックオンの攻撃で使用する。.
	protected void updateDesiredDir(GameObject target)
	{
		float turn = 0.0f;

		// targetの座標をローカル座標系で取得.
		Vector3 focal_local_pos = transform.InverseTransformPoint(target.transform.position);
		focal_local_pos.y = 0.0f; // 高さはみない.
		
		// 振り向く角度を決める.
		if (Vector3.Dot(Vector3.right, focal_local_pos) > 0)
		{
			turn = Vector3.Angle(focal_local_pos, Vector3.forward);
		}
		else
		{
			turn = -Vector3.Angle(focal_local_pos, Vector3.forward);
		}

		// move_dir に回転の角度を入れる.
		desired_dir = this.getDirection() + turn;
	}


	// ================================================================ //
	// 
	// ビヘイビアーの使うコマンド.
	// 　ただし、ネットワーク対応のためのスタブになっている.
	//
	
	/**
	 targetName が示すプレイヤーキャラクタに対して直接攻撃（突撃）を行う.
	 @param targetName 突撃対象のプレイヤーキャラクタ名.
	 @param attackPower ダメージにかかる係数.
	 */
	public void cmdBossDirectAttack(string target_account_name, float attack_power)
	{
		do {

			if(this.state != chrControllerEnemyBase.EnemyState.MAIN) {
	
				break;
			}

			chrBehaviorPlayer charge_target_player = PartyControl.getInstance().getPlayerWithAccountName(target_account_name);

			if(charge_target_player == null) {

				// 発生しないはずだが....
				Debug.LogError ("Can't find the player to attack directly.");
				break;
			}

			PlayCharge(charge_target_player.gameObject, attack_power);

		} while(false);
	}

	/**
	 ジャンプ攻撃を行う.
	 @param attackPower 攻撃力.
	 @param attackRange 攻撃判定が届く範囲.
	 */
	public void cmdBossRangeAttack(float power, float attackRange)
	{
		do {

			if(this.state != chrControllerEnemyBase.EnemyState.MAIN) {
	
				break;
			}

			PlayJump(power, attackRange);

		} while(false);
	}


	/**
	 クイック攻撃する.
	 @param targetName 突撃対象のプレイヤーキャラクタ名.
	 @param attackPower ダメージにかかる係数.
	 */
	public void cmdBossQuickAttack(string target_account_name, float attack_power)
	{
		do {

			if(this.state != chrControllerEnemyBase.EnemyState.MAIN) {
	
				break;
			}

			chrBehaviorPlayer charge_target_player = PartyControl.getInstance().getPlayerWithAccountName(target_account_name);

			if(charge_target_player == null) {
	
				// 発生しないはずだが....
				Debug.LogError ("Can't find the player to attack directly.");
				break;
			}

			this.PlayQuickAttack(charge_target_player.gameObject, attack_power);

		} while(false);
	}

    /**
     オブジェクトとのヒット.
     */
	void OnCollisionEnter(Collision other)
	{
		if (other.gameObject.tag == "Wall")
		{
			hitWall(other.contacts[0].point, other.contacts[0].normal);
		}
	}

	void OnCollisionStay(Collision other)
	{
		switch(other.gameObject.tag) {

			case "Player":
			{
				chrBehaviorPlayer	player = other.gameObject.GetComponent<chrBehaviorPlayer>();

				if(player != null) {

					CollisionResult	result = new CollisionResult();
			
					result.object0    = this.gameObject;
					result.object1    = other.gameObject;
					result.is_trigger = false;
	
					this.collision_results.Add(result);
				}
			}
			break;
		}
	}
}
