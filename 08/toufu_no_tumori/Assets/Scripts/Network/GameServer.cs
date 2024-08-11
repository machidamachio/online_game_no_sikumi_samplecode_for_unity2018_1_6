// ゲーム内の同期オブジェクトの調停
//
// ■プログラムの説明
// ゲーム内で同期をとるオブジェクトの調停を行うクラスです.
// MonoBehaviour クラスを継承していますので Awake() 関数で初期化をおこないます.
// Awake() 関数内で同期をとるオブジェクトに関するパケットを受信した際に呼び出される関数を登録しています.
// ゲーム側の通信とは別に処理を行うためリスニングソケットの生成、接続処理を行う StartServer() 関数を
// ゲームとは別に起動します.
// アイテムの調停は OnReceiveItemPacket() 関数で行います.
// アイテムは取得時は MediatePickupItem() 関数でアイテムの調停します.
// アイテムは破棄時は MediateDropItem() 関数でアイテムの調停をします.
// 引越しの同期、遊びに行くイベント、チャット、ゲーム開始の同期は OnReceiveReflectionData() 関数で行います.
// OnReceiveItemPacket() 関数は調停を行わないオブジェクトの同期を行う関数です.
// 自分や通信相手だけが所有するオブジェクト等は単に相手に情報を伝えるだけでよいのでそのまま転送します.
// OnEventHandling() 関数で接続、切断のイベントを処理しています.
// 接続時に SendGameSyncInfo() 関数でゲーム前の同期情報の送信をしています.
//

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

// アイテム取得状態.
enum PickupState
{
	Growing = 0, 			// 発生中.
	None, 					// 未取得.
	PickingUp,				// 取得中.
	Picked,					// 取得済.
	Dropping,				// 破棄中.
	Dropped,				// 破棄.
}

public class GameServer : MonoBehaviour {

	// 未取得時のオーナーID.
	private const string 	ITEM_OWNER_NONE = "";

	// ゲームサーバで使用するポート番号.
	private const int 		serverPort = 50764;

	// ゲームサーバのバージョン.
	public const int		SERVER_VERSION = 1; 	
		


	// 通信モジュールのコンポーネント.
	Network					network_ = null;

	// アイテムの状態.
	private struct ItemState
	{
		public string			itemId;			// ユニークな id.
		public PickupState		state;			// アイテムの取得状況.
		public string 			ownerId;		// 所有者.
	}

	Dictionary<string, ItemState>		itemTable_;


	void Awake() {
	
		itemTable_ = new Dictionary<string, ItemState>();

		GameObject go = new GameObject("ServerNetwork");
		network_ = go.AddComponent<Network>();
		if (network_ != null) {
			// アイテムデータ受信関数登録.
			network_.RegisterReceiveNotification(PacketId.ItemData, this.OnReceiveItemPacket);
			// 引越しイベント受信関数登録.
			network_.RegisterReceiveNotification(PacketId.Moving, this.OnReceiveReflectionData);
			// 遊びに行くイベント受信関数登録.
			network_.RegisterReceiveNotification(PacketId.GoingOut, this.OnReceiveReflectionData);
			// チャット文章受信関数登録.
			network_.RegisterReceiveNotification(PacketId.ChatMessage, this.OnReceiveReflectionData);
			// ゲーム開始前情報受信関数登録.
			network_.RegisterReceiveNotification(PacketId.GameSyncInfoHouse, this.OnReceiveReflectionData);

			// イベントハンドラ.
			network_.RegisterEventHandler(OnEventHandling);
		}
	}
	
	// Update is called once per frame
	void Update () {
	}

	// 待ち受け開始.
	public bool StartServer()
	{
		return network_.StartServer(NetConfig.SERVER_PORT, Network.ConnectionType.TCP);
	}

	// 待ち受け終了.
	public void StopServer()
	{
		network_.StopServer();
	}

	// アイテム調停のパケット受信通知関数.
	public void OnReceiveItemPacket(PacketId id, byte[] data)
	{
		ItemPacket packet = new ItemPacket(data);
		ItemData item = packet.GetPacket();

		PickupState state = (PickupState) item.state;

		string log = "[SERVER] ReceiveItemData " +
						"itemId:" + item.itemId +
						" state:" + state.ToString() +
						" ownerId:" + item.ownerId;
		Debug.Log(log);

		switch (state) {
		case PickupState.PickingUp:
			MediatePickupItem(item.itemId, item.ownerId);
			break;

		case PickupState.Dropping:
			MediateDropItem(item.itemId, item.ownerId);
			break;

		default:
			break;
		}
	}

	// アイテム取得時の調停関数.
	void MediatePickupItem(string itemId, string charId)
	{
		ItemState istate;
		string log = "";

		int count = 1;
		foreach (ItemState state in itemTable_.Values) {
			log = "[SERVER] current item[" + count +"]" + 
					"itemId:" + state.itemId + 
					" state:" + state.state.ToString() +
					" ownerId:" + state.ownerId;
			Debug.Log(log);
			++count;
		}

		if (itemTable_.ContainsKey(itemId) == false) {
			// 見つからなかったので新規アイテムとします.
			istate = new ItemState();

			istate.itemId = itemId;
			istate.state = PickupState.Picked;
			istate.ownerId = charId;
			// 新規アイテムとして登録します.
			itemTable_.Add(itemId, istate);

			log = "[SERVER] Unregisterd item pickedup " +
					"itemId:" + itemId +
					" ownerId:" + istate.ownerId;
			Debug.Log(log);

			// アイテムの取得状況に変更がある場合は端末へ通知します.
			SendItemState(istate);
			
			return;
		}

		istate = itemTable_[itemId];

		// 他のキャラクターが取得していないか確認します.
		if (istate.state == PickupState.None) {
			// このアイテムは取得可能です.
			istate.state = PickupState.Picked;
			istate.ownerId = charId;
			// アイテム情報を更新します.
			itemTable_[itemId] = istate;

			log = "[SERVER] Registerd item pickedup " +
					"itemId:" + itemId +
					" ownerId:" + istate.ownerId;
			Debug.Log(log);

			// アイテムの取得状況に変更がある場合は端末へ通知します
			SendItemState(istate);
		}
		else {
			log = "[SERVER] Already item pickedup " +
					"itemId:" + istate.itemId + "(" + itemId + ")"+
					" state:" + istate.state.ToString() +
					" ownerId:" + istate.ownerId;
			Debug.Log(log);
		}
	}


	// アイテム破棄時の調停関数.
	void MediateDropItem(string itemId, string charId)
	{
		string log = "";

		// アイテムがない場合は無視します
		if (itemTable_.ContainsKey(itemId) == false) {
			return;
		}

		ItemState istate = itemTable_[itemId];
		if (istate.state != PickupState.Picked ||
		    istate.ownerId != charId) {
			// このアイテムは破棄できない.
			log = "[SERVER] Illegal item drop state " +
					"itemId:" + itemId +
					" state:" + istate.state.ToString() +
					" ownerId:" + istate.ownerId;
			Debug.Log(log);
			return;		
		}

		// アイテムを破棄します.
		istate.state = PickupState.None;
		istate.ownerId = charId;
		itemTable_[itemId] = istate;
		
		log = "[SERVER] Item dropped " +
				"itemId:" + itemId +
				" state:" + istate.state.ToString() +
				" ownerId:" + istate.ownerId;
		Debug.Log(log);

		// アイテムが破棄されたことをゲストへ通知します
		SendItemState(istate);

		istate.ownerId = ITEM_OWNER_NONE;
		itemTable_[itemId] = istate;
	}

	// アイテムの状態の送信.
	private bool SendItemState(ItemState state)
	{
		// アイテム取得の応答をします.
		ItemData data = new ItemData();
		
		data.itemId = state.itemId;
		data.ownerId = state.ownerId;
		data.state = (state.state == PickupState.None)? (int)PickupState.Dropped : (int)state.state;
	
		string log = "[SERVER] Send Item State" +
				"itemId:" + data.itemId +
				" state:" + data.state.ToString() +
				" ownerId:" + data.ownerId;
		Debug.Log(log);

		ItemPacket packet = new ItemPacket(data);
		network_.SendReliable<ItemData>(packet);

		return true;
	}

	// 各自で所有する同期オブジェクトのリフレクター関数.
	public void OnReceiveReflectionData(PacketId id, byte[] data)
	{
		Debug.Log("[SERVER]OnReceiveReflectionData");
		network_.SendReliableToAll(id, data);
	}

	// ゲーム前の同期情報をの送信.
	private void SendGameSyncInfo()
	{
		Debug.Log("[SERVER]SendGameSyncInfo start");

		SyncGameData data = new SyncGameData();

		data.version = SERVER_VERSION;
		data.itemNum = itemTable_.Count;
		data.items = new ItemData[itemTable_.Count];

		// サーバからは引越し情報は送らない.
		data.moving = new MovingData();
		data.moving.characterId = "";
		data.moving.houseId = "";
		data.moving.moving = false;

		int index = 0;
		foreach (ItemState state in itemTable_.Values) {
			data.items[index].itemId = state.itemId;
			data.items[index].ownerId = state.ownerId;
			data.items[index].state = (int)state.state;
			string log = "[SERVER] Item sync[" + index + "]" +
					"itemId:" + data.items[index].itemId +
					" state:" + data.items[index].state +
					" ownerId:" + data.items[index].ownerId;
			Debug.Log(log);
			++index;
		}

		Debug.Log("[SERVER]SendGameSyncInfo end");

		SyncGamePacket packet = new SyncGamePacket(data);
		network_.SendReliable<SyncGameData>(packet);
	}

	// クライアントとの切断関数.
	private void DisconnectClient()
	{
		Debug.Log("[SERVER]DisconnectClient");

		network_.Disconnect();
	}


	// test

	void OnGUI()
	{
	}

	// ================================================================ //
	
	// イベントハンドラー.
	public void OnEventHandling(NetEventState state)
	{
		switch (state.type) {
		case NetEventType.Connect:
			Debug.Log("[SERVER]NetEventType.Connect");
			SendGameSyncInfo();
			break;

		case NetEventType.Disconnect:
			Debug.Log("[SERVER]NetEventType.Disconnect");
			DisconnectClient();
			break;
		}
	}

}
