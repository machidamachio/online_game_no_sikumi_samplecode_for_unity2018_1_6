using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GameObjectExtension;

// ドア.
// ルーム間ドア、またはフロアー間のドア.
public class DoorControl : MonoBehaviour {

	public Map.RoomIndex	room_index;		// このドアが置かれている部屋.

	public bool is_unlocked = false;		// ドアのカギが開いてる？.
	
	public int KeyType;						// このドアを開けるカギの色.

	public DoorControl		connect_to;		// つながっているドア（となりのルームのドア）.

	public Map.EWSN			door_dir;

	public enum TYPE {

		NONE = -1,

		ROOM = 0,			// ルーム間のドア.
		FLOOR,				// フロアー間のドア.

		NUM,
	};

	public TYPE		type = TYPE.NONE;

	protected 	RoomController		room;				// このドアがあるルーム.
	protected	string				keyItemName;		// link to the key that can unlock this.

	protected	List<int>		entered_players = new List<int>();		// イベントボックス内にいるプレイヤーの local_index.

	// ================================================================ //

	protected enum STEP {

		NONE = -1,

		SLEEP = 0,				// スリープ（イベントボックスが反応しないように）.
		WAIT_ENTER,				// プレイヤー全員がイベントボックスに入るのを待ってる.

		EVENT_IN_ACTION,		// ルーム移動イベント実行中.

		WAIT_LEAVE,				// プレイヤー全員がイベントボックスから出るのを待ってる.

		NUM,
	};
	protected Step<STEP>	step = new Step<STEP>(STEP.NONE);

	public Network			m_network = null;

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
		this.step.set_next(STEP.WAIT_ENTER);

		this.setCreamVisible(true);

		// Networkクラスのコンポーネントを取得.
		GameObject obj = GameObject.Find("Network");
		if(obj != null) {
			m_network = obj.GetComponent<Network>();
		}
	}
	
	void	Update()
	{
		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			case STEP.WAIT_ENTER:
			{
				if(this.is_unlocked) {
					if(this.keyItemName == null) {
						if(this.entered_players.Count >= PartyControl.get().getPlayerCount()) {

							this.step.set_next(STEP.EVENT_IN_ACTION);
							PartyControl.get().clearDoorState(this.keyItemName);
						}
					}
					else {
						if(this.entered_players.Count > 0 &&
							PartyControl.get().isDoorOpen(this.keyItemName)) {

							this.step.set_next(STEP.EVENT_IN_ACTION);
							PartyControl.get().clearDoorState(this.keyItemName);
						}
					}
				}
			}
			break;

			case STEP.EVENT_IN_ACTION:
			{
			}
			break;

			case STEP.WAIT_LEAVE:
			{
				if(this.entered_players.Count == 0) {

					this.step.set_next(STEP.WAIT_ENTER);
				}
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {

				case STEP.SLEEP:
				{
					if(this.moji_effect != null) {

						this.moji_effect.gameObject.destroy();

						this.moji_effect = null;
					}
				}
				break;

				case STEP.WAIT_ENTER:
				{
					if(this.is_unlocked) {

						if(this.moji_effect == null) {
	
							this.moji_effect = EffectRoot.get().createDoorMojisEffect(this.transform.position);
						}
					}
				}
				break;

				case STEP.WAIT_LEAVE:
				{
					if(this.moji_effect != null) {
	
						this.moji_effect.gameObject.destroy();
						this.moji_effect = null;
					}
				}
				break;

				case STEP.EVENT_IN_ACTION:
				{
					TransportEvent	transport_event = EventRoot.get().startEvent<TransportEvent>();

					if(transport_event != null) {

						transport_event.setDoor(this);

					} else {

						Debug.LogWarning("Can't begin Transport Event");
					}

					// 『次の部屋』をセットしておく.
					if(this.connect_to != null) {

						PartyControl.get().setNextRoom(this.connect_to.room);
					}
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			case STEP.WAIT_ENTER:
			{
			}
			break;

		}

		// ---------------------------------------------------------------- //

	}

	// プレイヤーがイベントボックスにはいったとき.
	// （ドア移動イベントのときに明示的に呼ぶ）.
	public void		enterPlayer(int player_local_index)
	{
		// イベントボックスに入ったプレイヤーをリストに追加する.
		do {

			if(player_local_index < 0) {

				break;
			}

			if(this.entered_players.Contains(player_local_index)) {

				break;
			}

			this.entered_players.Add(player_local_index);

		} while(false);
	}

	// プレイヤーがイベントボックスから出たとき.
	// （ドア移動イベントのときに明示的に呼ぶ）.
	public void		leavePlayer(int player_local_index)
	{
		// イベントボックスから出たプレイヤーをリストから取り除く.
		do {

			if(player_local_index < 0) {

				break;
			}

			if(!this.entered_players.Contains(player_local_index)) {

				break;
			}

			this.entered_players.Remove(player_local_index);

		} while(false);
	}

	// トリガーにヒットした瞬間だけよばれるメソッド.
	void	OnTriggerEnter(Collider other)
	{
		// イベントボックスに入ったプレイヤーをリストに追加する.
		do {

			if(other.tag != "Player") {

				break;
			}

			chrController	player = other.gameObject.GetComponent<chrController>();

			if(player == null) {

				break;
			}

			if(player.local_index < 0) {

				break;
			}

			if(this.entered_players.Contains(player.local_index)) {

				break;
			}

			this.entered_players.Add(player.local_index);

			// ゲームサーバへ通知.
			if (this.step.get_current() == STEP.WAIT_ENTER &&
				player.global_index == GlobalParam.get().global_account_id) {

				CharDoorState door = new CharDoorState();
				door.globalId = player.global_index;
				door.keyId = (this.keyItemName != null)? this.keyItemName : "NONE";
				door.isInTrigger = true;
				door.hasKey = (this.keyItemName != null)? PartyControl.getInstance().hasKey(player.local_index, door.keyId) : true;

				string log = "DoorId:" + door.keyId + " trigger:" + door.isInTrigger + " hasKey:" + door.hasKey;
				Debug.Log(log);

				DoorPacket packet = new DoorPacket(door);
				if (m_network != null) {
					int server_node = m_network.GetServerNode();
					m_network.SendReliable<CharDoorState>(server_node, packet);
				} else {
					PartyControl.get().cmdMoveRoom(door.keyId);
				}
			}

		} while(false);
	}

	// トリガーから何かがヒットした瞬間だけよばれるメソッド.
	void	OnTriggerExit(Collider other)
	{
		// イベントボックスから出たプレイヤーをリストから取り除く.
		do {

			if(other.tag != "Player") {

				break;
			}

			chrController	player = other.gameObject.GetComponent<chrController>();

			if(player == null) {

				break;
			}

			if(!this.entered_players.Contains(player.local_index)) {

				break;
			}

			this.entered_players.Remove(player.local_index);

			// ゲームサーバへ通知.
			if (player.global_index == GlobalParam.get().global_account_id) {
				CharDoorState door = new CharDoorState();
				door.globalId = player.global_index;
				door.keyId = (this.keyItemName != null)? this.keyItemName : "NONE";
				door.isInTrigger = false;
				door.hasKey = PartyControl.getInstance().hasKey(player.local_index, door.keyId);

				string log = "DoorId:" + door.keyId + " trigger:" + door.isInTrigger + " hasKey:" + door.hasKey;
				Debug.Log(log);

				DoorPacket packet = new DoorPacket(door);
				if (m_network != null) {
					int serer_node = m_network.GetServerNode();
					m_network.SendReliable<CharDoorState>(serer_node, packet);
				} else {
					PartyControl.get().clearDoorState(door.keyId);
				}
			}

		} while(false);
	}

	// ================================================================ //

	// EVENT_IN_ACTION を開始する.
	public void		beginEventInAction()
	{
		this.step.set_next(STEP.EVENT_IN_ACTION);
	}

	// WAIT_LEAVE を開始する.
	public void		beginWaitLeave()
	{
		this.step.set_next(STEP.WAIT_LEAVE);
	}

	// WAIT_ENTER を開始する.
	public void		beginWaitEnter()
	{
		this.step.set_next(STEP.WAIT_ENTER);
	}

	// スリープする.
	public void		beginSleep()
	{
		this.step.set_next(STEP.SLEEP);
	}

	// スリープを解除する.
	public void		endSleep()
	{
		if(this.step.get_current() == STEP.SLEEP && this.step.get_next() == STEP.NONE) {

			this.step.set_next(STEP.WAIT_ENTER);
		}
	}

	// クリームを表示/非表示する.
	public void		setCreamVisible(bool is_visible)
	{
		var	cream = this.transform.Find("Cream");

		if(cream != null) {

			cream.gameObject.SetActive(is_visible);
		}
	}

	// ================================================================ //

	public bool IsUnlocked()
	{
	#if true
		// 絵素材の確認用に、一時的に変えました.
		return(is_unlocked);
	#else
		return is_unlocked || connect_to.is_unlocked;
	#endif
	}

	protected	DoorMojiControl		moji_effect = null;

	public void Unlock()
	{
		is_unlocked = true;

		this.setCreamVisible(!this.is_unlocked);

		if(this.room.isCurrent()) {

		}

		if(this.room.isCurrent()) {

			this.step.set_next(STEP.WAIT_ENTER);

		} else {

			this.step.set_next(STEP.SLEEP);
		}

		// キーを削除する.
		// 自分がつながっているドアだった＝キーを拾ったドアじゃないときは、.
		// キーアイテムをここで削除しておかなきゃだめ.

		string	key_instance_name = Item.Key.getInstanceName((Item.KEY_COLOR)this.KeyType, this.room_index);

		ItemManager.getInstance().deleteItem(key_instance_name);
	}

	public Vector3	getPosition()
	{
		return(this.gameObject.transform.position);
	}

	// ドアを明示的にロックする（デバッグでしか使わなそうなので、頭に "db"(debug) をつけました.
	public void	dbLock()
	{
		is_unlocked = false;

		this.setCreamVisible(!this.is_unlocked);

		if(this.moji_effect != null) {

			GameObject.Destroy(this.moji_effect.gameObject);
			this.moji_effect = null;
		}
	}

	public void SetKey(string keyItemName)
	{
		this.keyItemName = keyItemName;
	}

	public void SetRoom(RoomController room)
	{
		this.room = room;
	}

	public RoomController GetRoom()
	{
		return this.room;
	}

	//
}
