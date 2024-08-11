// ゲームのシーケンス制御
//
// ■プログラムの説明
// 通信の接続、切断、送受信の定型処理をまとめたクラスです.
// 接続全般のシーケンス処理を OnGUI() 関数で行います.
// 対戦相手との接続が完了するとジャンケンの選択に移行します.
// ジャンケンの選択は UpdateSelectRPS() 関数で行い、選択されると NetworkController クラスを使用して送信します. 
// ジャンケンの選択後は対戦相手の通信を待ちます.
// 対戦相手の通信を待ち UpdateWaitRPS() 関数で行います.
// ジャンケン後のアクションの選択は UpdateAction() 関数で行われます.
// アクションを選択したら UpdateAction() 関数内で NetworkController.SendActionData() 関数で送信します. 
// 対戦相手のアクションデータの受信も UpdateAction() 関数内で行っています.
// ゲームが終了したら DisconnectNetwork() 関数を呼び出して切断します.
// EventCallback() 関数をイベントハンドラーとして登録して対戦相手が切断時したときのイベントを処理します.
//

using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

public class RockPaperScissors : MonoBehaviour {
    public GameObject m_serverPlayerPrefab; //サーバー側のプレーヤーキャラ.
    public GameObject m_clientPlayerPrefab; //クライアント側のプレーヤーキャラ.

    public GameObject m_RPSSelectorPrefab;  //ジャンケン選択.
    public GameObject m_shootCallPrefab;    //ジャンケンポン！の掛け声演出用.
    public GameObject m_battleSelectPrefab; //攻守選択.
    public GameObject m_actionControllerPrefab; //戦闘演出.
    public GameObject m_resultScenePrefab;  //リザルト表示.

    public GameObject m_finalResultWinPrefab;   //最終結果 勝ち.
    public GameObject m_finalResultLosePrefab;  //最終結果 負け.

    const int PLAYER_NUM = 2;
	const int PLAY_MAX = 3;
    GameObject m_serverPlayer;  //よく使うので捕まえておく.
    GameObject m_clientPlayer;  //よく使うので捕まえておく.

    GameState m_gameState = GameState.None;
	InputData[]         m_inputData = new InputData[PLAYER_NUM];
    NetworkController   m_networkController = null;
    string              m_serverAddress;

    int 				m_playerId = 0;
	int[]				m_score = new int[PLAYER_NUM];
    Winner              m_actionWinner = Winner.None;
	
	bool				m_isGameOver = false;

    //攻守の送受信待ち用.
    float m_timer;
    bool m_isSendAction;
    bool m_isReceiveAction;

	
	// ゲーム進行状況.
	enum GameState
	{
		None = 0,
		Ready,      //対戦相手のログイン待ち.
		SelectRPS,  //ジャンケン選択.
		WaitRPS,    //受信待ち.
		Shoot,      //ジャンケン演出.
		Action,     //叩いて被っての選択・受信待ち.
		EndAction,  //叩いて被って演出.
		Result,     //結果発表.
		EndGame,    //おわり.
		Disconnect,	//エラー.
	};

		
	// Use this for initialization
	void Start () {
        ////ResultChecker.WinnerTest();

        m_serverPlayer = null;
        m_clientPlayer = null;

        m_timer = 0;
        m_isSendAction = false;
        m_isReceiveAction = false;

		// 初期化します.
        for (int i = 0; i < m_inputData.Length; ++i) {
            m_inputData[i].rpsKind = RPSKind.None;
            m_inputData[i].attackInfo.actionKind = ActionKind.None;
            m_inputData[i].attackInfo.actionTime = 0.0f;
        }

		// まだ動作させません.
		m_gameState = GameState.None;

		
		for (int i = 0; i < m_score.Length; ++i) {
			m_score[i] = 0;	
		}

		// 通信モジュールを作成します.
		GameObject go = new GameObject("Network");
		if (go != null) {
			TransportTCP transport = go.AddComponent<TransportTCP>();
			if (transport != null) {
				transport.RegisterEventHandler(EventCallback);
			}
			DontDestroyOnLoad(go);
		}

        // ホスト名を取得します.
		m_serverAddress = GetServerIPAddress();	
	}
	
	// Update is called once per frame
	void Update () {
	
		switch (m_gameState) {
		case GameState.None:
			break;

		case GameState.Ready:
			UpdateReady();
			break;

		case GameState.SelectRPS:
			UpdateSelectRPS();
			break;

		case GameState.WaitRPS:
			UpdateWaitRPS();
			break;

		case GameState.Shoot:
			UpdateShoot();
			break;

		case GameState.Action:
			UpdateAction();
			break;

		case GameState.EndAction:
			UpdateEndAction();
			break;

		case GameState.Result:
			UpdateResult();
			break;

		case GameState.EndGame:
			UpdateEndGame();
			break;

		case GameState.Disconnect:
			break;
		}
	}
	
	void OnGUI() {
		switch (m_gameState) {
		case GameState.EndGame:
			OnGUIEndGame();
			break;

		case GameState.Disconnect:
			NotifyDisconnection();
			break;
		}

		float px = Screen.width * 0.5f - 100.0f;
		float py = Screen.height * 0.75f;
		
		//未接続のときのGUIを表示します.
        if (m_networkController == null) {
			if (GUI.Button(new Rect(px, py, 200, 30), "対戦相手を待ちます")) {
                m_networkController = new NetworkController();  //サーバー.
                m_playerId = 0;
                m_gameState = GameState.Ready;
                m_isGameOver = false;

                //プレイヤー生成.
                m_serverPlayer = Instantiate(m_serverPlayerPrefab) as GameObject;
                m_serverPlayer.name = m_serverPlayerPrefab.name;

                GameObject.Find("BGM").GetComponent<AudioSource>().Play(); //BGM.
                GameObject.Find("Title").SetActive(false); //タイトル表示OFF.
            }

            // クライアントを選択した時の接続するサーバのアドレスを入力します.
			Rect labelRect = new Rect(px, py + 80, 200, 30);
			GUIStyle style = new GUIStyle();
			style.fontStyle = FontStyle.Bold;
			style.normal.textColor = Color.white;
			GUI.Label(labelRect, "あいてのIPあどれす", style);
			labelRect.y -= 2;
			style.fontStyle = FontStyle.Normal;
			style.normal.textColor = Color.black;
            GUI.Label(labelRect, "あいてのIPあどれす", style);
			m_serverAddress = GUI.TextField(new Rect(px, py + 95, 200, 30), m_serverAddress);

			if (GUI.Button(new Rect(px, py + 40, 200, 30), "対戦相手と接続します")) {
                m_networkController = new NetworkController(m_serverAddress);  //サーバー.
                m_playerId = 1;
                m_gameState = GameState.Ready;
                m_isGameOver = false;

                //プレイヤーを生成します.
                m_clientPlayer = Instantiate(m_clientPlayerPrefab) as GameObject;
                m_clientPlayer.name = m_clientPlayerPrefab.name;
                //プレイヤー表記の位置を調整します.
                GameObject board = GameObject.Find("BoardYou");
                Vector3 pos = board.transform.position;
                pos.x *= -1;
                board.transform.position = pos;

                GameObject.Find("BGM").GetComponent<AudioSource>().Play(); //BGM.
                GameObject.Find("Title").SetActive(false); //タイトル表示OFF.
            }
        }
	}
		
	
    // 接続待ち.
	void UpdateReady()
	{        
        // 接続しているか確認します.
        if (m_networkController.IsConnected() == false) {
            return;
        }

        //プレイヤーキャラが作られてないなら生成します.
        if (m_serverPlayer == null) {
            m_serverPlayer = Instantiate(m_serverPlayerPrefab) as GameObject;
            m_serverPlayer.name = m_serverPlayerPrefab.name;
        }
        if (m_clientPlayer == null) {
            m_clientPlayer = Instantiate(m_clientPlayerPrefab) as GameObject;
            m_clientPlayer.name = m_clientPlayerPrefab.name;
        }
        
        // モーションがIdleになるまで待機します.
        if (m_serverPlayer.GetComponent<Player>().IsIdleAnimation() == false) {
            return;
        }
        if (m_clientPlayer.GetComponent<Player>().IsIdleAnimation() == false) {
            return;
        }
        
        // プレイヤー提示が終わるまで待機します.
        GameObject board = GameObject.Find("BoardYou");
        if (board == null) {
            return;
        }
        if (board.GetComponent<BoardYou>().IsEnd() == false) {
            return;
        }

        // 全ての待ちを通過したので次の状態へ遷移します.
        board.GetComponent<BoardYou>().Sleep(); //表記OFF.
        m_gameState = GameState.SelectRPS;
	}


    // ジャンケンの選択.
	void UpdateSelectRPS()
	{
        GameObject obj = GameObject.Find("RPSSelector");
        if (obj == null) {
            // 演出用のオブジェクトがないなら生成します(このシーケンスの初期動作です).
            obj = Instantiate(m_RPSSelectorPrefab) as GameObject;
            obj.name = "RPSSelector";
            return;
        }

        RPSKind rps = obj.GetComponent<RPSSelector>().GetRPSKind();
        if (rps != RPSKind.None) {
            m_inputData[m_playerId].rpsKind = rps;

            // 送信.
			m_networkController.SendRPSData(m_inputData[m_playerId].rpsKind);
            m_gameState = GameState.WaitRPS;
        }
	}

    // ジャンケン選択の通信待ち.
	void UpdateWaitRPS()
	{
        // 受信待ち.
		RPSKind rps = m_networkController.ReceiveRPSData();
		if(rps == RPSKind.None) {
			// まだ受信していません.
			return;
		}
		m_inputData[m_playerId ^ 1].rpsKind = rps;

        m_serverPlayer.GetComponent<Player>().SetRPS(m_inputData[0].rpsKind, m_inputData[1].rpsKind);
        m_clientPlayer.GetComponent<Player>().SetRPS(m_inputData[1].rpsKind, m_inputData[0].rpsKind);

		m_gameState = GameState.Shoot;

        // 演出は用済みなので消去します.
        GameObject obj = GameObject.Find("RPSSelector");
        Destroy(obj);
	}

    // ジャン、ケン、ポンの演出.
	void UpdateShoot()
	{
        GameObject obj = GameObject.Find("ShootCall");
        if (obj == null) {
            // 演出用のオブジェクトがないなら生成します(このシーケンスの初期動作です).
            obj = Instantiate(m_shootCallPrefab) as GameObject;
            obj.name = "ShootCall";
            return;
        }

        // ジャンケンポンの掛け声が終わるまで待ちます.
        ShootCall sc = obj.GetComponent<ShootCall>();
        if (sc.IsEnd()) {
            Destroy(obj);
            m_gameState = GameState.Action;
        }
	}

    // 攻守の選択.
	void UpdateAction()
	{
        GameObject obj = GameObject.Find("BattleSelect");
        if (obj == null) {
            //演出用のオブジェクトがないなら生成します(このシーケンスの初期動作です).
            obj = Instantiate(m_battleSelectPrefab) as GameObject;
            obj.name = "BattleSelect";

            //選択したジャンケンの手を渡します.
            obj.GetComponent<BattleSelect>().Setup(m_inputData[0].rpsKind, m_inputData[1].rpsKind);

            // 変数初期化.
            m_timer = Time.time;
            m_isSendAction = false;
            m_isReceiveAction = false;
            return;
        }

        // 選択終了を待ちます.
        BattleSelect battleSelect = obj.GetComponent<BattleSelect>();
        if (battleSelect.IsEnd() && m_isSendAction == false) {
            // 時間と行動選択を取得します.
            float time = battleSelect.GetTime();
            ActionKind action = battleSelect.GetActionKind();

            m_inputData[m_playerId].attackInfo.actionKind = action;
            m_inputData[m_playerId].attackInfo.actionTime = time;


            // 相手に送信します.
			m_networkController.SendActionData(action, time);

            // アニメーション(自分).
            GameObject player = (m_playerId == 0) ? m_serverPlayer : m_clientPlayer;
            player.GetComponent<Player>().ChangeAnimationAction(action);
            
            m_isSendAction = true;          //送信成功.
        }

        // 攻守受信待ち.
        if (m_isReceiveAction == false) {
            // 受信チェック:相手の攻撃/防御をチェックします.
			bool isReceived = m_networkController.ReceiveActionData(ref m_inputData[m_playerId ^ 1].attackInfo.actionKind,
			                                                        ref m_inputData[m_playerId ^ 1].attackInfo.actionTime);

            if (isReceived) {
                // アニメーション(対戦相手).
                ActionKind action = m_inputData[m_playerId ^ 1].attackInfo.actionKind;
                GameObject player = (m_playerId == 1) ? m_serverPlayer : m_clientPlayer;
                player.GetComponent<Player>().ChangeAnimationAction(action);

                m_isReceiveAction = true;   //受信成功.
            }
            else if (Time.time - m_timer > 5.0f) {
                // 時間切れなので相手の入力はデフォルトの設定にします.
                m_inputData[m_playerId ^ 1].attackInfo.actionKind = ActionKind.None;
                m_inputData[m_playerId ^ 1].attackInfo.actionTime = 5.0f;
                m_isReceiveAction = true;   // 時間切れなので受信成功にします.
            }
        }

        // 進んでいいかチェックします.
        if (m_isSendAction == true && m_isReceiveAction == true) {
            // 通信時に行う変換処理で値の精度が落ちるため、ここで値の精度をあわせます.
            float time = m_inputData[m_playerId].attackInfo.actionTime;
            short actTime = (short)(time * 1000.0f);
            m_inputData[m_playerId].attackInfo.actionTime = actTime / 1000.0f;

            m_gameState = GameState.EndAction;


            Debug.Log("Own Action:" + m_inputData[m_playerId].attackInfo.actionKind.ToString() +
                      ",  Time:" + m_inputData[m_playerId].attackInfo.actionTime);
            Debug.Log("Opponent Action:" + m_inputData[m_playerId^1].attackInfo.actionKind.ToString() +
                      ",  Time:" + m_inputData[m_playerId^1].attackInfo.actionTime);
        }

    }


    // 攻撃・防御のアニメーション演出待ち.
	void UpdateEndAction()
	{
        GameObject obj = GameObject.Find("ActionController");
        if (obj == null) {
            // 演出用のオブジェクトがないなら生成します(このシーケンスの初期動作です).
            obj = Instantiate(m_actionControllerPrefab) as GameObject;
            obj.name = "ActionController";

            // 演出のために勝敗判定をします.
            InputData serverPlayer = m_inputData[0];
            InputData clientPlayer = m_inputData[1];
            
            // ジャンケン判定.
            Winner rpsWinner = ResultChecker.GetRPSWinner(serverPlayer.rpsKind, clientPlayer.rpsKind);
            // 攻防早押し判定.
            m_actionWinner = ResultChecker.GetActionWinner(serverPlayer.attackInfo, clientPlayer.attackInfo, rpsWinner);
            Debug.Log("RPS Winner:" + rpsWinner.ToString() + " ActionWinner" + m_actionWinner.ToString());

            // 演出開始.
            obj.GetComponent<ActionController>().Setup(
                m_actionWinner, m_score[0], m_score[1]
            );
            return;
        }

        // 戦闘演出が終わるのを待ちます.
        ActionController actionController = obj.GetComponent<ActionController>();
        if (actionController.IsEnd()) {
            // 後始末をします.
            Destroy(GameObject.Find("BattleSelect"));       // 攻守選択の表示物を消します.
            Destroy(GameObject.Find("ActionController"));   // 演出制御を消去します.

            m_gameState = GameState.Result;
        }
	}

	// 結果算出.
	void UpdateResult()
	{
        GameObject obj = GameObject.Find("ResultScene");
        if (obj == null) {
            // 演出用のオブジェクトがないなら生成します(このシーケンスの初期動作です).
            obj = Instantiate(m_resultScenePrefab) as GameObject;
            obj.name = "ResultScene";

            // 得点判定の前に以前の得点を記憶しておきます.
            int[] prevScores = { m_score[0], m_score[1] };
            // 勝った方にポイントが入ります.
            if (m_actionWinner == Winner.ServerPlayer) {
                ++m_score[0];
            }
            else if (m_actionWinner == Winner.ClientPlayer) {
                ++m_score[1];
            }
            
            // 自分の勝敗を求めます.
            ResultScene.Result ownResult = ResultScene.Result.Lose;
            if (m_actionWinner == Winner.Draw) {
                ownResult = ResultScene.Result.Draw;
            }
            else if (m_playerId == 0 && m_actionWinner == Winner.ServerPlayer) {
                ownResult = ResultScene.Result.Win;
            }
            else if (m_playerId == 1 && m_actionWinner == Winner.ClientPlayer) {
                ownResult = ResultScene.Result.Win;
            }

            // 演出を開始します.
            obj.GetComponent<ResultScene>().Setup(prevScores, m_score, ownResult);
            return;
        }

        // 演出を待ちます.
        ResultScene resultScene = obj.GetComponent<ResultScene>();
        if (resultScene.IsEnd()) {
            Debug.Log("result end");
            if (m_score[0] == PLAY_MAX || m_score[1] == PLAY_MAX) {
                // すべての対戦が終わったた場合はゲーム終了です.
                GameObject.Find("BGM").SendMessage("FadeOut"); //BGM.
                m_gameState = GameState.EndGame;
            }
            else {
                // 次の対戦に進みます.
                Reset();
                m_gameState = GameState.Ready;
            }

            // 後始末をします.
            Destroy(obj);
        }
	}

	// ゲーム終了(結果表示準備).
	void UpdateEndGame()
	{
        GameObject obj = GameObject.Find("FinalResult");
        if (obj == null) {
            // 勝ち負けに応じて結果を表示させます.
            if (m_score[m_playerId] > m_score[m_playerId ^ 1]) {
                obj = Instantiate(m_finalResultWinPrefab) as GameObject;    // 勝ち.
                obj.name = "FinalResult";
            }
            else {
                obj = Instantiate(m_finalResultLosePrefab) as GameObject;   // 負け.
                obj.name = "FinalResult";
            }
        }
	}

	// ゲームリセット.
	void Reset()
	{	
        // 入力を初期化します.
		for (int i = 0; i < m_inputData.Length; ++i) {
			m_inputData[i].rpsKind = RPSKind.None;
			m_inputData[i].attackInfo.actionKind = ActionKind.None;
            m_inputData[i].attackInfo.actionTime = 0.0f;
		}

        // キャラクターの状態をリセットします.
        Destroy(m_serverPlayer);
        Destroy(m_clientPlayer);

        m_serverPlayer = Instantiate(m_serverPlayerPrefab) as GameObject;
        m_clientPlayer = Instantiate(m_clientPlayerPrefab) as GameObject;
        m_serverPlayer.name = "Daizuya";
        m_clientPlayer.name = "Toufuya";
        m_serverPlayer.GetComponent<Player>().ChangeAnimation(Player.Motion.Idle);
        m_clientPlayer.GetComponent<Player>().ChangeAnimation(Player.Motion.Idle);
        // プレイヤーの表記を出します.
        GameObject board = GameObject.Find("BoardYou");
        board.GetComponent<BoardYou>().Run();
	}
	
	// ゲーム終了後の表示.
	void OnGUIEndGame()
	{			
		// 終了ボタン表示します.
        GameObject obj = GameObject.Find("FinalResult");
        if (obj == null) { return; }

        Animation anim = obj.GetComponent<Animation>();
        if (anim.isPlaying) { return; }
        

        Rect r = new Rect(Screen.width / 2 - 50, Screen.height -60, 100, 50);
        if (GUI.Button(r, "RESET")) {
			DisconnectNetwork();
            SceneManager.LoadScene("RockPaperScissors");
        }       
	}

	// 切断処理.
	void DisconnectNetwork() 
	{
		if (m_networkController != null) {
			m_networkController.Disconnect();
			m_networkController = null;
		}

	}
	
	// ゲーム終了チェック.
	public bool IsGameOver()
	{
		return m_isGameOver;
	}

	// イベントハンドラー.
	public void EventCallback(NetEventState state)
	{
		switch (state.type) {
		case NetEventType.Disconnect:
			if (m_gameState < GameState.EndGame && m_isGameOver == false) {
				m_gameState = GameState.Disconnect;
			}
			break;
		}
	}

	// 切断通知.
	void NotifyDisconnection()
	{
		GUISkin skin = GUI.skin;
		GUIStyle style = new GUIStyle(GUI.skin.GetStyle("button"));
		style.normal.textColor = Color.white;
		style.fontSize = 25;
		
		float sx = 450;
		float sy = 200;
		float px = Screen.width / 2 - sx * 0.5f;
		float py = Screen.height / 2 - sy * 0.5f;
		
		string message = "回線が切断しました.\n\nぼたんをおしてね.";
		if (GUI.Button (new Rect (px, py, sx, sy), message, style)) {
			// ゲームが終了しました.
			m_isGameOver = true;
			SceneManager.LoadScene("RockPaperScissors");
		}
	}

	// 端末のIPアドレスを取得.
	public string GetServerIPAddress() {

		string hostAddress = "";
		string hostname = Dns.GetHostName();

		// ホスト名からIPアドレスを取得します.
		IPAddress[] adrList = Dns.GetHostAddresses(hostname);

		for (int i = 0; i < adrList.Length; ++i) {
			string addr = adrList[i].ToString();
			string [] c = addr.Split('.');

			if (c.Length == 4) {
				hostAddress = addr;
				break;
			}
		}

		return hostAddress;
	}

}
