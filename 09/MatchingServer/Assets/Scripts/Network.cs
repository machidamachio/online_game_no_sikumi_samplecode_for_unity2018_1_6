// ネットワークライブラリ
//
// ■プログラムの説明
// ゲームサーバの通信をTransportTCPクラスで行うクラスです.
// 常に通信が行えるよう DontDestroyOnLoad() 関数を呼び、シーンをまたいでも破棄されないようにしています.
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
//　　ゲームサーバープログラムからデータを送受信を行うために　Send***() 関数、Receive() 関数を使用します.
//
// ●イベント処理
//	  クライアントの接続や切断を検知するために ServerSignalNotification() 関数をイベントハンドラーとして登録しています.
//    ServerSignalNotification() 関数で接続/切断したクライアントをゲームサーバーのプログラムに通知します.
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

	private SessionTCP		m_sessionTcp = null;


	private const int 		m_headerVersion = NetConfig.SERVER_VERSION;

	private Dictionary<int, NodeInfo> m_nodes = new Dictionary<int, NodeInfo>();

	// 送受信用のパケットの最大サイズ.
	private const int		m_packetSize = 1400;
	

	// 受信パケット処理関数のデリゲート.
	public delegate			void RecvNotifier(int node, byte[] data);

	// 受信パケット振り分けディクショナリー.
	private Dictionary<int, RecvNotifier> m_notifier = new Dictionary<int, RecvNotifier>();

	// イベント通知のデリゲート.
	public delegate void 	NetEventHandler(int node, NetEventState state);
	// イベントハンドラー.
	private NetEventHandler	m_handler;


	private class NodeInfo
	{
		public int	node = 0;
	}


	void Awake()
	{
		m_sessionTcp = new SessionTCP();
		m_sessionTcp.RegisterEventHandler(ServerSignalNotification);

		DontDestroyOnLoad(gameObject);
	}


	// Update is called once per frame
	void Update()
	{
		byte[] packet = new byte[m_packetSize];

		Dictionary<int, NodeInfo> nodes = new Dictionary<int, NodeInfo>(m_nodes);

		foreach (int node in nodes.Keys) {
			// 到達保障パケットの受信をします.
			while (m_sessionTcp.Receive(node, ref packet) > 0) {
				// 受信パケットの振り分けをします.
				Receive(node, packet);
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
	public bool StartServer(int port, int connectionMax)
	{
		Debug.Log("Start server called.!");

		// リスニングソケットを生成します.
		try {
			// 到達保障用のTCP通信を開始します.
			m_sessionTcp.StartServer(port, connectionMax);
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
		if (m_sessionTcp != null) {
			m_sessionTcp.StopServer();
		}

		Debug.Log("Server stopped.");
	}

	// 接続処理.
	public int Connect(string address, int port)
	{
		int node = -1;

		// 到達保障用のTCP通信を開始します.
		node = m_sessionTcp.Connect(address, port);

		return node;
	}

	// 切断処理.
	public bool Disconnect(int node)
	{	
		if (m_sessionTcp != null) {
			m_sessionTcp.Disconnect(node);
		}

		return true;
	}
	
	// 受信通知関数登録.
	public void RegisterReceiveNotification(PacketId id, RecvNotifier notifier)
	{
		int index = (int)id;

		m_notifier.Add(index, notifier);
	}

	// サーバーか確認.
	public bool IsServer()
	{
		if (m_sessionTcp == null) {
			return false;
		}

		return	m_sessionTcp.IsServer();
	}

	// 接続している端末のエンドポイントを取得.
	public IPEndPoint GetEndPoint(int node)
	{
		if (m_sessionTcp == null) {
			return default(IPEndPoint);
		}

		return m_sessionTcp.GetRemoteEndPoint(node);
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
			if (sendSize < 0) {
				string str = "Send Packet[" +  id  + "]";
				Debug.Log(str);
			}
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
			if (sendSize < 0) {
				string str = "Send reliable packet[" +  header.packetId  + "]";
				Debug.Log(str);
			}
		}
		
		return sendSize;
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
		string str = "";
		for (int i = 0; i< 16; ++i) {
			str += data[i] + ":";
		}
		Debug.Log(str);

		int packetId = (int)header.packetId;
		if (packetId < m_notifier.Count && m_notifier[packetId] != null) {
			int headerSize = Marshal.SizeOf(typeof(PacketHeader));
			byte[] packetData = new byte[data.Length - headerSize];
			Buffer.BlockCopy(data, headerSize, packetData, 0, packetData.Length);
	
			m_notifier[packetId](node, packetData);
		}
	}

	// サーバーのイベント通知.
	public void ServerSignalNotification(int node, NetEventState state)
	{
		string str = "Node:" + node + " type:" + state.type.ToString() + " State:" + state.ToString();
		Debug.Log("ServerSignalNotification called");
		Debug.Log(str);
		
		switch (state.type) {
		case NetEventType.Connect: {
			NodeInfo info = new NodeInfo();
			info.node = node;
			m_nodes.Add(node, info);
			if (m_handler != null) {
				m_handler(node, state);
			}
		} 	break;
			
		case NetEventType.Disconnect: {
			if (m_handler != null) {
				m_handler(node, state);
			}
			m_nodes.Remove(node);
		}	break;
			
		}
	}

	// イベントハンドラーの登録.
	public void RegisterEventHandler(NetEventHandler handler)
	{
		m_handler = handler;
	}
}
