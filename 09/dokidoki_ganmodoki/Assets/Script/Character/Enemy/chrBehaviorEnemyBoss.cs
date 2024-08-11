using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 敵ボスの思考ルーチン.
// ホストでのみ動作する.
public class chrBehaviorEnemyBoss : chrBehaviorEnemy {

	private chrControllerEnemyBoss myController; 			// 前提とするコントローラクラスへのキャスト済参照の保持.

	// ---------------------------------------------------------------- //
	
	public enum STEP {
		
		NONE = -1,
		
		MOVE = 0,			// 動く.
		REST,				// 止まってる.
		ACTION,				// アクションの終了待ち.
		VANISH,				// やられた.
		
		NUM,
	};
	
	public STEP			step      = STEP.NONE;
	public STEP			next_step = STEP.NONE;
	public float		step_timer = 0.0f;
	
	// ---------------------------------------------------------------- //
	protected List<chrBehaviorPlayer> targetPlayers;		// 戦う相手たち（アカウント名順に並んでいる）.
	protected int indexOfTargets;							// 現在狙っている対象（targetPlayersのインデックス）.
	protected chrBehaviorPlayer	focus;						// 現在狙っている対象（targetPlayers[indexOfTargets]のキャッシュ）.

	// ---------------------------------------------------------------- //
	// 通信関連.

	protected Network	m_network = null;

	protected bool		m_isHost = false;
	
	// ---------------------------------------------------------------- //
	
	// 3次スプライン補間で使用する点数.
	protected const int	PLOT_NUM = 4;
	
	// 送信回数.
	protected int	m_send_count = 0;
	
	// 現在のプロットのインデックス.
	protected int 	m_plotIndex = 0;
	
	// 停止状態のときはデータを送信しないようにする.
	protected Vector3		m_prev;

	// 間引いた座標を保存.
	protected List<CharacterCoord>	m_culling = new List<CharacterCoord>();
	// 補間した座標を保存.
	protected List<CharacterCoord>	m_plots = new List<CharacterCoord>();

	// ---------------------------------------------------------------- //
	// キャラクタースペック
	// ---------------------------------------------------------------- //
	private float		REST_TIME = 1.0f;
	private float		MOVE_TIME = 3.0f;//5.0f;		// 最低でもこれくらい動く.
	
	// ================================================================ //
	// このコントローラが前提とするコントローラクラスへの参照を返す.
	protected chrControllerEnemyBoss getMyController()
	{
		if (myController == null) {
			myController = this.control as chrControllerEnemyBoss;
		}
		return myController;
	}
	
	// ================================================================ //
	// ビヘイビア側で探知したやられをコントローラにバイパスさせる.
	public override void		onDamaged()
	{
		this.getMyController().causeDamage();
	}

	// やられたことにする.
	public override void		causeVanish()
	{
		this.getMyController().causeVanish();
	}

	// 死亡通知によるやられ
	public void 				dead()
	{
		this.getMyController().life = 0;
		this.getMyController().causeDamage();
	}
	
	// ================================================================ //
	
	public override void	initialize()
	{
		base.initialize();
		initializeTargetPlayers();
	}

	public override void	start()
	{
		this.next_step = STEP.MOVE;

		GameObject go = GameObject.Find("Network");
		if (go != null) {
			m_network = go.GetComponent<Network>();
		}

		m_isHost = (GlobalParam.get().global_account_id == 0)? true : false;
	}
	
	public override	void	execute()
	{
		if (isPaused) {
			return;
		}
		
		// ---------------------------------------------------------------- //
		// ステップ内の経過時間を進める.
		
		this.step_timer += Time.deltaTime;
		
		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.
		
		if(this.next_step == STEP.NONE) {
            //Debug.Log(this.step);
			switch(this.step) {
				
				case STEP.MOVE:
				{
					if(this.step_timer >= MOVE_TIME && this.getMyController().CanBeControlled()) {
						
						this.next_step = STEP.REST;
					}
				}
				break;
				
				case STEP.REST:
				{
					if(this.step_timer >= REST_TIME) {
						decideNextStep();
					}
				}
				break;
			
				default:
					break;
			}
		}
		
		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.
		
		while(this.next_step != STEP.NONE) {
			
			this.step      = this.next_step;
			this.next_step = STEP.NONE;
			switch(this.step) {
			case STEP.REST:
				this.getMyController().acceleration = 0.0f;
				this.getMyController().velocity = 0.0f;
				break;
				
			case STEP.MOVE:
				break;

			default:
				break;
			}
			
			this.step_timer = 0.0f;
		}
		
		// ---------------------------------------------------------------- //
		// 各状態での実行処理.
		
		switch(this.step) {
			
		case STEP.MOVE:
			updateMoving();
			break;
			
		case STEP.VANISH: 
			break;
		}
		
		// ---------------------------------------------------------------- //
	}

	protected void decideNextStep()
	{
		if(m_isHost) {

			// 近くにいる（クイック攻撃できる）プレイヤーを探す.
			chrBehaviorPlayer	target = this.find_close_player();
	
			if(target != null)
			{
				this.next_step = STEP.ACTION;
				EnemyRoot.getInstance().RequestBossQuickAttack(target.getAcountID(), 1.0f);
			} 
			else
			{
	
				// FIXME: 通信に対応させること.
				float randomValue = Random.value * 4;
		
				if (randomValue < 1.0f)
				{
		            Debug.Log("DirectAttack");
					this.next_step = STEP.ACTION;

		            EnemyRoot.getInstance().RequestBossDirectAttack(focus.getAcountID(), 1.0f);
				}
				else if (randomValue < 2.0f)
				{
		            Debug.Log("RangeAttack");
					this.next_step = STEP.ACTION;

					EnemyRoot.getInstance().RequestBossRangeAttack(1.0f, 5.0f);
				}
				else
				{
					this.next_step = STEP.MOVE;
				}
			}

		} else {

			this.next_step = STEP.MOVE;
		}
        
		// ---------------------------------------------------------------- //
		// キャラクターの座標を送る.
		sendCharacterCoordinates();
	}

	// 近くにいる（クイック攻撃できる）プレイヤーを探す.
	protected chrBehaviorPlayer		find_close_player()
	{
		chrBehaviorPlayer	target = null;

		foreach(var result in this.control.collision_results) {

			if(result.object1.tag != "Player") {

				continue;
			}

			chrBehaviorPlayer	player = chrBehaviorBase.getBehaviorFromGameObject<chrBehaviorPlayer>(result.object1);

			if(player == null) {

				continue;
			}

			Vector3		to_player = player.control.getPosition() - this.control.getPosition();

			to_player.Normalize();

			float		pinch = Mathf.Acos(Vector3.Dot(to_player, this.transform.forward))*Mathf.Rad2Deg;

			if(pinch > 90.0f) {

				continue;
			}

			target = player;
			break;
		}

		return(target);
	}

    //範囲攻撃するならtrue.
    private bool rangeAttackEnable(float attackRange) {
        //攻撃範囲に入っているプレーヤーをカウントして判断する.
        int rangeInPlayerCount = 0;
        foreach (chrBehaviorPlayer p in targetPlayers) {
            Vector3 diff = transform.position - p.transform.position;
            if (diff.magnitude < attackRange) {
                rangeInPlayerCount++;       //有効範囲に入ったプレーヤーをカウント.
            }
        }
        
        //有効範囲に何人いるかで攻撃するかどうかを決める.
        int rangeAttackThreshold = Random.Range(0, targetPlayers.Count);    //閾値はランダム.
        if (rangeInPlayerCount > rangeAttackThreshold) {
            Debug.Log("rangeInPlayerCount:" + rangeInPlayerCount);
            return true;
        }
        return false;
    }

    //+-findAngleの範囲で正面のターゲットを検索して返す(index).探すのに失敗したら-1.
    private int findForwardTarget(float findAngle) {
        //自分(正面)とターゲットとの角度で判定する.
        int findIndex = -1;
        
        Vector2 forward = new Vector2( transform.forward.x, transform.forward.z ).normalized;
        for(int i=0; i < targetPlayers.Count; ++i){
            chrBehaviorPlayer p = targetPlayers[i];
            Vector3 diff = p.transform.position - transform.position;
            Vector2 targetVec = new Vector2(diff.x, diff.z).normalized;

            float angle = Vector2.Angle(forward, targetVec);
            //Debug.Log(angle);

            //正面からの角度範囲内だったら見つかったことにする.
            if (angle < findAngle) {
                findAngle = angle;  //一番正面にいるターゲットを見つけるため検索範囲を更新して処理を続ける.
                findIndex = i;
            }
        }
        Debug.Log("findForwardTarget:" + findIndex);
        return findIndex;
    }


	// ---------------------------------------------------------------- //
	// １０フレームに１回、座標をネットに送る.
	private void sendCharacterCoordinates()
	{

		if(m_network == null) {
			
			return;
		}
		
		if(this.step != STEP.MOVE) {

			return;
		}
		
		m_send_count = (m_send_count + 1)%SplineData.SEND_INTERVAL;
		
		if(m_send_count != 0) {
			
			return;
		}
		
		// 通信用座標送信.
		Vector3 target = this.control.getPosition() + Vector3.left;
		CharacterCoord coord = new CharacterCoord(target.x, target.z);
		
		Vector3 diff = m_prev - target;
		if (diff.sqrMagnitude > 0.0001f) {
			
			m_culling.Add(coord);
			
			AccountData	account_data = AccountManager.get().getAccountData(GlobalParam.getInstance().global_account_id);
			
			CharacterRoot.get().SendCharacterCoord(account_data.avator_id, m_plotIndex, m_culling); 
			++m_plotIndex;
			
			if (m_culling.Count >= PLOT_NUM) {
				
				m_culling.RemoveAt(0);
			}
			
			m_prev = target;
		}
	}

	//========================================================================
	// コントローラ側からメッセージングされるアクションの終了通知(AIヒント).

	public void NotifyFinishedCharging()
	{
		// 突進攻撃のターゲットだったインデックスの次のインデックスのキャラにフォーカス.
		indexOfTargets = (indexOfTargets + 1) % targetPlayers.Count;
		focus = targetPlayers[indexOfTargets];

		this.next_step = STEP.MOVE;
	}

	public void NotifyFinishedJumping()
	{
		indexOfTargets = (indexOfTargets + 1) % targetPlayers.Count;
		focus = targetPlayers[indexOfTargets];

		this.next_step = STEP.MOVE;
	}

	public void NotifyFinishedQuickAttack()
	{
		indexOfTargets = (indexOfTargets + 1) % targetPlayers.Count;
		focus = targetPlayers[indexOfTargets];

		this.next_step = STEP.MOVE;
	}

	public void NotifyFinishedTyphoon()
	{
		this.next_step = STEP.MOVE;
	}

	public void NotifyDied()
	{
		this.next_step = STEP.VANISH;
	}

	//========================================================================
	// AIによるパッド操作.

	// 目標に正面を向ける.
	protected void updateMoving()
	{
		float turn;

		Vector3 focal_local_pos = this.control.transform.InverseTransformPoint(focus.transform.position);
		focal_local_pos.y = 0.0f; // 高さはみない.

		if(!m_isHost && m_plots.Count > 0) {
			CharacterCoord coord = m_plots[0];
			focal_local_pos = new Vector3(coord.x, focal_local_pos.y, coord.z);
			if (m_plots.Count > 0) {
				m_plots.RemoveAt(0);
			}
		}

		//float	turn = Random.Range(-90.0f, 90.0f);
		if (Vector3.Dot(Vector3.right, focal_local_pos) > 0)
		{
			turn = Vector3.Angle(focal_local_pos, Vector3.forward);
		}
		else
		{
			turn = -Vector3.Angle(focal_local_pos, Vector3.forward);
		}
		turn = Mathf.Clamp(turn, -90, 90);
		
		// move_dir に回転の角度を入れる.
		this.getMyController().SetMoveDirection(this.control.getDirection() + turn);
		this.getMyController().acceleration = this.getMyController().maxSpeed;
	}

	//========================================================================

	// このボスが戦っているプレイヤーのリストを取得する.
	protected void initializeTargetPlayers()
	{
		// アカウント名でABC順にソートしておく.
		targetPlayers = new List<chrBehaviorPlayer>(PartyControl.getInstance().getPlayers());
		targetPlayers.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
		
		indexOfTargets = 0;
		
		// 狙うプレイヤーを決定する.  ABCソートの先頭アカウント名を持つプレイヤーが最初の標的になる.
		focus = targetPlayers[indexOfTargets];
	}

	// [開発用] 最新のパーティ情報を取得してプレイヤーのリストを更新する。.
	public virtual void updateTargetPlayers()
	{
		// 本番のゲームでは呼び出されるべきではないのもので、開発用サブクラスで実装する.
	}

	// ================================================================ //
	// 
	// ゲスト端末でのボスの移動.
	//
	
	public void CalcCoordinates(int index, CharacterCoord[] data)
	{
		// 受信した座標を保存.
		do {
			
			// データーが空っぽ（念のため）.
			if(data.Length <= 0) {
				
				break;
			}
			
			// 新しいデーターがない.
			if(index <= m_plotIndex) {
				
				break;
			}
			
			// m_plotIndex ... m_culling[] の最後の頂点のインデックス.
			// index       ... data[] の最後の頂点のインデックス.
			//
			// index - m_plotIndex ... 今回新たに追加された頂点の数.
			//
			int		s = data.Length - 1 - (index - m_plotIndex);
			
			if(s < 0) {
				
				break;
			}
			
			for(int i = s;i < data.Length;i++) {
				
				m_culling.Add(data[i]);
			}
			
			// m_culling[] の最後の頂点のインデックス.
			m_plotIndex = index;
			
			// スプライン曲線を求めて補間する.	
			SplineData	spline = new SplineData();
			spline.CalcSpline(m_culling);
			
			// 求めたスプライン補間を座標情報として保存する.
			CharacterCoord plot = new CharacterCoord();
			for (int i = 0; i < spline.GetPlotNum(); ++i) {
				spline.GetPoint(i, out plot);
				m_plots.Add(plot);
			}
			
			// 一番古い座標を削除.
			if (m_culling.Count > PLOT_NUM) {
				
				m_culling.RemoveRange(0, m_culling.Count - PLOT_NUM);
			}
			
		} while(false);
		

		// 受信した座標を保存.
		for (int i = 0; i < data.Length; ++i) {
			int p = index - PLOT_NUM - i + 1;
			if (p < m_plotIndex) {
				m_culling.Add(data[i]);
			}
		}
	}

}
