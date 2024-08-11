// キー入力の送受信、キー入力遅延の制御
//
// ■プログラムの説明
// 通信の接続、切断、キーデータの送受信を行うクラスです.
// 待ち受け、接続の処理を NetworkController() 関数(コンストラクタ)で行います.
// キーデータの送受信を行い、自分と対戦相手のキーデータが揃ったらゲーム側にデータを渡して同期をとります. 
// UpdateSync() 関数で入力されたキーデータの送信を行い、対戦相手のキーデータを受信のシーケンスを実行します.
// キーデータの送信は SendInputData() 関数で行い、対戦相手のキーデータの受信は ReceiveInputData() 関数で行っています.
// キーデータが揃いゲームを1フレーム進めてい良い状態になると IsSync() 関数が True を返します.
// IsSync() 関数が False を返している間は必要なキーデータがそろっていないのでフレームを進めることができません.
// OnEventHandling() 関数をイベントハンドラーとして登録して対戦相手が接続/切断したときのイベントを処理します.
//

//#define EMURATE_INPUT //デバッグ中入力.
//#define DEBUG_WRITE

using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class NetworkController{
#if DEBUG_WRITE
    System.IO.StreamWriter m_debugWriterSyncData = null;
#endif
    void DebugWriterSetup() {
#if DEBUG_WRITE
        string filename = Application.dataPath + "/SyncData.log";
        m_debugWriterSyncData = new System.IO.StreamWriter(filename);
        m_debugWriterSyncData.WriteLine("SyncDataLog");
#endif
    }
    ~NetworkController() {
#if DEBUG_WRITE
        m_debugWriterSyncData.WriteLine("end");
        m_debugWriterSyncData.Close();
#endif
    }

    TransportUDP m_transport;       //よく使うので.
    InputManager m_inputManager;    //捕まえておく.

    // サーバー/クライアントの判定用.
    public enum HostType {
        Server,
        Client,
    }

    HostType m_hostType;

	private static int 			playerNum = 2;
	private const int			bufferNum = 4;  //何個分の入力値を送信するのか.	
	
	private List<MouseData>[]	inputBuffer = new List<MouseData>[playerNum];
	private MouseData[]			mouseData = new MouseData[playerNum];

	public enum SyncState {
		NotStarted = 0,			// キーデータの送受信をしていない.
		WaitSynchronize,		// キーデータの送信または受信をしている.
		Synchronized,			// 同期状態.
	}

    private int                 sendFrame = -1;
	private int					recvFrame = -1;

	private bool				isSynchronized = false;

	// 状態管理変数.
	private SyncState			syncState = SyncState.NotStarted;

	// 接続状態.
	private bool				isConnected = false;

	// 切断用フラフ.
	private int 				suspendSync = 0;

	// 通信環境が悪い時のための冗長データの再送信カウンタ.
	private int					noSyncCount = 0;

	// ゲーム終了時の切断までの猶予期間.
	private int					disconnectCount = 0;

	// 接続確認用のダミーパケットデータ.
	private const string 		requestData = "Request Connection.";
	

    // コンストラクタ.
    public NetworkController(string hostAddress, bool isHost) {
        DebugWriterSetup();

        isSynchronized = false;
		m_hostType = isHost? HostType.Server : HostType.Client;

        GameObject nObj = GameObject.Find("Network");
        m_transport = nObj.GetComponent<TransportUDP>();
		// 同一の端末で実行できるようにポート番号をずらしています.
		// 別々の端末で実行する場合はポート番号が同じものを使います.
		int listeningPort = isHost? NetConfig.GAME_PORT : NetConfig.GAME_PORT + 1;
		m_transport.StartServer(listeningPort);
		// 同一の端末で実行できるようにポート番号をずらしています.
		// 別々の端末で実行する場合はポート番号が同じものを使います.
		int remotePort = isHost? NetConfig.GAME_PORT + 1 : NetConfig.GAME_PORT;
		m_transport.Connect(hostAddress, remotePort);

		m_transport.RegisterEventHandler(OnEventHandling);

        GameObject iObj = GameObject.Find("InputManager");
        m_inputManager = iObj.GetComponent<InputManager>();

        for (int i = 0; i < inputBuffer.Length; ++i) {
            inputBuffer[i] = new List<MouseData>();
        }
    }
	
	// ネットワークの終了.
	public void Disconnect() {
		
		m_transport.Disconnect();
		m_transport.StopServer();
	}

    //ネットワークの状態を取得.
    public bool IsConnected()
	{
#if EMURATE_INPUT
        return true;    //デバッグ中は接続してるものとして偽装します.
#endif

		bool netConnected = m_transport.IsConnected();

        return (isConnected && netConnected);
    }

	public SyncState GetSyncState()
	{
		return syncState;
	}

	public bool IsSuspned()
	{
		return (suspendSync == 0x03);
	}

    public HostType GetHostType()
	{
        return m_hostType;
    }
    
	public void SuspendSync()
	{
		if (suspendSync > 0) {
			return;
		}

		// bit1:切断応答、bit0:切断要求.
		suspendSync = 0x01;
		Debug.Log("SuspendSync requested.");
	}

	// 同期しているか確認.
    public bool IsSync()
	{
		bool isSuspended = ((suspendSync & 0x02) == 0x02);
		bool frameSync = (syncState == SyncState.Synchronized && isSynchronized);

		return (frameSync || !isConnected || isSuspended);
    }

    public void ClearSync()
	{
        isSynchronized = false;
    }


    // 送受信して同期を取る.
    public bool UpdateSync()
	{
		if (IsConnected() == false && syncState == SyncState.NotStarted) {
			// 接続するまで相手に接続要求を投げます.
			// TransportUDP.AcceptClient関数で初めてパケットを受信すると
			// 接続フラグが立ちますのでダミーパケットを投げます.
			byte[] request = System.Text.Encoding.UTF8.GetBytes(requestData);
			m_transport.Send(request, request.Length);
			return false;
		}

		// キーバッファに現在のフレームのキー情報を追加します.
		bool update = EnqueueMouseData();
		
		// 送信.
		if (update) {
			SendInputData();
		}
		
		// 受信.
		ReceiveInputData();

		// キーバッファ先頭のキー入力情報を反映させます.
        if (IsSync() == false) {    //同期済みのままなら何もしない.
            DequeueMouseData();
        }
		
#if EMURATE_INPUT
        EmurateInput(); //デバッグ中は入力を偽装します.
#endif

		return IsSync();
    }

    // 送信.
    void SendInputData()
	{
		PlayerInfo info = PlayerInfo.GetInstance();
		int playerId = info.GetPlayerId();
		int count = inputBuffer[playerId].Count;
		
		InputData inputData = new InputData();
		inputData.count = count;
		inputData.flag = suspendSync;
		inputData.datum = new MouseData[bufferNum];

		for (int i = 0; i < count; ++i) {
			inputData.datum[i] = inputBuffer[playerId][i];
		}
		
		// 構造体をbyte配列に変換します.
		InputSerializer serializer = new InputSerializer();
		bool ret = serializer.Serialize(inputData);
		if (ret) {
			byte[] data = serializer.GetSerializedData();
			
			// データを送信します.
			m_transport.Send(data, data.Length);
		}

		// 状態を更新.
		if (syncState == SyncState.NotStarted) {
			syncState = SyncState.WaitSynchronize;
		}
    }

    // 受信.
    public void ReceiveInputData()
	{
		byte[] data = new byte[1400];
		
		// データを送信します.
		int recvSize = m_transport.Receive(ref data, data.Length);
		if (recvSize < 0) {
			// 入力情報を受信していないため次のフレームを処理することができません.
			return;
		} 

		string str = System.Text.Encoding.UTF8.GetString(data);
		if (requestData.CompareTo(str.Trim('\0')) == 0) {
			// 接続要求パケットを受信しました.
			return;
		}

		// byte配列を構造体に変換します.
		InputData inputData = new InputData();
		InputSerializer serializer = new InputSerializer();
		serializer.Deserialize(data, ref inputData);
		
		// 受信した入力情報を設定します.
		PlayerInfo info = PlayerInfo.GetInstance();
		int playerId = info.GetPlayerId();
		int opponent = (playerId == 0)? 1 : 0;
		
		for (int i = 0; i < inputData.count; ++i) {
			int frame = inputData.datum[i].frame;
			if (recvFrame + 1 == frame) {
				inputBuffer[opponent].Add(inputData.datum[i]);
				++recvFrame;
			}
		}

		// 切断フラグを監視.  bit1:切断応答、bit0:切断要求.
		if ((inputData.flag & 0x03) == 0x03) {
			// 切断フラグを受信.
			suspendSync = 0x03;
			Debug.Log("Receive SuspendSync.");
		}

		if ((inputData.flag & 1) > 0 && (suspendSync & 1) > 0) {
			suspendSync |= 0x02;
			Debug.Log("Receive SuspendSync." + inputData.flag);
		}

		if (isConnected && suspendSync == 0x03) {
			// お互いに切断状態になったので相手への切断フラグを送信するための.
			// 猶予期間をとってちょっとしたら切断します.
			++disconnectCount;
			if (disconnectCount > 10) {
				m_transport.Disconnect();
				Debug.Log("Disconnect because of suspendSync.");
			}
		}
		
		// 状態を更新.
		if (syncState == SyncState.NotStarted) {
			syncState = SyncState.WaitSynchronize;
		}
	}


    // キーバッファへ追加.(入力遅延以上の情報は無視してfalseを返す).
	public bool EnqueueMouseData()
	{
		PlayerInfo info = PlayerInfo.GetInstance();
		int playerId = info.GetPlayerId();

		if (inputBuffer[playerId].Count >= bufferNum) {
			// 入力遅延以上の情報は受け付けません.
			++noSyncCount;
			if (noSyncCount >= bufferNum) {
				noSyncCount = 0;
				return true;
			}

			return false;
		}
		
		// キー入力を取得してキーバッファへ追加します.
        sendFrame++;
        MouseData mouseData = m_inputManager.GetLocalMouseData();
        mouseData.frame = sendFrame;
		inputBuffer[playerId].Add(mouseData);        

		return true;
	}

    // 同期済みの入力値を取り出す.
	public void DequeueMouseData()
	{
		// 両端末のデータがそろっているかチェックします.
		for (int i = 0; i < playerNum; ++i) {
			if (inputBuffer[i].Count == 0) {
				return;     //入力値がない場合はなにもしない.
			}
		}
		
		// データがそろっていたのでゲームで使用できるようにデータを渡します.
		for (int i = 0; i < playerNum; ++i) {
			mouseData[i] = inputBuffer[i][0];
			inputBuffer[i].RemoveAt(0);

            // 入力管理者に、同期済みのデータとして渡します.
            m_inputManager.SetInputData(i, mouseData[i]);

#if false
            m_debugWriterSyncData.WriteLine(mouseData[i]);
#endif
		}
#if false
        m_debugWriterSyncData.Flush();
#endif

		// 状態を更新します.
		if (syncState != SyncState.Synchronized) {
			syncState = SyncState.Synchronized;
		}

		isSynchronized = true;
	}

	// イベントハンドラー.
	public void OnEventHandling(NetEventState state)
	{
		switch (state.type) {
		case NetEventType.Connect:
			isConnected = true;
			Debug.Log("[NetworkController] Connected.");
			break;
			
		case NetEventType.Disconnect:
			isConnected = false;
			Debug.Log("[NetworkController] Disconnected.");
			break;
		}
	}

    // debug code.
    void EmurateInput() {
        PlayerInfo info = PlayerInfo.GetInstance();
        int playerId = info.GetPlayerId();
        MouseData inputData = m_inputManager.GetLocalMouseData(); //m_inputManager.GetMouseData(playerId);
        
        //同期済み入力値の偽装(自分の入力を相手のとして与える).
        int opponent = (playerId == 0) ? 1 : 0;
        m_inputManager.SetInputData(playerId, inputData);
        m_inputManager.SetInputData(opponent, inputData);

        // = SyncFlag.Synchronized;
        isSynchronized = true;
    }

}
