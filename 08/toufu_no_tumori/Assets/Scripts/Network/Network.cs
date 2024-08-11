// ネットワークライブラリ
//
// ■プログラムの説明
// TransportTCPクラスとTransportUDPクラスのまとめて使用できるようにするクラスです.
// 常に通信が行えるよう DontDestroyOnLoad() 関数を呼び、シーンをまたいでも破棄されないようにしています.
// 通信相手のデータを受信するためのサーバー処理を実行して待ち受けを行います.
// 通信相手が Connect() 関数を呼び出して接続したらお互いに送受信できる状態になります.
// 通信相手と Send***() 関数、ReceivePacket() 関数で双方向のデータ通信を行います.
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
//　　ゲームプログラムからデータを送受信を行うために　Send***() 関数、ReceivePacket() 関数を使用します.
//   これらの関数はソケットによる送受信をしていません.
//   実際の送受信は通信スレッド側で行います.これはデータの祖受信によるオーバーヘッドでゲームの処理時間を消費しないようにしています.
//
// ●イベント処理
//	  通信相手の接続や切断をゲームプログラムに通知するための仕組みをイベントハンドラーとして登録、削除をします.
//    ゲームプログラム側に EventHandler 型の関数 [例： void foo(NetEventState state) { ... } ]を定義します.
//    定義した関数を RegisterEventHandler() 関数、UnregisterEventHandler() 関数に渡してイベントハンドラーに登録、解除をします.
//    通信相手が接続、切断した時にイベントハンドラーに登録した関数が呼び出され、ゲームプログラムの関数がデリゲートからが呼び出されます.
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

	private TransportTcp	m_tcp = null;

	private TransportUdp	m_udp = null;

	private Thread			m_thread = null;

	private	bool			m_isStarted = false;

	// サーバー.
	private bool 			m_isServer = false;
	
	
	private const int 		m_headerVersion = 1;


	// 送受信用のパケットの最大サイズ.
	private const int		m_packetSize = 1400;


	private const int packetMax = (int)PacketId.Max;

	// 受信パケット処理関数のデリゲート.
	public delegate	void	RecvNotifier(PacketId id, byte[] data);
	// 受信パケット振り分けハッシュテーブル.
	private Dictionary<int, RecvNotifier> m_notifier = new Dictionary<int, RecvNotifier>();

	// イベント通知のデリゲート.
	public delegate void 	EventHandler(NetEventState state);
	// イベントハンドラー.
	private EventHandler	m_handler;

	// イベント発生フラグ.
	private bool			m_eventOccured = false;

	// 接続プロトコル指定.
	public enum ConnectionType
	{
		TCP = 0,		// TCPのみ接続対象.
		UDP,			// UDPのみ接続対象.
		Both,			// TCP,UDPの両方が接続対象.
	}

	void Awake()
	{
		DontDestroyOnLoad(gameObject);
	}

	// Use this for initialization
	void Start()
	{

	}
	
	// Update is called once per frame
	void Update()
	{
		if (IsConnected() == true) {
			byte[] packet = new byte[m_packetSize];

			// 到達保障パケットの受信をします.
			while (m_tcp != null && m_tcp.Receive(ref packet, packet.Length) > 0) {
				// 受信パケットの振り分けをします.
				ReceivePacket(packet);
			}
	
			// 非到達保障パケットの受信をします.
			while (m_udp != null && m_udp.Receive(ref packet, packet.Length) > 0) {
				// 受信パケットの振り分けをします.
				ReceivePacket(packet);
			}
		}
	}

	// アプリケーション終了時の処理.
	void OnApplicationQuit()
	{
		if (m_isStarted == true) {
			StopServer();
		}
	}

	// 待ち受け開始.
	public bool StartServer(int port, ConnectionType type)
	{
		Debug.Log("Network::StartServer called. port:" + port);
		
		// リスニングソケットを生成します.
		try {
			// 到達保障用のTCP通信を開始します.
			if (type == ConnectionType.TCP ||
			    type == ConnectionType.Both) {
				m_tcp = new TransportTcp();
				m_tcp.StartServer(port);
				m_tcp.RegisterEventHandler(OnEventHandling);
			}
			// 到達保障を必要としないUDP通信を開始します.
			if (type == ConnectionType.UDP ||
			    type == ConnectionType.Both) {
				m_udp = new TransportUdp();
				m_udp.StartServer(port);
				m_udp.RegisterEventHandler(OnEventHandling);
			}
		}
		catch {
			Debug.Log("Network::StartServer fail.!");
			return false;
		}

		Debug.Log("Network::Server started.!");

		m_isServer = true;

		return LaunchThread();
	}

	// 待ち受け終了.
	public void StopServer()
	{
		m_isStarted = false;
		if (m_thread != null) {
			m_thread.Join();
			m_thread = null;
		}

		// TCPのサーバー起動を停止します.
		if (m_tcp != null) {
			m_tcp.StopServer();
		}

		// UDPのサーバー起動を停止します.
		if (m_udp != null) {
			m_udp.StopServer();
		}

		m_notifier.Clear();

		m_isServer = false;
		m_eventOccured = false;

		Debug.Log("Server stopped.");
	}

	// 接続処理.
	public bool Connect(string address, int port, ConnectionType type)
	{
		try {
			Debug.Log("Addrss:" + address + " port:" + port + " type:" + type.ToString());

			bool ret = true;
			if (type == ConnectionType.TCP ||
			    type == ConnectionType.Both) {
				// 到達保障用のTCP通信を開始します.
				if (m_tcp == null) {
					m_tcp = new TransportTcp();
					m_tcp.RegisterEventHandler(OnEventHandling);
				}
				ret &= m_tcp.Connect(address, port);
			}

			if (type == ConnectionType.UDP ||
			    type == ConnectionType.Both) {
				// 到達保障を必要としないUDP通信を開始します.
				if (m_udp == null) {
					m_udp = new TransportUdp();
					m_udp.RegisterEventHandler(OnEventHandling);
				}
				ret &= m_udp.Connect(address, port);
			}

			if (ret == false) {
				if (m_tcp != null) { m_tcp.Disconnect(); }
				if (m_udp != null) { m_udp.Disconnect(); }
				return false;
			}
		}
		catch {
			return false;
		}

		return LaunchThread();
	}

	// 切断処理.
	public bool Disconnect()
	{	
		if (m_tcp != null) {
			m_tcp.Disconnect();
		}
		
		if (m_udp != null) {
			m_udp.Disconnect();
		}

		m_isStarted = false;
		m_eventOccured = false;

		return true;
	}

	// 受信通知関数登録.
	public void RegisterReceiveNotification(PacketId id, RecvNotifier notifier)
	{
		int index = (int)id;

		if (m_notifier.ContainsKey(index)) {
			m_notifier.Remove(index);
		}

		m_notifier.Add(index, notifier);
	}

	// 受信通知関数削除.
	public void UnregisterReceiveNotification(PacketId id)
	{
		int index = (int)id;

		if (m_notifier.ContainsKey(index)) {
			m_notifier.Remove(index);
		}
	}

	// イベント通知関数登録.
	public void RegisterEventHandler(EventHandler handler)
	{
		m_handler += handler;
	}
	
	// イベント通知関数削除.
	public void UnregisterEventHandler(EventHandler handler)
	{
		m_handler -= handler;
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
				
	// 接続可能状態にある.
	public bool IsConnected()
	{
		bool isTcpConnected = false;
		bool isUdpConnected = false;

		if (m_tcp != null && m_tcp.IsConnected()) {
			isTcpConnected = true;
		}

		if (m_udp != null && m_udp.IsConnected()) {
			isUdpConnected = true;
		}		

		if (m_tcp != null && m_udp == null) {
			return isTcpConnected;
		}

		if (m_tcp == null && m_udp != null) {
			return isUdpConnected;
		}
						
		return	(isTcpConnected && isUdpConnected);
	}

	// 相互通信している.
	public bool IsCommunicating()
	{
		bool isTcpConnected = false;
		bool isUdpConnected = false;
		
		// TCPの接続確認をします.
		if (m_tcp != null && m_tcp.IsConnected()) {
			isTcpConnected = true;
		}

		// UDPの接続確認をします.
		if (m_udp != null && m_udp.IsCommunicating()) {
			isUdpConnected = true;
		}		
		
		// TCPのみの接続確認をします.
		if (m_tcp != null && m_udp == null) {
			return isTcpConnected;
		}
		
		// UDPのみの接続確認をします.
		if (m_tcp == null && m_udp != null) {
			return isUdpConnected;
		}
		
		return	(isTcpConnected && isUdpConnected);
	}

	// サーバーか確認.
	public bool IsServer()
	{
		return	m_isServer;
	}


	// スレッド起動関数.
	bool LaunchThread()
	{
		Debug.Log("Launching thread.");	
		
		try {
			// Dispatch用のスレッドを起動します.
			m_isStarted = true;	
			m_thread = new Thread(new ThreadStart(Dispatch));
			m_thread.Start();
		}
		catch {
			Debug.Log("Cannot launch thread.");
			return false;
		}
		
		Debug.Log("Thread launched.");	
	
		return true;
	}

	// スレッド側の送受信処理.
	void Dispatch()
	{
		while (m_isStarted) {
						
			// クライアントとの受信を処理します.
			if (m_tcp != null) {			
				// TCPの送受信処理.
				m_tcp.Dispatch();
			}

			if ( m_udp != null) {			
				// UDPの送受信処理.
				m_udp.Dispatch();
			}

			Thread.Sleep(5);
		}
	}


	// 到達保障パケットの送信(バイナリー送信).
	public int Send(PacketId id, byte[] data)
	{
		int sendSize = 0;
		
		if (m_tcp != null) {
			// モジュールで使用するヘッダ情報を生成します.
			PacketHeader header = new PacketHeader();
			HeaderSerializer serializer = new HeaderSerializer();
			
			header.packetId = id;

			byte[] headerData = null;
			if (serializer.Serialize(header) == true) {
				headerData = serializer.GetSerializedData();
			}
			
			byte[] packetData = new byte[headerData.Length + data.Length];
			
			int headerSize = Marshal.SizeOf(typeof(PacketHeader));
			Buffer.BlockCopy(headerData, 0, packetData, 0, headerSize);
			Buffer.BlockCopy(data, 0, packetData, headerSize, data.Length);
			
			sendSize = m_tcp.Send(data, data.Length);
		}
		
		return sendSize;
	}	

	// 到達保障パケットの送信(パケット送信).
	public int SendReliable<T>(IPacket<T> packet)
	{
		int sendSize = 0;
		
		if (m_tcp != null) {
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
			
			sendSize = m_tcp.Send(data, data.Length);
			if (sendSize < 0) {
				// 送信エラー.
				string str = "Send reliable packet[" +  header.packetId  + "]";
				Debug.Log(str);
			}
		}
		
		return sendSize;
	}

	// 到達保障パケットをセッション全員に送信(パケット送信).
	// この章ではセッションに2人しかいないため1人のみに送信します.
	public void SendReliableToAll<T>(IPacket<T> packet)
	{
		byte[] data = packet.GetData();

		int sendSize = m_tcp.Send(data, data.Length);
		if (sendSize < 0) {
			// 送信エラー.
		}
	}

	// 到達保障パケットをセッション全員に送信(バイナリー送信).
	// この章ではセッションに2人しかいないため1人のみに送信します.
	public void SendReliableToAll(PacketId id, byte[] data)
	{
		if (m_tcp != null) {
			// モジュールで使用するヘッダ情報を生成します.
			PacketHeader header = new PacketHeader();
			HeaderSerializer serializer = new HeaderSerializer();
			
			header.packetId = id;
			
			byte[] headerData = null;
			if (serializer.Serialize(header) == true) {
				headerData = serializer.GetSerializedData();
			}

			byte[] pdata = new byte[headerData.Length + data.Length];
			
			int headerSize = Marshal.SizeOf(typeof(PacketHeader));
			Buffer.BlockCopy(headerData, 0, pdata, 0, headerSize);
			Buffer.BlockCopy(data, 0, pdata, headerSize, data.Length);
			
			int sendSize = m_tcp.Send(pdata, pdata.Length);
			if (sendSize < 0) {
				// 送信エラー.
				string str = "Send reliable packet[" +  header.packetId  + "]";
				Debug.Log(str);
			}
		}
	}

	// 到達保証なしのパケット送信.
	public int SendUnreliable<T>(IPacket<T> packet)
	{
		int sendSize = 0;
		
		if (m_udp != null) {
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
			
			sendSize = m_udp.Send(data, data.Length);
		}
		
		return sendSize;
	}

	// パケットの受信.
	public void ReceivePacket(byte[] data)
	{
		PacketHeader header = new PacketHeader();
		HeaderSerializer serializer = new HeaderSerializer();

		bool ret = serializer.Deserialize(data, ref header);
		if (ret == false) {
			// パケットとして認識できないので破棄します.
			return;			
		}

		if (m_udp != null && m_udp.IsCommunicating() == true) {
			// 通信相手から初の受信時に相互通信できるように接続の通知をします.
			if (m_handler != null && m_eventOccured == false) {
				NetEventState state = new NetEventState();
				state.type = NetEventType.Connect;
				state.result = NetEventResult.Success;
				state.endPoint = m_udp.GetRemoteEndPoint();
				Debug.Log("Event handler call.");
				m_handler(state);
				m_eventOccured = true;
			}
		}
						
		int packetId = (int)header.packetId;
		if (m_notifier.ContainsKey(packetId) &&
			m_notifier[packetId] != null) {
			int headerSize = Marshal.SizeOf(typeof(PacketHeader));//sizeof(PacketId) + sizeof(int);
			byte[] packetData = new byte[data.Length - headerSize];
			Buffer.BlockCopy(data, headerSize, packetData, 0, packetData.Length);
	
			m_notifier[packetId]((PacketId)packetId, packetData);
		}
	}

	// ゲームサーバーの起動.
	public bool StartGameServer()
	{
		Debug.Log("GameServer called.");

		GameObject obj = new GameObject("GameServer");
		GameServer server = obj.AddComponent<GameServer>();
		if (server == null) {
			Debug.Log("GameServer failed start.");
			return false;
		}

		server.StartServer();
		DontDestroyOnLoad(server);
		Debug.Log("GameServer started.");
		
		return true;
	}
	
	// ゲームサーバーの停止.
	public void StopGameServer()
	{
		GameObject obj = GameObject.Find("GameServer");
		if (obj) {
			GameObject.Destroy(obj);
		}

		Debug.Log("GameServer stoped.");
	}

	// イベントハンドラー呼び出し.
	public void OnEventHandling(NetEventState state)
	{
		if (m_handler != null) {
			m_handler(state);
		}
	}
}
