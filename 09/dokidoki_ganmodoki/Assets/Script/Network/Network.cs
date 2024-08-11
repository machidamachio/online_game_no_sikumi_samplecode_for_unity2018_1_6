// ネットワークライブラリ
//
// ■プログラムの説明
// ゲームサーバー、クライアント同士の通信を行うクラスです.
// 到達保障、非到達保障のセッション管理を行います.
// クライアントのデータを受信するためのサーバー処理を実行して待ち受けを行います.
// クライアントが Connect() 関数を呼び出して接続したらお互いに送受信できる状態になります.
// クライアントと Send***() 関数、ReceivePacket() 関数で双方向のデータ通信を行います.
// 
// ●待ち受けの動作
//   受信用ソケットを生成し、クライアントと送受信を行う送受信関数をゲームスレッドとは別のスレッド(通信スレッド)として起動します.
//   通信相手から送信されたメッセージを受信してゲームスレッドに渡すためにキューにバッファリングします.
//   待ち受けの動作は次の関数で処理しています.
//     StartServer() 関数でクライアントとの通信を開始します.
//     StopServer() 関数でクライアントとの通信を終了します.
//
// ●通信相手への接続の動作
//   Connect() 関数で待ち受けをしてる通信相手に接続を行います.
//   Disconnect() 関数で通信相手と切断します.
//
// ●送受信処理
//　　ゲームプログラムからデータを送受信を行うために　Send***() 関数、Receive() 関数を使用します.
//
//


using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;





public class Network : MonoBehaviour {

	// 到達保障で接続するためのセッション管理.
	private SessionTCP		m_sessionTcp = null;

	// 非到達保障で接続するためのセッション管理.
	private SessionUDP		m_sessionUdp = null;

	// サーバーのノードID.
	private int				m_serverNode = -1;

	// 接続しているクライアント同士のIDを保存.
	private int[]			m_clientNode = new int[NetConfig.PLAYER_MAX];

	// 到達保障で接続するノードの情報を保存.
	private NodeInfo[]		m_reliableNode = new NodeInfo[NetConfig.PLAYER_MAX];

	// 非到達保障で接続するノードの情報を保存.
	private NodeInfo[]		m_unreliableNode = new NodeInfo[NetConfig.PLAYER_MAX];

	// 送受信用のパケットの最大サイズ.
	private const int		m_packetSize = 1400;
	
	// 受信パケット処理関数のデリゲート.
	public delegate			void RecvNotifier(int node, PacketId id, byte[] data);

	// 受信パケット振り分けハッシュテーブル.
	private Dictionary<int, RecvNotifier> m_notifier = new Dictionary<int, RecvNotifier>();
	
	// イベントハンドラー.
	private List<NetEventState>	m_eventQueue = new List<NetEventState>();

	private class NodeInfo
	{
		public int	node = 0;
	}

	public enum ConnectionType
	{
		Reliable = 0,
		Unreliable,
	}

	// 初期化.
	void Awake()
	{
		m_sessionTcp = new SessionTCP();
		m_sessionTcp.RegisterEventHandler(OnEventHandlingReliable);

		m_sessionUdp = new SessionUDP();
		m_sessionUdp.RegisterEventHandler(OnEventHandlingUnreliable);

		for (int i = 0; i < m_clientNode.Length; ++i) {
			m_clientNode[i] = -1;
		}
	}
	
	// Update is called once per frame
	void Update()
	{
		byte[] packet = new byte[m_packetSize];
		for (int id = 0; id < m_reliableNode.Length; ++id) {
			if (m_reliableNode[id] != null) {
				int node = m_reliableNode[id].node;
				if (IsConnected(node) == true) {
					// 到達保障パケットの受信をします.
					while (m_sessionTcp.Receive(node, ref packet) > 0) {
						// 受信パケットの振り分けをします.
						Receive(node, packet);
					}
				}
			}
		}

		// 非到達保障パケットの受信をします.
		for (int id = 0; id < m_unreliableNode.Length; ++id) {
			if (m_unreliableNode[id] != null) {
				int node = m_unreliableNode[id].node;
				if (IsConnected(node) == true) {
					// 到達保障のないパケットの受信をします.
					while (m_sessionUdp.Receive(node, ref packet) > 0) {
						// 受信パケットの振り分けをします.
						Receive(node, packet);
					}
				}
			}		
		}
	}

	// アプリケーション終了時の処理.
	void OnApplicationQuit()
	{
		Debug.Log("OnApplicationQuit called.!");

		StopServer();
	}

	// 待ち受け開始.
	public bool StartServer(int port, int connectionMax, ConnectionType type)
	{
		Debug.Log("Start server called.!");

		// リスニングソケットを生成します.
		try {
			if (type == ConnectionType.Reliable) {
				// 到達保障用のTCP通信を開始します.
				m_sessionTcp.StartServer(port, connectionMax);
			}
			else {
				// 到達保障を必要としないUDP通信はいつでも受信できるようにリスニングを開始します.
				m_sessionUdp.StartServer(port, connectionMax);
			}
		}
		catch {
			Debug.Log("Server fail start.!");
			return false;
		}

		Debug.Log("Server started.!");


		return true;
	}

	// 待ち受け終了.
	public void StopServer()
	{
		Debug.Log("StopServer called.");

		// サーバー起動を停止.
		if (m_sessionUdp != null) {
			m_sessionUdp.StopServer();
		}

		if (m_sessionTcp != null) {
			m_sessionTcp.StopServer();
		}


		Debug.Log("Server stopped.");
	}

	// 接続処理.
	public int Connect(string address, int port, ConnectionType type)
	{
		int node = -1;

		if (type == ConnectionType.Reliable && m_sessionTcp != null) {
			// 到達保障用のTCP通信を開始します.
			node = m_sessionTcp.Connect(address, port);
		}

		if (type == ConnectionType.Unreliable && m_sessionUdp != null) {
			// 到達保障でないUDP通信を開始します.
			node = m_sessionUdp.Connect(address, port);
		}

		return node;
	}

	// 切断処理(ノードID指定).
	public void Disconnect(int node)
	{	
		if (m_sessionTcp != null) {
			m_sessionTcp.Disconnect(node);
		}
		
		if (m_sessionUdp != null) {
			m_sessionUdp.Disconnect(node);
		}
	}

	// 全ノード切断処理.
	public void Disconnect()
	{
		if (m_sessionTcp != null) {
			m_sessionTcp.Disconnect();
		}
		
		if ( m_sessionUdp != null) {
			m_sessionUdp.Disconnect();
		}

		m_notifier.Clear();
	}

	// 受信通知関数登録.
	public void RegisterReceiveNotification(PacketId id, RecvNotifier notifier)
	{
		int index = (int)id;

		m_notifier.Add(index, notifier);
	}

	// 受信通知のクリア.
	public void ClearReceiveNotification()
	{
		m_notifier.Clear();
	}

	// 受信通知関数削除.
	public void UnregisterReceiveNotification(PacketId id)
	{
		int index = (int)id;
		
		if (m_notifier.ContainsKey(index)) {
			m_notifier.Remove(index);
		}
	}
	
	// イベント状態取得.
	public NetEventState GetEventState()
	{

		if (m_eventQueue.Count == 0) {
			return null;
		}

		NetEventState state = m_eventQueue[0];

		m_eventQueue.RemoveAt(0);

		return 	state;
	}

	// 接続確認.
	public bool IsConnected(int node)
	{
		if (m_sessionTcp != null) {
			if (m_sessionTcp.IsConnected(node)) {
				return true;
			}
		}
		
		if (m_sessionUdp != null) {
			if (m_sessionUdp.IsConnected(node)) {
				return true;
			}
		}

		return	false;
	}

	// サーバー動作設定確認.
	public bool IsServer()
	{
		if (m_sessionTcp == null) {
			return false;
		}

		return	m_sessionTcp.IsServer();
	}

	// 接続元(ローカル側)のエンドポイント取得.
	public IPEndPoint GetLocalEndPoint(int node)
	{
		if (m_sessionTcp == null) {
			return default(IPEndPoint);
		}

		return m_sessionTcp.GetLocalEndPoint(node);
	}

	// 到達保障パケットの送信(パケットID指定).
	public int Send<T>(int node, PacketId id, IPacket<T> packet)
	{
		int sendSize = 0;
		
		if (m_sessionTcp != null) {
			// モジュールで使用するヘッダ情報を生成します.
			PacketHeader header = new PacketHeader();
			HeaderSerializer serializer = new HeaderSerializer();
					
			header.packetId = id;

			byte[] headerData = null;
			if (serializer.Serialize(header) == true) {
				headerData = serializer.GetSerializedData();
			}
			byte[] packetData = packet.GetData();
			
			byte[] data = new byte[headerData.Length + packetData.Length];
			
			int headerSize = Marshal.SizeOf(typeof(PacketHeader));
			Buffer.BlockCopy(headerData, 0, data, 0, headerSize);
			Buffer.BlockCopy(packetData, 0, data, headerSize, packetData.Length);

			sendSize = m_sessionTcp.Send(node, data, data.Length);
		}
		
		return sendSize;
	}

	// 到達保障パケットの送信(パケットID未指定).
	public int SendReliable<T>(int node, IPacket<T> packet)
	{
		int sendSize = 0;
		
		if (m_sessionTcp != null) {
			// モジュールで使用するヘッダ情報を生成します.
			PacketHeader header = new PacketHeader();
			HeaderSerializer serializer = new HeaderSerializer();
			
			header.packetId = packet.GetPacketId();

			byte[] headerData = null;
			if (serializer.Serialize(header) == true) {
				headerData = serializer.GetSerializedData();
			}

			byte[] packetData = packet.GetData();
			byte[] data = new byte[headerData.Length + packetData.Length];
			
			int headerSize = Marshal.SizeOf(typeof(PacketHeader));
			Buffer.BlockCopy(headerData, 0, data, 0, headerSize);
			Buffer.BlockCopy(packetData, 0, data, headerSize, packetData.Length);
			
			sendSize = m_sessionTcp.Send(node, data, data.Length);
			if (sendSize < 0 && m_eventQueue != null) {
				// 送信エラー.
				NetEventState state = new NetEventState();
				state.node = node;
				state.type = NetEventType.SendError;
				state.result = NetEventResult.Failure;
				m_eventQueue.Add(state);
			}
		}
		
		return sendSize;
	}

	// 到達保障パケットをセッション全員に送信(パケット送信).
	public void SendReliableToAll<T>(IPacket<T> packet)
	{
		foreach (NodeInfo info in m_reliableNode) {
			if (info != null) {

				int sendSize = SendReliable<T>(info.node, packet);
				if (sendSize < 0 && m_eventQueue != null) {
					// 送信エラー.
					NetEventState state = new NetEventState();
					state.node = info.node;
					state.type = NetEventType.SendError;
					state.result = NetEventResult.Failure;
					m_eventQueue.Add(state);
				}

			}
		}
	}

	// 到達保障パケットをセッション全員に送信(バイナリー送信).
	public void SendReliableToAll(PacketId id, byte[] data)
	{
		foreach (NodeInfo info in m_reliableNode) {

			if (info != null && info.node >= 0) {
				// モジュールで使用するヘッダ情報を生成します.
				PacketHeader header = new PacketHeader();
				HeaderSerializer serializer = new HeaderSerializer();
				
				header.packetId = id;
				
				byte[] headerData = null;
				if (serializer.Serialize(header) == true) {
					headerData = serializer.GetSerializedData();
				}
				
				byte[] packetData = data;
				byte[] pdata = new byte[headerData.Length + packetData.Length];
				
				int headerSize = Marshal.SizeOf(typeof(PacketHeader));
				Buffer.BlockCopy(headerData, 0, pdata, 0, headerSize);
				Buffer.BlockCopy(packetData, 0, pdata, headerSize, packetData.Length);

				int sendSize = m_sessionTcp.Send(info.node, pdata, pdata.Length);
				if (sendSize < 0 && m_eventQueue != null) {
					// 送信エラー.
					NetEventState state = new NetEventState();
					state.node = info.node;
					state.type = NetEventType.SendError;
					state.result = NetEventResult.Failure;
					m_eventQueue.Add(state);
				}

			}
		}
	}

	// 非到達保障パケットの送信(パケット送信).
	public int SendUnreliable<T>(int node, IPacket<T> packet)
	{
		int sendSize = 0;
		
		if (m_sessionUdp != null) {
			// モジュールで使用するヘッダ情報を生成します.
			PacketHeader header = new PacketHeader();
			HeaderSerializer serializer = new HeaderSerializer();
			
			header.packetId = packet.GetPacketId();

			byte[] headerData = null;
			if (serializer.Serialize(header) == true) {
				headerData = serializer.GetSerializedData();
			}
			byte[] packetData = packet.GetData();
			
			byte[] data = new byte[headerData.Length + packetData.Length];
			
			int headerSize = Marshal.SizeOf(typeof(PacketHeader));
			Buffer.BlockCopy(headerData, 0, data, 0, headerSize);
			Buffer.BlockCopy(packetData, 0, data, headerSize, packetData.Length);
			
			sendSize = m_sessionUdp.Send(node, data, data.Length);

 			if (sendSize < 0 && m_eventQueue != null) {
				// 送信エラー.
				NetEventState state = new NetEventState();
				state.node = node;
				state.type = NetEventType.SendError;
				state.result = NetEventResult.Failure;
				m_eventQueue.Add(state);
			}
		}
		
		return sendSize;
	}

	// 非到達保障パケットをセッション全員に送信(パケット送信).
	public void SendUnreliableToAll<T>(IPacket<T> packet)
	{
		foreach (NodeInfo info in m_unreliableNode) {
			if (info != null) {
				 SendUnreliable<T>(info.node, packet);
			}
		}
	}

	// パケットの受信.
	private void Receive(int node, byte[] data)
	{
		PacketHeader header = new PacketHeader();
		HeaderSerializer serializer = new HeaderSerializer();

		serializer.SetDeserializedData(data);
		bool ret = serializer.Deserialize(ref header);
		if (ret == false) {
			Debug.Log("Invalid header data.");
			// パケットとして認識できないので破棄します.
			return;			
		}

		int packetId = (int)header.packetId;
		if (m_notifier.ContainsKey(packetId) &&
		    m_notifier[packetId] != null) {
			int headerSize = Marshal.SizeOf(typeof(PacketHeader));
			byte[] packetData = new byte[data.Length - headerSize];
			Buffer.BlockCopy(data, headerSize, packetData, 0, packetData.Length);
	
			m_notifier[packetId](node, header.packetId, packetData);
		}
	}

	// 到達保障のパケットのイベントハンドラー.
	public void OnEventHandlingReliable(int node, NetEventState state)
	{
		Debug.Log("OnEventHandling called");
		string str = "Node:" + node + " type:" + state.type.ToString() + " State:" + state.type + "[" + state.result + "]";
		Debug.Log(str);
		
		switch (state.type) {
		case NetEventType.Connect: {
			for (int i = 0; i < m_reliableNode.Length; ++i) {
				if (m_reliableNode[i] == null) {
					NodeInfo info = new NodeInfo();
					
					info.node = node;
					m_reliableNode[i] = info;
					break;
				}
				else if (m_reliableNode[i].node == -1) {
					m_reliableNode[i].node = node;
				}
			}
			
		} 	break;
			
		case NetEventType.Disconnect: {
			for (int i = 0; i < m_reliableNode.Length; ++i) {

				if (m_reliableNode[i] != null && m_reliableNode[i].node == node) {

					m_reliableNode[i].node = -1;
					break;
				}
			}
			
		}	break;
			
		}

		
		if (m_eventQueue != null) {
			// イベント登録.
			NetEventState eState = new NetEventState();
			eState.node = node;
			eState.type = state.type;
			eState.result = NetEventResult.Success;
			m_eventQueue.Add(eState);
		}

	}

	// 非到達保障のパケットのイベントハンドラー.
	public void OnEventHandlingUnreliable(int node, NetEventState state)
	{
		Debug.Log("OnEventHandlingUnreliable called");
		string str = "Node:" + node + " type:" + state.type.ToString() + " State:" + state.result.ToString();
		Debug.Log(str);

		switch (state.type) {
			case NetEventType.Connect: {
				for (int id = 0; id < m_unreliableNode.Length; ++id) {
					if (m_unreliableNode[id] == null) {
						NodeInfo info = new NodeInfo();

						info.node = node;
						m_unreliableNode[id] = info;
					break;
					}
					else if (m_unreliableNode[id].node == -1) {
						m_unreliableNode[id].node = node;
					}
				}

			} 	break;

			case NetEventType.Disconnect: {

				for (int i = 0; i < m_unreliableNode.Length; ++i) {
					
					if (m_unreliableNode[i] != null && m_unreliableNode[i].node == node) {
						m_unreliableNode[i].node = -1;
					break;
					}
				}

			}	break;
		}

		if (m_eventQueue != null) {
			// イベント登録.
			NetEventState eState = new NetEventState();
			eState.node = node;
			eState.type = state.type;
			eState.result = NetEventResult.Success;
			m_eventQueue.Add(eState);
		}
	}

	// ゲームサーバーの起動.
	public bool StartGameServer(int playerNum)
	{
		GameObject obj = new GameObject("GameServer");
		GameServer server = obj.AddComponent<GameServer>();
		if (server == null) {
			Debug.Log("GameServer failed start.");
			return false;
		}
		
		server.StartServer(playerNum);
		DontDestroyOnLoad(server);
		Debug.Log("GameServer started.");
		
		return true;
	}

	// ゲームサーバーの停止.
	public void StopGameServer()
	{
		GameObject obj = GameObject.Find("GameServer");
		if (obj) {
			GameServer server = obj.GetComponent<GameServer>();
			if (server != null) {
				server.StopServer();
			}
			GameObject.Destroy(obj);
		}
		
		Debug.Log("GameServer stoped.");
	}

	// サーバーのノードIDを設定.
	public void SetServerNode(int node)
	{
		m_serverNode = node;
	}

	// サーバーのノードIDを取得.
	public int GetServerNode()
	{
		return m_serverNode;
	}

	// クライアントのノードIDを設定.
	public void SetClientNode(int gid, int node)
	{
		m_clientNode[gid] = node;
	}

	// クライアントのノードIDを取得.
	public int GetClientNode(int gid)
	{
		return m_clientNode[gid];
	}

	// ノードIDからゲームで使用するプレイヤーIDを取得.
	public int GetPlayerIdFromNode(int node)
	{

		for (int i = 0; i < m_clientNode.Length; ++i) {
			if (m_clientNode[i] == node) {
				return i;
			}
		}

		return -1;
	}


}
