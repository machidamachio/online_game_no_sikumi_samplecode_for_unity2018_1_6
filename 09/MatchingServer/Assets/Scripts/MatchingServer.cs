// マッチングサーバー
//
// ■プログラムの説明
// 簡易的なマッチングを行うクラスの定義です.
// Unity の初期化関数である Start() 関数でサーバー処理の起動、クライアントからの要求パケットの処理
// をする関数の登録をしています.
// OnReceiveMatchingRequest() 関数でマッチングサーバーへの要求パケットを処理しています.
// CreateRoom() 関数でルーム作成要求のパケットを処理します.
// SearchRoom() 関数でルーム検索要求のパケットを処理します.
// JoinRoom() 関数でルーム入室要求のパケットを処理します.
// StartSession() 関数でゲーム開始通知のパケットを処理します.
// DrawRoomInformation() 関数でマッチングサーバーでメンバーを受け付けているルームの情報を表示しています.
//

using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Net;

public class MatchingServer : MonoBehaviour
{
	// 同時にマッチングを行えるルーム数の最大値.
	private const int 	maxRoomNum = 4;

	// 1ルームの最大マッチング人数.
	private const int 	maxMemberNum = NetConfig.PLAYER_MAX;

	// マッチングサーバーが起動していることを表示するためのカウンター.
	private int			counter = 0;

	// マッチングを行うための情報.
	private class RoomContent
	{
		public int 		node = -1;

		public int 		roomId = -1;

		public string	name = "";

		public int		level = 0;

		public bool		isClosed = false;

		public float	elapsedTime = 0.0f;

		public int[]	members = Enumerable.Repeat(-1, maxMemberNum).ToArray();

	}

	// メンバーの情報.
	private class MemberList
	{
		public int			node = -1;

		public int			roomId = -1;

		public IPEndPoint	endPoint = null;
	}

	// ルームに設定されたレベルの文字列表記.
	private string[] 	levelString = new string[] {
		"指定なし", "かんたん", "ふつう", "むずかしい"
	};

	// ルーム情報管理.
	private Dictionary<int, RoomContent> 	rooms_ = new Dictionary<int, RoomContent>();

	// ルームメンバー管理.
	private Dictionary<int, MemberList> 	m_members = new Dictionary<int, MemberList>();


	private Network 	network_ = null;
	
	private int			roomIndex = 0;

	private float			timer = 0.0f;

	// マッチングサーバーの動作状態.
	private enum State
	{
		Idle = 0,
		MatchingServer,
		RoomCreateRequested,
		RoomSearchRequested,
		RoomJoinRequested,
		StartSessionRequested,
		StartSessionNotified,
		MatchingEnded,
	}


	// Use this for initialization
	void Start()
	{
		GameObject obj = new GameObject("Network");
		network_ = obj.AddComponent<Network>();


		if (network_ != null) {
			network_.RegisterReceiveNotification(PacketId.MatchingRequest, OnReceiveMatchingRequest);
			network_.StartServer(NetConfig.MATCHING_SERVER_PORT, maxRoomNum);

			network_.RegisterEventHandler(OnEventHandling);
		}
	}

	// Update is called once per frame
	void Update()
	{
		// ルームメンバーがいなくなったルームの削除.
		Dictionary<int, RoomContent> rooms = new Dictionary<int, RoomContent>(rooms_);

		foreach (RoomContent room in rooms.Values) {

			if (room.isClosed) {
				rooms_[room.roomId].elapsedTime += Time.deltaTime;
				if (rooms_[room.roomId].elapsedTime > 3.0f) {
					// ゲーム開始したルームを削除します.
					DisconnectRoomMembers(room);
					rooms_.Remove(room.roomId);
				}
			}
			else {
				int count = 0;
				for (int i = 0; i < room.members.Length; ++i) {
					if (room.members[i] != -1) {
						++count;
					}
				}

				if (count == 0) {
					// 切断によってルームがなくなった.
					rooms_.Remove(room.roomId);
				}
			}
		}

		timer += Time.deltaTime;

		++counter;
	}


	void OnGUI()
	{
		int px = 10;
		int py = 40;
		int sx = 400;
		int sy = 100;

		Rect rect = new Rect(px, 5, 400, 100);

		string label = "マッチングサーバ起動中 ";
		for (int i = 0; i < (counter % 600) / 60; ++i) {
			label += ".";
		}

		// ホスト名を取得します.
		string serverAddress = GetServerIPAddress();

		label += "\n[IPアドレス:" + serverAddress + "][ポート:" + NetConfig.MATCHING_SERVER_PORT + "][接続数" + m_members.Count + "] ";

		GUI.Label(rect, label);

		// 生成されているルーム情報を表示します.
		foreach (RoomContent room in rooms_.Values) {
			DrawRoomInformation(new Rect(px, py, sx, sy),  room);
			py += sy + 10;
		}
	}

	// 生成されているルーム情報を表示.
	void DrawRoomInformation(Rect rect, RoomContent room)
	{
		string infoText = "";
		
		infoText += "部屋名[" + room.roomId + "]: " + room.name + "\t\t";
		
		int count = 0;
		string epStr = "";
		for (int i = 0; i < maxMemberNum; ++i) {
			if (room.members[i] != -1) {
				IPEndPoint ep = network_.GetEndPoint(room.members[i]);

				if (ep != null) {
					epStr += "メンバー" + (i+1).ToString() + "のアドレス: " + ep.Address + ":" + ep.Port + "\n"; 
					++count;
				}
			}
		}
		
		infoText += "人数: " + count + "\t\tレベル: " + levelString[room.level] + "\n\n";
		infoText += epStr;
		
		GUI.TextField(new Rect(rect.x, rect.y, rect.width, rect.height), infoText);

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

	//
	// マッチングサーバ側の処理.
	//


	// ルーム内のメンバーとの切断.
	void DisconnectRoomMembers(RoomContent room)
	{
		foreach (int node in room.members) {
			if (node != -1) {
				network_.Disconnect(node);
				m_members.Remove(node);
			}
		}
	}


	//
	// パケット受信処理.
	//
	
	// マッチングサーバーへの要求パケットを受信.
	void OnReceiveMatchingRequest(int node, byte[] data)
	{
		MatchingRequestPacket packet = new MatchingRequestPacket(data);
		MatchingRequest request = packet.GetPacket();

		string str = "ReceiveMatchingRequest:" + request.request;
		Debug.Log(str);

		if (request.version != NetConfig.SERVER_VERSION) {
			Debug.Log("Invalid request version.");
			// 異なるバージョンのヘッダは破棄します.
			Debug.Log("Current ver:" + NetConfig.SERVER_VERSION + " Req ver:" + request.version);
			return;
		}

		// 要求内容ごとの処理を実行します.
		switch (request.request) {
			case MatchingRequestId.CreateRoom: {	// ルーム作成要求.
				CreateRoom(node, request.name, request.level);
			}	break;

			case MatchingRequestId.SearchRoom: {	// ルーム検索要求.
				SearchRoom(node, request.roomId, request.level);
			}	break;

			case MatchingRequestId.JoinRoom: {		// ルーム入室要求.
				JoinRoom(node, request.roomId);
			}	break;

			case MatchingRequestId.StartSession: {	// ゲーム開始通知.
				StartSession(node, request.roomId);
			}	break;
		}
	}
	
	// ルーム作成.
	void CreateRoom(int node, string name, int level)
	{
		Debug.Log("ReceiveCreateRoomRequest");

		MatchingResponse response = new MatchingResponse();


		response.request = MatchingRequestId.CreateRoom;

		if (rooms_.Count < maxRoomNum) {

			// ルーム情報を作成し、ルーム情報へ追加します.
			RoomContent room = new RoomContent();

			room.roomId = roomIndex;

			room.name = name;

			room.level = level;

			// 自分自身を先頭に設定.
			room.members[0] = node;
			
			m_members[node].roomId = roomIndex;

			rooms_.Add(roomIndex, room);
			++roomIndex;


			response.result = MatchingResult.Success;
			response.roomId = room.roomId;
			response.name = "";

			string str = "Request node:" + node + " Created room id:" + response.roomId;
			Debug.Log(str);
		}
		else {
			// このマッチングサーバーで作成できる最大のルーム数を超えています.
			response.result = MatchingResult.RoomIsFull;
			response.roomId = -1;
			Debug.Log("Create room failed.");
		}

		MatchingResponsePacket packet = new MatchingResponsePacket(response);
		
		network_.SendReliable<MatchingResponse>(node, packet);
	}

	// ルーム検索.
	void SearchRoom(int node, int roomId, int level)
	{
		Debug.Log("ReceiveSearchRoomPacket");

		SearchRoomResponse response = new SearchRoomResponse();

		response.rooms = new RoomInfo[rooms_.Count];


		int index = 0;
		foreach (RoomContent r in rooms_.Values) {
			// 作成されているルームから検索要求にあったルームを探します.
			if (roomId == r.roomId ||
			    roomId != r.roomId && level >= 0 && (level == 0 || level == r.level)) {
				response.rooms[index].roomId = r.roomId;
				response.rooms[index].name = r.name;

				int count  = 0;
				for (int i = 0; i < r.members.Length; ++i) {
					if (r.members[i] != -1) {
						++count;
					}
				}
				response.rooms[index].members = count;
				
				++index;
			}
		}

		response.roomNum = index;


		SearchRoomPacket packet = new SearchRoomPacket(response);

		network_.SendReliable<SearchRoomResponse>(node, packet);

		
		string str = "Request node:" + node + " Created room num:" + response.roomNum;
		Debug.Log(str);
		for (int i = 0; i < response.roomNum; ++i) {
			str = "Room name[" + i + "]:" +  response.rooms[i].name + 
				  " [id:" + response.rooms[i].roomId + ":" + response.rooms[i].members +"]";
			Debug.Log(str);
		}
	}

	// ルーム入室.
	void JoinRoom(int node, int roomId)
	{
		Debug.Log("ReceiveJoinRoomPacket");

		MatchingResponse response = new MatchingResponse();

		response.roomId = -1;
		response.request = MatchingRequestId.JoinRoom;

		int memberNum = 0;
		if (rooms_.ContainsKey(roomId) == true) {
			RoomContent room = rooms_[roomId];

			m_members[node].roomId = roomId;
			
			response.result = MatchingResult.MemberIsFull;
			for (int i = 0; i < maxMemberNum; ++i) {
				if (room.members[i] == -1) {
					// 空きがあった.
					room.members[i] = node;
					rooms_[roomId] = room;
					response.result = MatchingResult.Success;
					response.roomId = roomId;
					response.name = room.name;
					break;
				}
			}

			// 定員チェック.
			for (int i = 0; i < room.members.Length; ++i) {
				if (room.members[i] != -1) {
					++memberNum;
				}
			}
		}
		else {
			// 入室要求されたルームが消失してしまった.
			Debug.Log("JoinRoom failed.");
			response.result = MatchingResult.RoomIsGone;
			response.name = "";
		}
		
		MatchingResponsePacket packet = new MatchingResponsePacket(response);	
		
		network_.SendReliable<MatchingResponse>(node, packet);
	}

	// ゲームを開始するセッションの情報を通知.
	void StartSession(int node, int roomId)
	{
		string str = "ReceiveStartSessionRequest[roomId:" + roomId + "]";
		Debug.Log(str);
		
		SessionData response = new SessionData();

		RoomContent room = null;
		if (rooms_.ContainsKey(roomId) == true) {
			// ルームメンバー全員の情報をそれぞれに送信します.
			// それぞれのメンバーはこの情報をもとにメンバー同士を接続します	.
			room = rooms_[roomId];

			response.endPoints = new EndPointData[maxMemberNum];
			
			int index = 0;
			for (int i = 0; i < maxMemberNum; ++i) {
				if (room.members[i] != -1) {
					
					IPEndPoint ep = network_.GetEndPoint(room.members[i]) as IPEndPoint;
					response.endPoints[index].ipAddress = ep.Address.ToString();
					response.endPoints[index].port = NetConfig.GAME_PORT;
					++index;
				}	
			}
			
			response.members = index;
			response.result = MatchingResult.Success;
		}
		else {
			// ゲーム開始要求されたルームが消失してしまった.
			response.result = MatchingResult.RoomIsGone;
		}

		if (room != null) {

			rooms_[roomId].isClosed = true;

			str = "Request room id: " + roomId + " MemberNum:" + response.members + " result:" + response.result;
			Debug.Log(str);

			for (int i = 0; i < response.members; ++i) {
				str = "member[" + i + "]" + ":" + response.endPoints[i].ipAddress + ":" + response.endPoints[i].port;
				Debug.Log(str);
			}

			// 作成されたルームメンバー全員分の情報を全メンバーに送信します.
			int index = 0;
			for (int i = 0; i < room.members.Length; ++i) {

				int target = room.members[i];

				if (target != -1) {
						
					response.playerId = index;

					SessionPacket packet = new SessionPacket(response);
					
					network_.SendReliable<SessionData>(target, packet);

					++index;
				}
			}


		}
	}

	// イベントハンドラー.
	public void OnEventHandling(int node, NetEventState state)
	{
		string str = "Node:" + node + " type:" + state.type.ToString() + " State:" + state.type + "[" + state.result + "]";
		Debug.Log("OnEventHandling called");
		Debug.Log(str);
		
		switch (state.type) {
		case NetEventType.Connect: {
			MemberList member = new MemberList();
			
			member.node = node;
			member.endPoint = network_.GetEndPoint(node);

			m_members.Add(node, member);
		} 	break;
			
		case NetEventType.Disconnect: {

            if (m_members.ContainsKey(node)) {
				
				// 切断時にルーム内の情報の整合をとるためにメンバー情報を削除します.
				int roomId = m_members[node].roomId;
				if (rooms_.ContainsKey(roomId)) {
					for (int i = 0; i < rooms_[roomId].members.Length; ++i) {
						if (rooms_[roomId].members[i] == node) {
							rooms_[roomId].members[i] = -1;
							break;
						}
					}
				}

                m_members.Remove(node);
            }			
		}	break;
			
		}
	}
}

