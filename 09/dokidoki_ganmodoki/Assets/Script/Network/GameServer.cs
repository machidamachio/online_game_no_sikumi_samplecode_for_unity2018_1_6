// ゲーム内の同期オブジェクトの調停
//
// ■プログラムの説明
// ゲーム内で同期をとるオブジェクトの調停を行うクラスです.
// 詳細は ReadMe_ganmodoki.txt を参照してください.
//

// 1台の端末で動作させる場合に定義します.
//#define UNUSE_MATCHING_SERVER

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


public class GameServer : MonoBehaviour {

	// ゲームサーバのバージョン.
	public const int			SERVER_VERSION = 1; 	

	// 通信モジュールのコンポーネント.
	Network						network_ = null;

	// セッション管理情報(ノード番号)とプレイヤーIDの関連付け.
	Dictionary<int, int>		m_nodes = new Dictionary<int, int>();

	// 部屋移動管理.
	Dictionary<string, int>		m_doors = new Dictionary<string, int>();

	// キーを所有しているキャラのマスクビット.
	// (のはずが本文中にシフトしているのでマスクになっていないです^^;).
	private static int 			KEY_MASK = NetConfig.PLAYER_MAX;

	// 参加プレイヤー数.
	private int					m_playerNum = 0;
	// 参加プレイヤーに対するマスクビット.
	private int 				m_currentPartyMask = 0;

	// 初期装備情報保存.
	Dictionary<int, int> 		m_equips = new Dictionary<int, int>();

	// 同期フラグ.
	private bool				m_syncFlag = false;
	
	// 未取得時のオーナーID.
	const string 				ITEM_OWNER_NONE = "";

	// アイテム取得状態.
	enum PickupState
	{
		None = 0, 				// 未取得.
		PickingUp,				// 取得中.
		Picked,					// 取得済.
		Dropping,				// 破棄中.
		Dropped,				// 破棄.
	}

	// アイテム状態を表す情報.
	private class ItemState
	{
		public string			itemId = "";					// ユニークな id.
		public PickupState		state = PickupState.None;		// アイテムの取得状況.
		public string 			ownerId = ITEM_OWNER_NONE;		// 所有者.
	}

	// アイテム管理テーブル.
	Dictionary<string, ItemState>		itemTable_;

	private int[]				prizeNum = new int[NetConfig.PLAYER_MAX];	

	// アイテム情報受信」フラグ.
	private bool				isRecvPrize = false;
	
	// マッチングサーバを使用しないときの接続確認用のキープアライブのTicker.
	private float[]				m_keepAlive = new float[NetConfig.PLAYER_MAX];

	// キープアライブタイムアウト.
	private const float 		KEEPALIVE_TIMEOUT = 10.0f;


	void Awake () {
	
		itemTable_ = new Dictionary<string, ItemState>();

		GameObject obj = new GameObject("Network-GameServer");
		network_ = obj.AddComponent<Network>();

		if (network_ != null) {
			DontDestroyOnLoad(network_);

			// 初期装備情報受信関数登録.
			network_.RegisterReceiveNotification(PacketId.Equip, this.OnReceiveEquipmentPacket);
			// アイテムデータ受信関数登録.
			network_.RegisterReceiveNotification(PacketId.ItemData, this.OnReceiveItemPacket);
			// アイテム使用受信関数登録.
			network_.RegisterReceiveNotification(PacketId.UseItem, this.OnReceiveReflectionPacket);
			// 移動情報受信関数登録.
			network_.RegisterReceiveNotification(PacketId.DoorState, this.OnReceiveDoorPacket);
			// モンスター発生受信関数登録.
			network_.RegisterReceiveNotification(PacketId.MonsterData, this.OnReceiveReflectionPacket);
			// HP情報受信関数登録.
			network_.RegisterReceiveNotification(PacketId.HpData, this.OnReceiveReflectionPacket);
			// ダメージ量情報受信関数登録.
			network_.RegisterReceiveNotification(PacketId.DamageData, this.OnReceiveReflectionPacket);
			// ダメージ通知情報受信関数登録.
			network_.RegisterReceiveNotification(PacketId.DamageNotify, this.OnReceiveReflectionPacket);
			// 召喚獣召喚受信関数登録.
			network_.RegisterReceiveNotification(PacketId.Summon, this.OnReceiveReflectionPacket);
			// ボス直接攻撃受信関数登録.
			network_.RegisterReceiveNotification(PacketId.BossDirectAttack, this.OnReceiveReflectionPacket);
			// ボス範囲攻撃受信関数登録.
			network_.RegisterReceiveNotification(PacketId.BossRangeAttack, this.OnReceiveReflectionPacket);
			// ボスクイック攻撃受信関数登録.
			network_.RegisterReceiveNotification(PacketId.BossQuickAttack, this.OnReceiveReflectionPacket);
			// ボス死亡通知受信関数登録.
			network_.RegisterReceiveNotification(PacketId.BossDead, this.OnReceiveReflectionPacket);
			// ご褒美取得情報受信関数登録.
			network_.RegisterReceiveNotification(PacketId.Prize, this.OnReceivePrizePacket);
			// チャット文章受信関数登録.
			network_.RegisterReceiveNotification(PacketId.ChatMessage, this.OnReceiveReflectionPacket);
		}

	}
	
	// Update is called once per frame
	void Update () {

		// イベントハンドリング.
		EventHandling();

		// 初期装備同期を監視.
		checkInitialEquipment();

		// 部屋移動の状態を監視.
		checkDoorOpen();

		// ご褒美のケーキ情報の受信チェック.
		checkReceivePrizePacket();

#if UNUSE_MATCHING_SERVER
		// 接続中の端末の動的チェック.
		// マッチングサーバを使用しない場合の接続端末を確定するためにチェックします.
		CheckConnection ();
#endif
	}

	// 待ち受け開始.
	public bool StartServer(int playerNum)
	{
		Debug.Log("Gameserver launched.");

		if (network_ == null) {
			Debug.Log("GameServer start fail.");
			return false; 
		}

		// 参加人数.
		m_playerNum = playerNum;

		// 参加プレイヤーのマスク.
		for (int i = 0; i < m_playerNum; ++i) {
			m_currentPartyMask |= 1 << i;

			prizeNum[i] = 0;
		}

		itemTable_.Clear();
		
		// ご褒美取得の初期化.
		for (int i = 0; i < prizeNum.Length; ++i) {
			prizeNum[i] = 0;
		}
		
		// キープアライブ受信情報を初期化.
		for (int i = 0; i < m_keepAlive.Length; ++i) {
			m_keepAlive[i] = 0.0f;
		}

		return network_.StartServer(NetConfig.GAME_SERVER_PORT, NetConfig.PLAYER_MAX, Network.ConnectionType.Reliable);
	}

	// 待ち受け終了.
	public void StopServer()
	{
		if (network_ == null) {
			Debug.Log("GameServer is not started.");

			return;
		}

		network_.StopServer();

		Debug.Log("Gameserver shotdown.");
	}

	// ================================================================ //

	// 装備情報のパケット受信通知関数.
	public void OnReceiveEquipmentPacket(int node, PacketId id, byte[] data)
	{
		EquipmentPacket packet = new EquipmentPacket(data);
		CharEquipment equip = packet.GetPacket();

		Debug.Log("[SERVER] Receive equipment packet [Account:" + equip.globalId + "][Shot:" + equip.shotType + "]");

		// キャラクターの装備を保存します.
		if (m_equips.ContainsKey(equip.globalId)) {
			m_equips[equip.globalId] = equip.shotType;
		}
		else {
			m_equips.Add(equip.globalId, equip.shotType);
		}

		// セッション管理情報とプレイヤーIDの関連付けを行います.
		if (m_nodes.ContainsKey(node) == false) {
			m_nodes.Add(node, equip.globalId);
		}

		m_syncFlag = true;

		// 実際のチェックは checkInitialEquipment で毎フレーム行います.
	}

	// 初期装備同期を監視.
	private void checkInitialEquipment() 
	{
		if (m_syncFlag == false) {
			return;
		}

		int equipFlag = 0;
		foreach (int index in m_equips.Keys) {
			equipFlag |= 1 << index;
		}

		// 受信したパケットデータからキャラクターのIDと装備の受信をチェックします.
		equipFlag &= m_currentPartyMask;
		if (equipFlag == m_currentPartyMask) {

			// 全員の武器選択情報が揃ったのでダンジョンへ突入します.
			GameSyncInfo sync = new GameSyncInfo();

			// ゲームサーバの乱数で種を決めます.
			TimeSpan ts = new TimeSpan(DateTime.Now.Ticks);
			double seconds = ts.TotalSeconds;
			sync.seed = (int) ((long)seconds - (long)(seconds/1000.0)*1000);

			Debug.Log("Seed: " + sync.seed);

			// 装備情報を格納します.
			sync.items = new CharEquipment[NetConfig.PLAYER_MAX];
			for (int i = 0; i < NetConfig.PLAYER_MAX; ++i) {
				sync.items[i].globalId = i;
				if (m_equips.ContainsKey(i)) {
					sync.items[i].shotType = m_equips[i];
				}
				else {
					sync.items[i].shotType = 0;
				}
			}
			
			if (network_ != null) {
				// 各端末へ通知
				GameSyncPacket syncPacket = new GameSyncPacket(sync);
				network_.SendReliableToAll<GameSyncInfo>(syncPacket);
			}

			// マッチングサーバを使用しない時のテスト用に初期装備情報をクリアしておきます.
			m_equips.Clear();
			Debug.Log("[SERVER] Clear equipment info.");

			m_syncFlag = false;
		}
	}

	// アイテム調停のパケット受信通知関数.
	public void OnReceiveItemPacket(int node, PacketId id, byte[] data)
	{
		ItemPacket packet = new ItemPacket(data);
		ItemData item = packet.GetPacket();
		
		string log = "[SERVER] ReceiveItemData " +
			"itemId:" + item.itemId +
				" state:" + item.state.ToString() +
				" ownerId:" + item.ownerId;
		Debug.Log(log);
		dbwin.console().print(log);

		PickupState state = (PickupState) item.state;
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
			dbwin.console().print(log);

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
			
			log = "[SERVER] Registerd item picked " +
				"itemId:" + itemId +
					" ownerId:" + istate.ownerId;
			Debug.Log(log);
			dbwin.console().print(log);

			// アイテムの取得状況に変更がある場合は端末へ通知します.
			SendItemState(istate);
		}
		else {
			log = "[SERVER] Already item pickedup " +
				"itemId:" + itemId +
					" state:" + istate.state.ToString() +
					" ownerId:" + istate.ownerId;
			dbwin.console().print(log);
			Debug.Log(log);
		}
	}

	// アイテム破棄時の調停関数.
	void MediateDropItem(string itemId, string charId)
	{
		string log = "";

		// アイテムがない場合は無視します.
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
		istate.ownerId = ITEM_OWNER_NONE;
		itemTable_[itemId] = istate;
		
		log = "[SERVER] Item dropped " +
			"itemId:" + itemId +
				" state:" + istate.state.ToString() +
				" ownerId:" + istate.ownerId;
		Debug.Log(log);

		// アイテムが破棄されたことをゲストへ通知します.
		SendItemState(istate);
	}

	// アイテムの状態の送信.
	bool SendItemState(ItemState state)
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
		network_.SendReliableToAll<ItemData>(packet);

		return true;
	}

	// ドアの状態パケット受信通知関数.
	public void OnReceiveDoorPacket(int node, PacketId id, byte[] data)
	{
		DoorPacket packet = new DoorPacket(data);
		CharDoorState door = packet.GetPacket();

		string log = "[SERVER] DoorPacket " +
				"keyId:" + door.keyId +
				" globalId:" + door.globalId +
				" is in:" + door.isInTrigger +
				" hasKey:" + door.hasKey;
		Debug.Log(log);

		int doorFlag = 0;
		if (m_doors.ContainsKey(door.keyId)) {
			// すでに誰かがのったドア.
			doorFlag = m_doors[door.keyId];
		}
		else {
			// 新規のドア.
			m_doors.Add(door.keyId, doorFlag);
		}

		// 受信したパケットデータからキャラクターのIDとカギの所有状態を更新します.
		if (door.isInTrigger) {
			doorFlag |= 1 << door.globalId;
			if (door.hasKey) {
				doorFlag |= 1 << KEY_MASK;
			}
		}
		else {
			doorFlag &= ~(1 << door.globalId);
			if (door.hasKey) {
				doorFlag &= ~(1 << KEY_MASK);
			}
		}

		log = "[SERVER] Door flag keyId:" + door.keyId + ":" + Convert.ToString(doorFlag, 2).PadLeft(5,'0');
		Debug.Log(log);

		// 状態を更新します.
		m_doors[door.keyId] = doorFlag;
		
		// 実際のチェックは checkDoorOpen で毎フレーム行います.
	}

	// ドアが開いたかチェック.
	private void checkDoorOpen()
	{
		Dictionary<string, int> doors = new Dictionary<string, int>(m_doors);

		foreach (string keyId in doors.Keys) {

			// 受信したパケットデータからキャラクターのIDとカギの所有を保存します.
			int doorFlag = m_doors[keyId];

			int mask = ((1 << KEY_MASK) | m_currentPartyMask);

			doorFlag &= mask;
			if (doorFlag == mask) {
				// カギを持って全員ドーナツに乗ったのでルーム移動を通知します.
				MovingRoom room = new MovingRoom();
				room.keyId = keyId;
	
				string log = "[SERVER] Room move Packet " + "keyId:" + room.keyId;
				Debug.Log(log);

				RoomPacket roomPacket = new RoomPacket(room);
				
				if (network_ != null) {
					network_.SendReliableToAll<MovingRoom>(roomPacket);
				}

				// 使い終わったのでクリアします.
				m_doors[keyId] = 0;
			}
		}
	}

	// ご褒美の取得数パケット受信通知関数.
	public void OnReceivePrizePacket(int node, PacketId id, byte[] data)
	{
		PrizePacket packet = new PrizePacket (data);
		PrizeData prize = packet.GetPacket ();

		int gid = getGlobalIdFromName(prize.characterId);

		string log = "[SERVER] Recv prize Packet[" + prize.characterId + "]:" + prize.cakeNum;
		Debug.Log(log);

		if (gid < 0) {
			return;
		}

		prizeNum[gid] = prize.cakeNum;

		// ケーキ取得情報の監視開始.
		isRecvPrize = true;
	}

	// キャラクターIDからグローバルIDを取得.
	private int getGlobalIdFromName(string name)
	{
		AccountData data = AccountManager.getInstance().getAccountData(name);

		return data.global_index;
	}

	// ケーキ取得結果の受信完了チェック.
	private void checkReceivePrizePacket()
	{
		if (isRecvPrize == false) {
			return;
		}

		for (int i = 0; i < NetConfig.PLAYER_MAX; ++i) {
			int node = network_.GetClientNode(i);
			if (network_.IsConnected(node) && prizeNum[i] < 0) {
				// まだそろっていない.
				return;
			}
		}

		PrizeResultData data = new PrizeResultData ();

		// 各クライアントに取得結果を通知.
		data.cakeDataNum = NetConfig.PLAYER_MAX;
		data.cakeNum = new int[NetConfig.PLAYER_MAX];
		for (int i = 0; i < data.cakeDataNum; ++i) {
			data.cakeNum[i] = prizeNum[i];
		}

		PrizeResultPacket packet = new PrizeResultPacket(data);
		network_.SendReliableToAll(packet);

		isRecvPrize = false;
	}

	// 各自で所有する同期オブジェクトのリフレクター関数.
	public void OnReceiveReflectionPacket(int node, PacketId id, byte[] data)
	{
		if (network_ != null) {
			network_.SendReliableToAll(id, data);
		}
	}


	// ================================================================ //

	// クライアントとの切断関数.
	private void DisconnectClient(int node)
	{
		Debug.Log("[SERVER]DisconnectClient");
		
		network_.Disconnect(node);

		if (m_nodes.ContainsKey(node) == false) {
			return;
		}

		// 現在接続中のクライアントのフラグを落とす.
		int gid = m_nodes[node];
		m_currentPartyMask &= ~(1 << gid);
	}

	// ================================================================ //

	// イベントハンドラー.
	public void EventHandling()
	{
		NetEventState state = network_.GetEventState();

		if (state == null) {
			return;
		}
				
		switch (state.type) {
		case NetEventType.Connect:
			Debug.Log("[SERVER]NetEventType.Connect");
			break;
			
		case NetEventType.Disconnect:
			Debug.Log("[SERVER]NetEventType.Disconnect");
			DisconnectClient(state.node);
			break;
		}
	}

	// ================================================================ //

#if UNUSE_MATCHING_SERVER
	private void CheckConnection()
	{
		int[] nodeList = new int[NetConfig.PLAYER_MAX];

		for (int i = 0; i < nodeList.Length; ++i) {
			nodeList[i] = -1;
		}

		foreach (int node in m_nodes.Keys) {
			int gid = m_nodes[node];

			nodeList[gid] = node;
		}


		for (int i = 0; i < NetConfig.PLAYER_MAX; ++i) {

			int node = nodeList[i];
			if (node >= 0) {
				m_keepAlive[i] = 0.0f;
			}
			else {
				m_keepAlive[i] += Time.deltaTime;
			}

			int mask = m_currentPartyMask & (1 << i);
			if (mask != 0 && m_keepAlive[i] > KEEPALIVE_TIMEOUT) {
				Debug.Log("[SERVER] KeepAlive timeout [" + i + "]:" + node);
				m_currentPartyMask &= ~(1 << i);

				Debug.Log("Current player mask:" + Convert.ToString(m_currentPartyMask, 2).PadLeft(4,'0'));
			}
		}
	}
#endif
}
