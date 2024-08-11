// 三目並べのシーケンス制御および通信制御
//
// ■プログラムの説明
// 自分のターンの時の処理を DoOwnTurn() 関数で行います.
// 配置可能な場所にマークが設置されたら対戦へ Send() 関数で送信します.
// 相手のターンの時の処理を DoOppnentTurn() 関数で行います. 
// 対戦相手がマークを設置してデータを送信するまで Receive() 関数の関数値をチェックして待ちます.
// 自分もしくは相手のターンが終了したときに UpdateTurn() 関数で勝敗のチェックを行いゲームが終了していたら Sequence.cs にゲーム終了へ制御を移します.
// EventCallback() 関数をイベントハンドラーとして登録して対戦相手が切断したときのイベントを処理します.
//

using UnityEngine;
using System;
using System.Collections;

public class TicTacToe : MonoBehaviour {
	
	// ゲーム進行状況.
	private enum GameProgress {
		None = 0,		// 試合開始前
		Ready,			// 試合開始合図表示.
		Turn,			// 試合中.
		Result,			// 結果表示.
		GameOver,		// ゲーム終了.
		Disconnect,		// 通信切断.
	};
	
	// ターン種別.
	private enum Turn {
		Own = 0,		// 自分のターン.
		Opponent,		// 相手のターン.
	};

	// マーク.
	private enum Mark {
		Circle = 0,		// ○.
		Cross,			// ×.
	};
	
	// 試合の結果.
	private enum Winner {
		None = 0,		// 試合中.
		Circle,			// ○勝利.
		Cross,			// ×勝利.
		Tie,			// 引き分け.
	};
	
	// マス目の数.
	private const int 		rowNum = 3;

	// 試合開始前の合図表示時間.
	private const float		waitTime = 1.0f;

	// 手持ち時間.
	private const float		turnTime = 10.0f;
	
	// 配置されたマークを保存.
	private int[]			spaces = new int[rowNum*rowNum];
	
	// 進行状況.
	private	GameProgress	progress;
	
	// 現在のターン.
	private Mark			turn;

	// ローカルのマーク.
	private Mark			localMark;

	// リモートのマーク.
	private Mark			remoteMark;

	// 残り時間.
	private float			timer;

	// 勝者.
	private Winner			winner;
	
	// ゲーム終了フラグ.
	private bool			isGameOver;

	// 待ち時間.
	private float			currentTime;
	
	// ネットワーク.
	private TransportTCP 	m_transport = null;

	// カウンタ.
	private float			step_count = 0.0f;

	//
	// テクスチャ関連.
	//

	// ○テクスチャ.
	public GUITexture		circleTexture;
	
	// ×テクスチャ.
	public GUITexture		crossTexture;
	
	// 盤のテクスチャ.
	public GUITexture		fieldTexture;

	// "あなた"のテクスチャ.
	public GUITexture		youTexture;

	// "勝ち"のテクスチャ.
	public GUITexture		winTexture;

	// "負け"のテクスチャ.
	public GUITexture		loseTexture;

    // サウンド.
    public AudioClip se_click;
    public AudioClip se_setMark;
    public AudioClip se_win;

	private static float SPACES_WIDTH = 400.0f;
	private static float SPACES_HEIGHT = 400.0f;

	private static float WINDOW_WIDTH = 640.0f;
	private static float WINDOW_HEIGHT = 480.0f;

	// Use this for initialization
	void Start () {
		
		// Networkクラスのコンポーネントを取得します.
		GameObject obj = GameObject.Find("Network");
		m_transport  = obj.GetComponent<TransportTCP>();
		if (m_transport != null) {
			m_transport.RegisterEventHandler(EventCallback);
		}

		// ゲームを初期化します.
		Reset();
		isGameOver = false;
		timer = turnTime;
	}
	
	// Update is called once per frame
	void Update()
	{
 		switch (progress) {
		case GameProgress.Ready:
			UpdateReady();
			break;

		case GameProgress.Turn:
			UpdateTurn();
			break;
			
		case GameProgress.GameOver:
			UpdateGameOver();
			break;			
		}
	}
	 
	void OnGUI()
	{
		switch (progress) {
		case GameProgress.Ready:
			// フィールドとマークを描画します.
			DrawFieldAndMarks();
			break;

		case GameProgress.Turn:
			// フィールドとマークを描画します.
			DrawFieldAndMarks();
			// 残り時間を描画します.
			if (turn == localMark) {
				DrawTime();
			}
			break;
			
		case GameProgress.Result:
			// フィールドとマークを描画します.
			DrawFieldAndMarks();
			// 勝者を表示します.
			DrawWinner();
			// 終了ボタン表示します.
			{
				GUISkin skin = GUI.skin;
				GUIStyle style = new GUIStyle(GUI.skin.GetStyle("button"));
				style.normal.textColor = Color.white;
				style.fontSize = 25;

				if (GUI.Button(new Rect(Screen.width/2-100, Screen.height/2, 200, 100), "おわり", style)) {
					progress = GameProgress.GameOver;
					step_count = 0.0f;
				}
			}
			break;

		case GameProgress.GameOver:
			// フィールドとマークを描画します.
			DrawFieldAndMarks();
			// 勝者を表示します.
			DrawWinner();
			break;

		case GameProgress.Disconnect:
			// フィールドとマークを描画します.
			DrawFieldAndMarks();
			// 切断通知をします.
			NotifyDisconnection();
			break;

		default:
			break;
		}

	}

	// ゲーム準備.
	void UpdateReady()
	{
		// 試合開始の合図の表示を待ちます.
		currentTime += Time.deltaTime;

		if (currentTime > waitTime) {
            //BGM再生開始します.
            GameObject bgm = GameObject.Find("BGM");
            bgm.GetComponent<AudioSource>().Play();

			// 表示が終わったらゲーム開始です.
			progress = GameProgress.Turn;
		}
	}

	// ターンの処理.
	void UpdateTurn()
	{
		bool setMark = false;

		if (turn == localMark) {
			setMark = DoOwnTurn();

            // 置けない場所を押されたときは、クリック用のSEを鳴らします.
            if (setMark == false && Input.GetMouseButtonDown(0)) {
                AudioSource audio = GetComponent<AudioSource>();
                audio.clip = se_click;
                audio.Play();
            }
		}
		else {
			setMark = DoOppnentTurn();

            // 置けないときに押されたときは、クリック用のSEを鳴らします.
            if (Input.GetMouseButtonDown(0)) {
                AudioSource audio = GetComponent<AudioSource>();
                audio.clip = se_click;
                audio.Play();
            }
		}

		if (setMark == false) {
			// 置き場を検討中です.	
			return;
		}
        else {
            // マークが置かれたSEを鳴らします.
            AudioSource audio = GetComponent<AudioSource>();
            audio.clip = se_setMark;
            audio.Play();
        }
		
		// マークの並びをチェックします.
		winner = CheckInPlacingMarks();
		if (winner != Winner.None) {
            //勝ちの場合はSEを鳴らします.
            if ((winner == Winner.Circle && localMark == Mark.Circle)
                || (winner == Winner.Cross && localMark == Mark.Cross)) {
                AudioSource audio = GetComponent<AudioSource>();
                audio.clip = se_win;
                audio.Play();
            }
            // BGMの再生を終了します.
            GameObject bgm = GameObject.Find("BGM");
            bgm.GetComponent<AudioSource>().Stop();

			// ゲーム終了です.
			progress = GameProgress.Result;			
		}
		
		// ターンを更新します.
		turn = (turn == Mark.Circle)? Mark.Cross : Mark.Circle; 
		timer = turnTime;
	}
	
	// ゲーム終了顔の処理.
	void UpdateGameOver()
	{
		step_count += Time.deltaTime;
		if (step_count > 1.0f) {
			// ゲームが終了しました.
			Reset();
			isGameOver = true;
		}
	}

	// 自分のターンの時の処理.
	bool DoOwnTurn()
	{
		int index = 0;

		timer -= Time.deltaTime;
		if (timer <= 0.0f) {
			// 時間切れ.
			timer = 0.0f;
			do {
				index = UnityEngine.Random.Range(0, 8);
			} while (spaces[index] != -1);
		}
		else {
			// マウスの左ボタンの押下状態を監視します.
			bool isClicked = Input.GetMouseButtonDown(0);
			if (isClicked == false) {
				// 押されていないのでなにもしません.
				return false;
			}
			
			Vector3 pos = Input.mousePosition;
			Debug.Log("POS:" + pos.x + ", " + pos.y + ", " + pos.z);
			
			// 受信した情報から選択されたマスに変換します.
			index = ConvertPositionToIndex(pos);
			if (index < 0) {
				// 範囲外が選択されました.
				return false;
			}
		}

		// マスに目を置きます.
		bool ret = SetMarkToSpace(index, localMark);
		if (ret == false) {
			// 置けない.
			return false;
		}

		// 選択したマスの情報を送信します.
		byte[] buffer = new byte[1];
		buffer[0] = (byte)index;
		m_transport.Send (buffer, buffer.Length);

		return true;
	}
	
	// 相手のターンの時の処理.
	bool DoOppnentTurn()
	{
		// 相手の情報を受信します.
		byte[] buffer = new byte[1];
		int recvSize = m_transport.Receive(ref buffer, buffer.Length);

		if (recvSize <= 0) {
			// まだ受信していません.
			return false;			
		}

		// 受信した情報から選択されたマスに変換します.
		int index = (int) buffer[0];

		Debug.Log("Recv:" + index + " [" + m_transport.IsServer() + "]");
	
		// マスに目を置きます.
		bool ret = SetMarkToSpace(index, remoteMark);
		if (ret == false) {
			// 置けない.
			return false;
		}
		
		return true;
	}
	
	// 2次元の配置座標を1次元のインデックス番号に変換.
	int ConvertPositionToIndex(Vector3 pos)
	{
		float sx = SPACES_WIDTH;
		float sy = SPACES_HEIGHT;
		
		// フィールドの左上隅を起点とした座標系に変換します.
		float left = ((float)Screen.width - sx) * 0.5f;
		float top = ((float)Screen.height + sy) * 0.5f;
		
		float px = pos.x - left;
		float py = top - pos.y;
		
		if (px < 0.0f || px > sx) {
			// フィールド外です.
			return -1;	
		}
		
		if (py < 0.0f || py > sy) {
			// フィールド外です.
			return -1;	
		}
	
		// インデックス番号へ変換します.
		float divide = (float)rowNum;
		int hIndex = (int)(px * divide / sx);
		int vIndex = (int)(py * divide / sy);
		
		int index = vIndex * rowNum  + hIndex;
		
		return index;
	}
	
	// マークを配置.
	bool SetMarkToSpace(int index, Mark mark)
	{
		if (spaces[index] == -1) {
			// 未選択のマスなので置けます.
			spaces[index] = (int) mark;
			return true;
		}
		
		// 既に置かれています.
		return false;
	}
	
	// マークの並びをチェック
	Winner CheckInPlacingMarks()
	{
		string spaceString = "";
		for (int i = 0; i < spaces.Length; ++i) {
			spaceString += spaces[i] + "|";
			if (i % rowNum == rowNum - 1) {
				spaceString += "  ";	
			}
		}
		Debug.Log(spaceString);
		
		// 横方向のチェックをします.
		for (int y = 0; y < rowNum; ++y) {
			int mark = spaces[y * rowNum];
			int num = 0;
			for (int x = 0; x < rowNum; ++x) {
				int index = y * rowNum + x;
				if (mark == spaces[index]) {
					++num;
				}
			}
			
			if (mark != -1 && num == rowNum) {
				// マークがそろったので勝敗が決定します.
				return (mark == 0)? Winner.Circle : Winner.Cross;
			}
		}
		
		// 縦方向のチェックをします.
		for (int x = 0; x < rowNum; ++x) {
			int mark = spaces[x];
			int num = 0;
			for (int y = 0; y < rowNum; ++y) {
				int index = y * rowNum + x;
				if (mark == spaces[index]) {
					++num;
				}
			}
					
			if (mark != -1 && num == rowNum) {
				// マークがそろったので勝敗が決定します.
				return (mark == 0)? Winner.Circle : Winner.Cross;
			}
		}
		
		// 左上からの斜め方向のチェックをします.
		{
			int mark = spaces[0];
			int num = 0;
			for (int xy = 0; xy < rowNum; ++xy) {
				int index = xy * rowNum + xy;
				if (mark == spaces[index]) {
					++num;
				}
			}
						
			if (mark != -1 && num == rowNum) {
				// マークがそろったので勝敗が決定します.
				return (mark == 0)? Winner.Circle : Winner.Cross;
			}	
		}

		// 左上からの斜め方向のチェックをします.
		{
			int mark = spaces[rowNum - 1];
			int num = 0;
			for (int xy = 0; xy < rowNum; ++xy) {
				int index = xy * rowNum + rowNum - xy - 1;
				if (mark == spaces[index]) {
					++num;
				}
			}
						
			if (mark != -1 && num == rowNum) {
				// マークがそろったので勝敗が決定します.
				return (mark == 0)? Winner.Circle : Winner.Cross;
			}	
		}
		
		// 引き分けのチェックをします.
		{
			int num = 0;
			foreach (int space in spaces) {
				if (space == -1) {
					++num;	
				}
			}
			
			if (num == 0) {
				// 置ける場所がなく勝敗がつかないので引き分けです.
				return Winner.Tie;
			}
		}
		
		return Winner.None;
	}

	// ゲームリセット.
	void Reset()
	{
		turn = Mark.Circle;
		progress = GameProgress.None;
		
		// 未選択として初期化します.
		for (int i = 0; i < spaces.Length; ++i) {
			spaces[i] = -1;	
		}
	}

	// フィールドとマークを描画.
	void DrawFieldAndMarks()
	{
		float sx = SPACES_WIDTH;
		float sy = SPACES_HEIGHT;
		
		// フィールドを描画します.
		Rect rect = new Rect(Screen.width / 2 - WINDOW_WIDTH * 0.5f, 
		                     Screen.height / 2 - WINDOW_HEIGHT * 0.5f, 
		                     WINDOW_WIDTH, 
		                     WINDOW_HEIGHT);
		Graphics.DrawTexture(rect, fieldTexture.texture);
		
		// フィールドの左上隅を起点とした座標系に変換します.
		float left = ((float)Screen.width - sx) * 0.5f;
		float top = ((float)Screen.height - sy) * 0.5f;

		// 置かれているマークを描画します.
		for (int index = 0; index < spaces.Length; ++index) {
			if (spaces[index] != -1) {
				int x = index % rowNum;
				int y = index / rowNum;
				
				float divide = (float)rowNum;
				float px = left + x * sx / divide;
				float py = top + y * sy / divide;
				
				Texture texture = (spaces[index] == 0)? circleTexture.texture : crossTexture.texture;
				
				float ofs = sx / divide * 0.1f;
				
				Graphics.DrawTexture(new Rect(px+ofs, py+ofs, sx * 0.8f / divide, sy* 0.8f / divide), texture);
			}
		}

		// 手番テクスチャを表示します.
		if (localMark == turn) {
			float offset = (localMark == Mark.Circle)? -94.0f : sx + 36.0f;
			rect = new Rect(left + offset, top + 5.0f, 68.0f, 136.0f);
			Graphics.DrawTexture(rect, youTexture.texture);
		}
	}

	// 残り時間表示.
	void DrawTime()
	{
		GUIStyle style = new GUIStyle();
		style.fontSize = 35;
		style.fontStyle = FontStyle.Bold;
		
		string str = "Time : " + timer.ToString("F3");
		
		style.normal.textColor = (timer > 5.0f)? Color.black : Color.white;
		GUI.Label(new Rect(222, 5, 200, 100), str, style);
		
		style.normal.textColor = (timer > 5.0f)? Color.white : Color.red;
		GUI.Label(new Rect(220, 3, 200, 100), str, style);
	}

	// 結果表示.
	void DrawWinner()
	{
		float sx = SPACES_WIDTH;
		float sy = SPACES_HEIGHT;
		float left = ((float)Screen.width - sx) * 0.5f;
		float top = ((float)Screen.height - sy) * 0.5f;

		// 手番テクスチャを表示します.
		float offset = (localMark == Mark.Circle)? -94.0f : sx + 36.0f;
		Rect rect = new Rect(left + offset, top + 5.0f, 68.0f, 136.0f);
		Graphics.DrawTexture(rect, youTexture.texture);

		// 結果表示.
		rect.y += 140.0f;

		if (localMark == Mark.Circle && winner == Winner.Circle ||
		    localMark == Mark.Cross && winner == Winner.Cross) {
			Graphics.DrawTexture(rect, winTexture.texture);
		}
			
		if (localMark == Mark.Circle && winner == Winner.Cross ||
		    localMark == Mark.Cross && winner == Winner.Circle) {
			Graphics.DrawTexture(rect, loseTexture.texture);
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
			Reset();
			isGameOver = true;
		}
	}

	// ゲーム開始.
	public void GameStart()
	{
		// ゲーム開始の状態にします.
		progress = GameProgress.Ready;

		// サーバが先手になるように設定します.
		turn = Mark.Circle;

		// 自分と相手のマークを設定します.
		if (m_transport.IsServer() == true) {
			localMark = Mark.Circle;
			remoteMark = Mark.Cross;
		}
		else {
			localMark = Mark.Cross;
			remoteMark = Mark.Circle;
		}

		// 前回の設定をクリアします.
		isGameOver = false;
	}
	
	// ゲーム終了チェック.
	public bool IsGameOver()
	{
		return isGameOver;
	}

	// イベント発生時のコールバック関数.
	public void EventCallback(NetEventState state)
	{
		switch (state.type) {
		case NetEventType.Disconnect:
			if (progress < GameProgress.Result && isGameOver == false) {
				progress = GameProgress.Disconnect;
			}
			break;
		}
	}
}
