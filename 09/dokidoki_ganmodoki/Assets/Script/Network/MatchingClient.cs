// マッチングサーバーとの接続
//
// ■プログラムの説明
// マッチングサーバーと接続を行うクラスの定義です.
// Unity の初期化関数である Start() 関数でサーバー処理の起動、マッチングサーバーへの要求パケットの処理
// をする関数の登録をしています.
// RequestCreateRoom() 関数でルーム作成要求のパケットを送信します.
// RequestSearchRoom() 関数でルーム検索要求のパケットを送信します.
// RequestJoinRoom() 関数でルーム入室要求のパケットを送信します.
// RequestStartSession() 関数でゲーム開始通知のパケットを送信します.
// OnReceiveMatchingResponse() 関数でマッチングサーバーからの応答パケットを処理しています.
// CreateRoomResponse() 関数でルーム作成の応答パケットを処理しています.
// SearchRoomResponse() 関数でルーム検索の応答パケットを処理しています.
// JoinRoomResponse() 関数でルーム参加の応答パケットを処理しています.
// OnReceiveSearchRoom() 関数でルーム検索結果パケットを処理しています.
// OnReceiveStartSession() 関数でゲーム開始パケットを処理しています.
// DrawRoomInfo() 関数でマッチングサーバーに作成されているルームの情報を表示しています.
//

// 1台の端末で動作させる場合に定義します.
//#define UNUSE_MATCHING_SERVER

using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Net;

public class MatchingClient : MonoBehaviour
{
	// 同時にマッチングを行えるルーム数の最大値.
	private const int 	maxRoomNum = 4;

	// 1ルームの最大マッチング人数.
	private const int 	maxMemberNum = NetConfig.PLAYER_MAX;

	// マッチングを行うための情報.
	private class RoomContent
	{
		public int 		node = -1;

		public int 		roomId = -1;

		public string	roomName = "";

		public int[]	members = Enumerable.Repeat(-1, maxMemberNum).ToArray();
	}

	// メンバーの情報.
	public class MemberList
	{
		public int			node = -1;

		public IPEndPoint	endPoint;
	}


	private Network 	network_;

	private int 		level_ = 0;

	// ルームに設定されたレベルの文字列表記.
	private string[] 	captions_ = new string[] {
		"指定なし", "かんたん", "ふつう", "むずかしい"
	};

	// for client.
	private string		roomName = "";

	private	bool		isRoomOwner = false;

	// ルーム情報管理.
	private Dictionary<int, RoomContent> 	m_rooms = new Dictionary<int, RoomContent>();
	

	private int				playerId = -1;

	private	RoomContent		joinedRoom = new RoomContent();

	private	MemberList[]	sessionMembers = new MemberList[maxMemberNum];

	private int 			m_memberNum = 0;

	private State			matchingState = State.Idle;

	private float			timer = 0.0f;

	private	int				serverNode = 0;

	// マッチングクライアントの動作状態.
	private enum State
	{
		Idle = 0,
		MatchingServer,
		RoomCreateRequested,
		RoomSearchRequested,
		RoomJoinRequested,
		WaitMembers,
		StartSessionRequested,
		StartSessionNotified,
		MatchingEnded,

		RoomCreateFailed,
		RoomJoinFailed,
	}


	// Use this for initialization
	void Start()
	{
		GameObject obj = GameObject.Find("Network");
		network_ = obj.GetComponent<Network>();

		if (network_ != null) {
			network_.RegisterReceiveNotification(PacketId.MatchingResponse, this.OnReceiveMatchingResponse);
			network_.RegisterReceiveNotification(PacketId.SearchRoomResponse, this.OnReceiveSearchRoom);
			network_.RegisterReceiveNotification(PacketId.StartSessionNotify, this.OnReceiveStartSession);
		}

		// 初回は全ルーム検索.
		RequestSearchRoom(-1, 0);

		matchingState = State.Idle;

#if UNUSE_MATCHING_SERVER
		matchingState = State.StartSessionNotified;
#endif
	}

	// Update is called once per frame
	void Update()
	{
		switch (matchingState) {

		case State.RoomCreateRequested:
			WaitMembers(joinedRoom);
			break;

		case State.RoomJoinRequested:
			WaitMembers(joinedRoom);
			break;

#if UNUSE_MATCHING_SERVER
		case State.StartSessionNotified:
			OnReceiveStartSession(0, 0, null);
			break;
#endif
		}

		timer += Time.deltaTime;
	}


	// ルームの情報更新.
	void WaitMembers(RoomContent room)
	{
		if (timer > 5.0f) {
			RequestSearchRoom(room.roomId, -1);
			timer = 0.0f;
		}
	}
	
	public void OnGUIMatching()
	{


		switch (matchingState) {
		case State.Idle:
			OnGUISelectRoomType();
			break;

		case State.RoomCreateRequested:
			OnGUIRoomCreated();
			break;

		case State.RoomJoinRequested:
			DrawRoomInfo(false);
			break;

		case State.MatchingEnded:
			DrawRoomInfo(false);
			break;

		case State.RoomCreateFailed:
			NotifyError("部屋を作成できませんでした");
			break;

		case State.RoomJoinFailed:
			NotifyError("部屋に参加できませんでした");
			break;
		}
	}

	// ルームの作成/検索の選択画面.
	void OnGUISelectRoomType()
	{
		int px = Screen.width/2;
		int py = Screen.height/2 - 50;
		int sx = 180;
		int sy = 30;

		{
			GUIStyle style = new GUIStyle();
			style.fontSize = 16;
			style.fontStyle = FontStyle.Bold;
			style.normal.textColor = Color.black;
			
			GUI.Label(new Rect(px-100, py-20, 160, 20), "部屋のおなまえ", style);
			roomName = GUI.TextField(new Rect(px-100, py, sx, sy), roomName);
		}

		if (GUI.Button(new Rect(px+90, py, sx-60, sy), "部屋を作る")) {
			RequestCreateRoom(roomName, level_);
			matchingState = State.RoomCreateRequested;
		}
		
		if (GUI.Button(new Rect(px+90, py+40, sx-60, sy), "部屋を探す")) {
			RequestSearchRoom(-1, level_);
		}


		{
			GUIStyle style = new GUIStyle();
			style.fontSize = 14;
			style.fontStyle = FontStyle.Bold;
			style.normal.textColor = Color.black;
			
			// レベル選択.
			level_ = GUI.SelectionGrid(new Rect(px-100, py+40, sx, 40), level_, captions_, 2);  
		}


		DrawRoomInfo(true);
	}

	// ルーム作成時の画面.
	void OnGUIRoomCreated()
	{
		int px = Screen.width/2;
		int py = Screen.height/2 - 50;

		if (GUI.Button(new Rect(px-100, py, 200, 30), "ゲームを始める")) {
			RequestStartSession(joinedRoom);
		}

		DrawRoomInfo(false);
	}

	// ルームの情報表示.
	private void DrawRoomInfo(bool enable)
	{
		int px = Screen.width/2 - 150;
		int py = Screen.height/2 + 70;
		int sy = 30;

		{
			GUIStyle style = new GUIStyle();
			style.fontSize = 16;
			style.fontStyle = FontStyle.Bold;
			style.normal.textColor = Color.black;

			if (matchingState == State.Idle) {
				GUI.Label(new Rect(px+30, py-20, 160, 20), "みつかったお部屋", style);
			}
			else if (matchingState == State.RoomCreateRequested || 
			         matchingState == State.RoomJoinRequested ||
			         matchingState == State.MatchingEnded) {
				GUI.Label(new Rect(px+110, py-20, 160, 20), "部屋の状況", style);
			}
		}

		int index = 1;
		foreach (RoomContent room in m_rooms.Values) {
			string label = "部屋" + index + " : " + room.roomName + 
				" (あと: " + (maxMemberNum - room.members[0]).ToString() + "人)";
			if (GUI.Button(new Rect(px, py, 300, sy), label)) {
				if (enable) {
					RequestJoinRoom(room);
				}
			}
			py += sy + 10;
			++index;
		}
	}

	// マッチングが終了したか確認.
	public bool IsFinishedMatching()
	{
		return (matchingState == State.MatchingEnded);
	}

	// ルームオーナー確認.
	public bool	IsRoomOwner()
	{
		return isRoomOwner;
	}

	// サーバーのノードIDを設定.
	public void SetServerNode(int node)
	{
		serverNode = node;
	}

	// プレイヤーID取得.
	public int GetPlayerId()
	{
		return playerId;
	}

	// メンバーの情報取得
	public MemberList[] GetMemberList()
	{
		return sessionMembers;
	}

	// ルーム参加人数取得.
	public int GetMemberNum()
	{
		return m_memberNum;
	}

	//
	// クライアント側の処理
	//

	// ルーム作成要求の送信.
	void RequestCreateRoom(string name, int level)
	{
		MatchingRequest request = new MatchingRequest();

		request.request = MatchingRequestId.CreateRoom;
		request.version = NetConfig.SERVER_VERSION;
		request.name = name;
		request.roomId = -1;
		request.level = level;

		MatchingRequestPacket packet = new MatchingRequestPacket(request);

		if (network_ != null) {
			network_.SendReliable<MatchingRequest>(serverNode, packet);
		}
	}

	// ルーム検索要求の送信.
	void RequestSearchRoom(int roomId, int level) 
	{
		MatchingRequest request = new MatchingRequest();

		request.request = MatchingRequestId.SearchRoom;
        request.version = NetConfig.SERVER_VERSION;
		request.name = "";
		request.roomId = roomId;
		request.level = level;

		MatchingRequestPacket packet = new MatchingRequestPacket(request);
		
		network_.SendReliable<MatchingRequest>(serverNode, packet);
	}

	// ルーム参加要求の送信.
	void RequestJoinRoom(RoomContent room)
	{
		MatchingRequest request = new MatchingRequest();
		
		request.request = MatchingRequestId.JoinRoom;
        request.version = NetConfig.SERVER_VERSION;
        request.name = "";
		request.roomId = room.roomId;
		request.level = -1;

		if (network_ != null) {
			MatchingRequestPacket packet = new MatchingRequestPacket(request);
		
			network_.SendReliable<MatchingRequest>(serverNode, packet);
		}

		string str = "RequestJoinRoom:" + room.roomId;
		Debug.Log(str);
	}
	
	// ゲーム開始要求の送信.
	void RequestStartSession(RoomContent room)
	{
		MatchingRequest request = new MatchingRequest();
		
		request.request = MatchingRequestId.StartSession;
        request.version = NetConfig.SERVER_VERSION;
        request.name = room.roomName;
		request.roomId = room.roomId;
		request.level = -1;

		Debug.Log("Request start session[room:" + room.roomName + " " + room.roomId + "]");

		if (network_ != null) {
			MatchingRequestPacket packet = new MatchingRequestPacket(request);
		
			network_.SendReliable<MatchingRequest>(serverNode, packet);
		}
	}

	//
	// パケット受信処理.
	//
	
	// マッチングサーバーからの応答パケット受信関数.
	void OnReceiveMatchingResponse(int node, PacketId id, byte[] data)
	{
		MatchingResponsePacket packet = new MatchingResponsePacket(data);
		MatchingResponse response = packet.GetPacket();

		string str = "ReceiveMatchingResponse:" + response.request;
		Debug.Log(str);

		switch (response.request) {
		case MatchingRequestId.CreateRoom:
			CreateRoomResponse(response.result, response.roomId);
			break;

		case MatchingRequestId.JoinRoom:
			JoinRoomResponse(response.result, response.roomId) ;
			break;

		default:
			Debug.Log("Unknown request.");
			break;
		}
	}

	// ルーム検索結果パケット受信関数.
	void OnReceiveSearchRoom(int node, PacketId id, byte[] data)
	{
		Debug.Log("ReceiveSearchRoom");

		SearchRoomPacket packet = new SearchRoomPacket(data);
		SearchRoomResponse response = packet.GetPacket();

		string str = "Created room num:" + response.roomNum;
		Debug.Log(str);

		SearchRoomResponse(response.roomNum, response.rooms);
	}

	// ゲーム開始パケット受信関数.
	void OnReceiveStartSession(int node, PacketId id, byte[] data)
	{
		Debug.Log("ReceiveStartSession");

#if UNUSE_MATCHING_SERVER
		SessionData response = new SessionData();

		{
			int memberNum = NetConfig.PLAYER_MAX;
			string hostname = Dns.GetHostName();
			IPAddress[] adrList = Dns.GetHostAddresses(hostname);
			response.endPoints = new EndPointData[memberNum];

			response.result = MatchingResult.Success;
			response.playerId = GlobalParam.get().global_account_id;
			response.members = memberNum;

			for (int i = 0; i < memberNum; ++i) {
				response.endPoints[i] = new EndPointData();
				response.endPoints[i].ipAddress = adrList[0].ToString();
				response.endPoints[i].port = NetConfig.GAME_PORT;
			}

		}
#else
		SessionPacket packet = new SessionPacket(data);
		SessionData response = packet.GetPacket();
#endif
		playerId = response.playerId;

		SetSessionMembers(response.result, response.members, response.endPoints);

		matchingState = State.MatchingEnded;
	}

	// ルーム作成の応答パケット受信処理.
	void CreateRoomResponse(MatchingResult result, int roomId)
	{
		Debug.Log("CreateRoomResponse");

		if (result == MatchingResult.Success) {
			isRoomOwner = true;
			joinedRoom.roomId = roomId;
			matchingState = State.RoomCreateRequested;

			RequestSearchRoom(roomId, -1);

			string str = "Create room is success [id:" + roomId + "]";
			Debug.Log(str);
		}
		else {
			matchingState = State.RoomCreateFailed;

			Debug.Log("Create room is failed.");
		}
	}

	// ルーム検索の応答パケット受信処理.
	void SearchRoomResponse(int roomNum, RoomInfo[] rooms)
	{
		m_rooms.Clear();

		for (int i = 0; i < roomNum; ++i) {
			RoomContent r = new RoomContent();

			r.roomId = rooms[i].roomId;
			r.roomName = rooms[i].name;
			r.members[0] = rooms[i].members;

			m_rooms.Add(rooms[i].roomId, r);

			string str = "Room name[" + i + "]:" + rooms[i].name + 
				" [id:" + rooms[i].roomId + ":" + rooms[i].members +"]";
			Debug.Log(str);
		}
	}

	// ルーム参加の応答パケット受信処理.
	void JoinRoomResponse(MatchingResult result, int roomId) 
	{
		Debug.Log("JoinRoomResponse");

		if (result == MatchingResult.Success) {
			joinedRoom.roomId = roomId;
			matchingState = State.RoomJoinRequested;

			string str = "Join room was success [id:" + roomId + "]";
			Debug.Log(str);
		}
		else {
			matchingState = State.RoomJoinFailed;
			RequestSearchRoom(-1, -1);
			Debug.Log("Join room was failed.");
		}
	}

	// ルームメンバー(セッションメンバー)の設定.
	void SetSessionMembers(MatchingResult result, int memberNum, EndPointData[] endPoints)
	{
		Debug.Log("StartSessionNotify");

		string str = "MemberNum:" + memberNum + " result:" + result;
		Debug.Log(str);

		m_memberNum = memberNum;

		for (int i = 0; i < memberNum; ++i) {

			MemberList member = new MemberList();

			member.node = i;
			member.endPoint = new IPEndPoint(IPAddress.Parse(endPoints[i].ipAddress), endPoints[i].port);

			sessionMembers[i] = member;

			str = "member[" + i + "]:" + member.endPoint.Address.ToString() + " : " + endPoints[i].port;
			Debug.Log(str);
		}
	}

	// エラー通知.
	private void NotifyError(string message)
	{
		GUIStyle style = new GUIStyle(GUI.skin.GetStyle("button"));
		style.normal.textColor = Color.white;
		style.fontSize = 25;
		
		float sx = 450;
		float sy = 200;
		float px = Screen.width / 2 - sx * 0.5f;
		float py = Screen.height / 2 - sy * 0.5f;
		
		if (GUI.Button (new Rect (px, py, sx, sy), message, style)) {
			matchingState = State.Idle;
		}
	}
}

