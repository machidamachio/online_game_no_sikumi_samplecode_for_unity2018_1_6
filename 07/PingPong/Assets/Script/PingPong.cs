// 「フロクのいなりずし」の全体シーケンス、フレーム制御
//
// ■プログラムの説明
// 通信の接続、切断、送受信の定型処理をまとめたクラスです.
// 接続全般の処理を OnGUI()、UpdateReady() 関数で行います.
// ゲームは UpdateGame() 関数でを行っています. 
// ゲーム本体のプログラムは GameControllerクラス で処理していますがゲーム内の通信制御は このクラスで処理しています.
// ゲームで使用するデータの送受信は LateUpdate() 関数内の NetworkController.UpdateSync() 関数で処理しています.
// 切断を検知したら NotifyDisconnection() 関数が呼び出されます.
//

using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;


public class PingPong : MonoBehaviour {
    public GameObject m_serverBarPrefab;
    public GameObject m_clientBarPrefab;
    public GameObject m_gameControllerPrefab;
    public GameObject m_resultControllerPrefab;

	GameMode		m_gameMode;     //シーケンス.
    float			m_timeScale;    //デフォルトのタイムスケールを記憶しておく.

    //ネットワーク.
    string m_hostAddress;
    NetworkController m_networkController = null;


	public enum GameMode {
		Ready = 0,  //接続待ち.        
        Game,       //ゲーム中.
        Result,     //結果表示.
	};


	void Awake()
	{
		m_timeScale = 1;
		Time.timeScale = 0;
	}


	// Use this for initialization
	void Start()
	{
        m_gameMode = GameMode.Ready;

        // ホスト名を取得します.
		m_hostAddress = GetServerIPAddress();
	}
	
	// Update is called once per frame
	void FixedUpdate() {

        switch (m_gameMode) {
		case GameMode.Ready:
			UpdateReady();
			break;

		case GameMode.Game:
			UpdateGame();
			break;

		case GameMode.Result:
			UpdateResult();
			break;
		}

		// フレーム同期を進行してよいかチェックします.
        if (m_networkController!=null && m_networkController.IsSync()) {
            // フレーム同期を進行したのでフラグをクリアします.
            m_networkController.ClearSync();
            
            Time.timeScale = 0; //この周のFixedUpdate関連の更新はfixedDeltaTimｅで更新されるが、2回目以降の呼び出しを防ぐため=0としています.
		}
	}

	void LateUpdate(){
        if (m_networkController != null) {
            if (m_networkController.UpdateSync()) {
				// 停止状態を解除します.
				Resume();
            }
            else {
                // 入力情報を受信していないため次のフレームを処理することができません.
                Suspend();
            }
		}
	}


	// 接続待ち.
	void UpdateReady(){
        //通信接続待ちをしてからゲームを始めます.
        if (m_networkController != null) {
            if (m_networkController.IsConnected() == true) {
                NetworkController.HostType hostType = m_networkController.GetHostType();
                GameStart(hostType == NetworkController.HostType.Server);

                m_gameMode = GameMode.Game;
            }
        }
	}


    // ゲーム中.
    void UpdateGame() {
        GameObject gameController = GameObject.Find(m_gameControllerPrefab.name);
        if (gameController == null) {
            gameController = Instantiate(m_gameControllerPrefab) as GameObject;
            gameController.name = m_gameControllerPrefab.name;
            GameObject.Find("BGM").GetComponent<AudioSource>().Play();    //BGM再生.
            return;
        }

        if (gameController.GetComponent<GameController>().IsEnd()) {
 			m_networkController.SuspendSync();
			if (m_networkController.IsSuspned() == true) {
				m_gameMode = GameMode.Result;
			}
        }
    }
    

	// 結果表示.
	void UpdateResult(){
        //結果表示して勝ち負けを出す.
        GameObject resultController = GameObject.Find(m_resultControllerPrefab.name);
        if (resultController == null) {
            resultController = Instantiate(m_resultControllerPrefab) as GameObject;
            resultController.name = m_resultControllerPrefab.name;
            GameObject.Find("BGM").SendMessage("FadeOut");    //BGMフェードアウト.
            return;
        }
	}

	
	// 一時停止解除.
	public void Resume(){
        Time.timeScale = m_timeScale;
	}

	// 一時停止.
	public void Suspend(){
		Time.timeScale = 0;
	}


	// ゲーム開始.
	void GameStart(bool isServer){
        // バーを生成します.
        GameObject serverBar = Instantiate(m_serverBarPrefab) as GameObject;
        serverBar.GetComponent<BarScript>().SetBarId(0);
        serverBar.name = m_serverBarPrefab.name;
        GameObject clientBar = Instantiate(m_clientBarPrefab) as GameObject;
        clientBar.GetComponent<BarScript>().SetBarId(1);
        clientBar.name = m_clientBarPrefab.name;


        // クライアントの場合は2P用のカメラにします.
        if (isServer == false) {
            Vector3 cameraPos = Camera.main.transform.position;
            cameraPos.y *= -1;
            cameraPos.x *= -1;
            Camera.main.transform.position = cameraPos;

            Vector3 cameraRot = Camera.main.transform.rotation.eulerAngles;
            cameraRot.x *= -1;
            cameraRot.y *= -1;
            cameraRot.z += 180;
            Camera.main.transform.rotation = Quaternion.Euler(cameraRot);

            GameObject light = GameObject.Find("Directional light");
            Vector3 lightRot = light.transform.rotation.eulerAngles;
            lightRot.x *= -1;
            light.transform.rotation = Quaternion.Euler(lightRot);
        }
	}



    void OnGUI() {
        // ボタンが押されたら通信をスタートします.
        if (m_networkController == null) {
            PlayerInfo info = PlayerInfo.GetInstance();

			int x = 50;
			int y = 650;

			// クライアントを選択した時の接続するサーバのアドレスを入力します.
			GUIStyle style = new GUIStyle();
			style.fontSize = 18;
			style.fontStyle = FontStyle.Bold;
			style.normal.textColor = Color.black;
			GUI.Label(new Rect(x, y-25, 200.0f, 50.0f), "対戦相手のIPアドレス", style);
			m_hostAddress = GUI.TextField(new Rect(x, y, 200, 20), m_hostAddress);
			y += 25;

			if (GUI.Button(new Rect(x, y, 150, 20), "対戦相手を待ちます")) {
				// サーバーとして起動します.
				m_networkController = new NetworkController(m_hostAddress, true);
                info.SetPlayerId( 0 );  // プレイヤーIDを設定します.

                GameObject.Find("Title").SetActive(false); // タイトル表示OFF.
            }

			if (GUI.Button(new Rect(x+160, y, 150, 20), "対戦相手と接続します")) {
				// クライアントとして起動します.
				m_networkController = new NetworkController(m_hostAddress, false);
                info.SetPlayerId( 1 );  // プレイヤーIDを設定します.

                GameObject.Find("Title").SetActive(false); // タイトル表示OFF.
            }
        }

        // リザルト終了時はボタンでリセットできるようにします.
        GameObject resultController = GameObject.Find(m_resultControllerPrefab.name);
        if (resultController && resultController.GetComponent<ResultController>().IsEnd()) {
            // 終了ボタンを表示します.
            if (GUI.Button(new Rect(20, Screen.height - 100, 80, 80), "RESET")) {
                SceneManager.LoadScene("PingPong");
				m_networkController.Disconnect();
				m_networkController = null;
            } 
			return;
        }

		// 切断をチェックします.
		if (m_networkController != null &&
			m_networkController.IsConnected() == false &&
		    m_networkController.IsSuspned() == false &&
		    m_networkController.GetSyncState() != NetworkController.SyncState.NotStarted) {
			// 切断しました.
			NotifyDisconnection(); 
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
		
		// 終了ボタンを表示します.  
		if (GUI.Button (new Rect (px, py, sx, sy), message, style)) {
			// ゲームが終了しました.
			SceneManager.LoadScene("PingPong");
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


