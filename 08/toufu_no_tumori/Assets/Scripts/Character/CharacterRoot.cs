using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

// キャラクターの生成など.
public class CharacterRoot : MonoBehaviour {


	public GameObject[]			chr_model_prefabs = null;		// モデルのプレハブ.

	protected AcountManager		account_man;						// アカウントマネージャー.
	protected GameInput			game_input;						// マウスなどの入力.

	protected List<QueryBase>		queries = new List<QueryBase>();		// クエリー.

	private Network				m_network = null;

	// ================================================================ //
	
	private bool				guestConnected = false;

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
		this.account_man = this.gameObject.GetComponent<AcountManager>();
		this.game_input = this.gameObject.GetComponent<GameInput>();


		m_network = GameObject.Find("Network").GetComponent<Network>();
		if (m_network != null) {
			m_network.RegisterReceiveNotification(PacketId.CharacterData, OnReceiveCharacterPacket);
			m_network.RegisterReceiveNotification(PacketId.Moving, OnReceiveMovingPacket);
			m_network.RegisterReceiveNotification(PacketId.ChatMessage, OnReceiveChatMessage);
			m_network.RegisterReceiveNotification(PacketId.GameSyncInfoHouse, OnReceiveSyncGamePacket);
			m_network.RegisterEventHandler(OnEventHandling);
		}
	}
	
	void	Update()
	{
		this.process_query();

		// リモートとの接続時の処理
		if (guestConnected) {
			SendGameSyncInfo();
			guestConnected = false;
		}
	}

	// クエリーの更新.
	protected void	process_query()
	{
		// フェールセーフ＆開発用.

		foreach(var query in this.queries) {

			//Debug.Log("[Query]" + query.timer);

			query.timer += Time.deltaTime;

			if(m_network == null) {

				// GameScene からはじめたとき（Title を経由しない）.
				// ネットワークオブジェクトがつくられていない.
				query.set_done(true);
				query.set_success(true);

			} else {

				// タイムアウト.
				if(query.timer > 5.0f) {

					query.set_done(true);
					query.set_success(false);
				}
			}
		}

		this.queries.RemoveAll(x => x.isDone());
	}

	// ================================================================ //

	public void		deletaCharacter(chrController character)
	{
		GameObject.Destroy(character.gameObject);
	}

	// ローカルプレイヤーのキャラクターを作る.
	public chrController		createPlayerAsLocal(string chr_name)
	{
		chrController	chr_control = this.create_chr_common(chr_name, "chrBehaviorLocal");

		return(chr_control);
	}

	// ネットプレイヤーのキャラクターを作る.
	public chrController		createPlayerAsNet(string chr_name)
	{
		chrController	chr_control = this.create_chr_common(chr_name, "chrBehaviorNet");

		return(chr_control);
	}

	// NPCのキャラクターを作る.
	public chrController		createNPC(string chr_name)
	{
		chrController	chr_control = this.create_chr_common(chr_name, "chrBehaviorNPC");

		return(chr_control);
	}

	// プレイヤーを作る.
	private chrController		create_chr_common(string name, string behavior_class_name)
	{
		chrController	chr_control = null;

		do {

			// モデルのプレハブを探す.
	
			GameObject	prefab = null;
	
			string	prefab_name = "ChrModel_" + name;
	
			prefab = System.Array.Find(this.chr_model_prefabs, x => x.name == prefab_name);
	
			if(prefab == null) {
	
				Debug.LogError("Can't find prefab \"" + prefab_name + "\".");
				break;
			}
	
			//
	

			GameObject	go = GameObject.Instantiate(prefab) as GameObject;

			go.name = name;

			// リジッドボディ.

			Rigidbody	rigidbody = go.AddComponent<Rigidbody>();

			rigidbody.constraints  = RigidbodyConstraints.None;
        	rigidbody.constraints |= RigidbodyConstraints.FreezePositionY;
	       	rigidbody.constraints |= RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;

			// コントローラー.

			chr_control = go.AddComponent<chrController>();

			chr_control.model       = go;
			chr_control.account_name = name;
			chr_control.account_man  = this.account_man;

			chr_control.game_input  = this.game_input;

			// ビヘイビアー.

			chr_control.behavior = go.GetComponent<chrBehaviorBase>();

			if(chr_control.behavior == null) {

				chr_control.behavior = go.AddComponent(Type.GetType(behavior_class_name)) as chrBehaviorBase;

			} else {

				// ビヘイビアーが最初からくっついていたときは、つくらない.
			}

			chr_control.behavior.controll = chr_control;
			chr_control.behavior.initialize();

		} while(false);

		return(chr_control);
	}

	// キャラクターを探す.
	public chrController	findCharacter(string name)
	{
		chrController	character = null;

		do {

			GameObject[]	characters = GameObject.FindGameObjectsWithTag("Charactor");

			if(characters.Length == 0) {

				break;
			}

			GameObject		go;

			go = System.Array.Find(characters, x => x.name == name);

			if(go == null) {

				break;
			}

			character = go.GetComponent<chrController>();

			if(character == null) {

				Debug.Log("Cannot find character:" + name);

				go = GameObject.Find(name);

				if(go != null) {

					break;
				}
				character = go.GetComponent<chrController>();
			}

		} while(false);

		return(character);
	}

	// キャラクターを探す.
	public T	findCharacter<T>(string name) where T : chrBehaviorBase
	{
		T		behavior = null;

		do {

			chrController	character = this.findCharacter(name);

			if(character == null) {

				break;
			}

			behavior = character.behavior as T;

		} while(false);

		return(behavior);
	}

	// ゲーム入力（マウスとか）をゲットする.
	public GameInput	getGameInput()
	{
		return(this.game_input);
	}

	// ================================================================ //
	// ビヘイビアー用のコマンド.
	// クエリー系.

	// 問い合わせ　おしゃべり（ふきだし）.
	public QueryTalk	queryTalk(string words, bool local = true)
	{
		QueryTalk		query = null;

		do {

			query = new QueryTalk(words);

			this.queries.Add(query);

		} while(false);

		GameObject netObj = GameObject.Find("Network");
		if (netObj && local) {		
			// Networkクラスのコンポーネントを取得.
			Network network  = netObj.GetComponent<Network>();
			// 吹き出しの要求を送る.
			ChatMessage chat = new ChatMessage();
			chat.characterId = GameRoot.getInstance().account_name_local;
			chat.message = words;
			ChatPacket packet = new ChatPacket(chat);
			network.SendReliable<ChatMessage>(packet);
		}

		return(query);
	}

	// 問い合わせ　ひっこし始めてもいい？.
	public QueryHouseMoveStart	queryHouseMoveStart(string house_name, bool local = true)
	{
		QueryHouseMoveStart		query = null;

		do {
			
			chrBehaviorNPC_House	house = CharacterRoot.getInstance().findCharacter<chrBehaviorNPC_House>(house_name);

			if(house == null) {

				break;
			}			

			query = new QueryHouseMoveStart(house_name);

			this.queries.Add(query);

		} while(false);

		// 引越し開始のリクエストをネットに送る.
		GameObject netObj = GameObject.Find("Network");
		if (netObj && local) {		
			// Networkクラスのコンポーネントを取得.
			Network network  = netObj.GetComponent<Network>();
			// 吹き出しの要求を送る.
			MovingData moving = new MovingData();
			moving.characterId = GameRoot.getInstance().account_name_local;
			moving.houseId = house_name;
			moving.moving = true;
			MovingPacket packet = new MovingPacket(moving);
			network.SendReliable<MovingData>(packet);

			// 引越し情報保存.
			GlobalParam.get().local_moving = moving;
		}

		return(query);
	}

	// 問い合わせ　ひっこし終わってもいい？.
	public QueryHouseMoveEnd	queryHouseMoveEnd(bool local = true)
	{
		QueryHouseMoveEnd		query = null;

		do {

			query = new QueryHouseMoveEnd();

			this.queries.Add(query);

		} while(false);

		// 引越しおしまいのリクエストをネットに送る.
		GameObject netObj = GameObject.Find("Network");
		if (netObj && local) {		
			// Networkクラスのコンポーネントを取得.
			Network network  = netObj.GetComponent<Network>();
			// 吹き出しの要求を送る.
			MovingData moving = new MovingData();
			moving.characterId = GameRoot.getInstance().account_name_local;
			moving.houseId = "";
			moving.moving = false;
			MovingPacket packet = new MovingPacket(moving);
			network.SendReliable<MovingData>(packet);
		}

		return(query);
	}

	// アバター名でプレイヤーキャラクターを探す.
	public chrController	findPlayer(string avator_id)
	{
		GameObject[]	characters = GameObject.FindGameObjectsWithTag("Player");

		chrController	character = null;
		
		foreach(GameObject go in characters) {
			
			chrController	chr = go.GetComponent<chrController>();
			AcountData		account_data = AcountManager.get().getAccountData(chr.global_index);
			
			if(account_data.avator_id == avator_id) {
				
				character = chr;
				break;
			}
		}

		if (character == null) {
			GameObject go = GameObject.Find(avator_id);
			if (go != null) {
				character = go.GetComponent<chrController>();
			}
		}

		return(character);
	}


	// ================================================================ //

	private	static CharacterRoot	instance = null;

	public static CharacterRoot	getInstance()
	{
		if(CharacterRoot.instance == null) {

			CharacterRoot.instance = GameObject.Find("GameRoot").GetComponent<CharacterRoot>();
		}

		return(CharacterRoot.instance);
	}

	public static CharacterRoot	get()
	{
		return(CharacterRoot.getInstance());
	}


	public void SendCharacterCoord(string charId, int index, List<CharacterCoord> character_coord)
	{
		GameObject netObj = GameObject.Find("Network");
		if(netObj) {		
			// Networkクラスのコンポーネントを取得.
			Network network  = netObj.GetComponent<Network>();
			if (network.IsConnected() == true) {
				// パケットデータ作成.
				CharacterData data = new CharacterData();
				
				data.characterId = charId;
				data.index = index;
				data.dataNum = character_coord.Count;
				data.coordinates = new CharacterCoord[character_coord.Count];
				for (int i = 0; i < character_coord.Count; ++i) {
					data.coordinates[i] = character_coord[i];
				}

				// キャラクターの座標送信.
				CharacterDataPacket packet = new CharacterDataPacket(data);
				int sendSize = network.SendUnreliable<CharacterData>(packet);
				if (sendSize > 0) {
				//	Debug.Log("Send character coord.[index:" + index + "]");
				}
			}
		}
	}

	
	public void 	OnReceiveCharacterPacket(PacketId id, byte[] data)
	{
		CharacterDataPacket packet = new CharacterDataPacket(data);
		CharacterData charData = packet.GetPacket();

		if (GlobalParam.get().is_in_my_home != GlobalParam.get().is_remote_in_my_home) {
			return;
		}

		chrBehaviorNet remote = CharacterRoot.get().findCharacter<chrBehaviorNet>(charData.characterId);
		if (remote != null) {
			remote.CalcCoordinates(charData.index, charData.coordinates);
		}
	}

	public void 	OnReceiveMovingPacket(PacketId id, byte[] data)
	{
		Debug.Log("OnReceiveMovingPacket");

		MovingPacket packet = new MovingPacket(data);
		MovingData moving = packet.GetPacket();
		
		Debug.Log("[CharId]" + moving.characterId);
		Debug.Log("[HouseName]" + moving.houseId);
		Debug.Log("[Moving]" + moving.moving);

		chrController remote =
			CharacterRoot.get().findCharacter(moving.characterId);
		
		// 引越しのクエリ発行.
		if (remote != null) {
			if (moving.moving) {
				Debug.Log("cmdQueryHouseMoveStart");
				QueryHouseMoveStart query = remote.cmdQueryHouseMoveStart(moving.houseId, false);
				if (query != null) {
					query.set_done(true);
					query.set_success(true);
				}
			}
			else {
				Debug.Log("cmdQueryHouseMoveEnd");
				QueryHouseMoveEnd query = remote.cmdQueryHouseMoveEnd(false);
				if (query != null) {
					query.set_done(true);
					query.set_success(true);
				}
			}
		}

		// 引越し情報保存.
		GlobalParam.get().remote_moving = moving;
	}
	
	public void 	OnReceiveChatMessage(PacketId id, byte[] data)
	{
		Debug.Log("OnReceiveChatMessage");

		ChatPacket packet = new ChatPacket(data);
		ChatMessage chat = packet.GetPacket();

		Debug.Log("{CharId]" + chat.characterId);
		Debug.Log("[CharMsg]" + chat.message);

		chrController remote =
			CharacterRoot.get().findCharacter(chat.characterId);

		// チャットメッセージ表示のクエリ発行.
		if (remote != null) {
			QueryTalk talk = remote.cmdQueryTalk(chat.message);
			if (talk != null) {
				talk.set_done(true);
				talk.set_success(true);
			}
		}
	}

	public void OnReceiveSyncGamePacket(PacketId id, byte[] data)
	{
		Debug.Log("Receive GameSyncPacket[CharacterRoot].");
		
		SyncGamePacket packet = new SyncGamePacket(data);
		SyncGameData sync = packet.GetPacket();

		Debug.Log("[CharId]" + sync.moving.characterId);
		Debug.Log("[HouseName]" + sync.moving.houseId);
		Debug.Log("[Moving]" + sync.moving.moving);

		if (sync.moving.characterId.Length == 0) {
			// 引越ししていない.
			return;
		}

		// 引越し情報保存.
		GlobalParam.get().remote_moving = sync.moving;
	}

	// ================================================================ //
	
	private void SendGameSyncInfo()
	{
		Debug.Log("[CLIENT]SendGameSyncInfo");

		SyncGameData data = new SyncGameData();

		data.version = GameServer.SERVER_VERSION;
		data.itemNum = 0;

		// ホストからは引越し情報だけ送る.
		data.moving = new MovingData();
		if (GlobalParam.get().local_moving.moving) {
			data.moving = GlobalParam.get().local_moving;
		}
		else {
			data.moving.characterId = "";
			data.moving.houseId = "";
			data.moving.moving = false;
		}

		SyncGamePacketHouse packet = new SyncGamePacketHouse(data);
		if (m_network != null) {
			m_network.SendReliable<SyncGameData>(packet);
		}
	}

	// ================================================================ //
	
	
	public void OnEventHandling(NetEventState state)
	{
		switch (state.type) {
		case NetEventType.Connect:
			Debug.Log("connect event");
			guestConnected = true;
			break;
		}
	}

}

