using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

// キャラクターの生成など.
public class CharacterRoot : MonoBehaviour {

	public GameObject[]			chr_model_prefabs = null;		// モデルのプレハブ.
#if false
	private AcountManager		account_man;						// アカウントマネージャー.
#endif

	private chrController	player = null;

	public Material		damage_material = null;					// ダメージ演出用マテリアル.
	public Material		vanish_material = null;					// 退場演出用マテリアル.

	public GameObject	player_bullet_negi_prefab = null;		// プレイヤーのショットのプレハブ（ねぎ）.
	public GameObject	player_bullet_yuzu_prefab = null;		// プレイヤーのショットのプレハブ（ゆず）.

	public GameObject	enemy_bullet_prefab = null;				// 敵弾用のプレハブ.

	private Network 	m_network = null;

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
#if false
		this.account_man = this.gameObject.GetComponent<AcountManager>();
#endif
		
		// Networkクラスのコンポーネントを取得.
		GameObject obj = GameObject.Find("Network");
		
		if(obj != null) {
			m_network = obj.GetComponent<Network>();
			if (m_network != null) {
				m_network.RegisterReceiveNotification(PacketId.CharacterData, OnReceiveCharacterPacket);
				m_network.RegisterReceiveNotification(PacketId.AttackData, OnReceiveAttackPacket);
				m_network.RegisterReceiveNotification(PacketId.ChatMessage, OnReceiveChatMessage);
				m_network.RegisterReceiveNotification(PacketId.HpData, OnReceiveHitPointPacket);
				m_network.RegisterReceiveNotification(PacketId.DamageData, OnReceiveDamageDataPacket);
				m_network.RegisterReceiveNotification(PacketId.DamageNotify, OnReceiveDamageNotifyPacket);
				// 召喚獣の管理は PartyControlに変更になりました.
				// そのため, 召喚獣出現パケット受信関数も PartyControlに移動しました.
				//m_network.RegisterReceiveNotification(PacketId.Summon, OnReceiveSummonPacket);
			}
		}
	}
	
	void	Update()
	{
	}

	// ================================================================ //

	public chrController		getPlayer()
	{
		return(this.player);
	}

	// ローカルプレイヤーのキャラクターを作る.
	public chrController		createPlayerAsLocal(string chr_name)
	{
		chrController	chr_control = this.create_chr_common(chr_name, "chrBehaviorLocal");

		CameraControl.getInstance().setPlayer(chr_control);

		this.player = chr_control;

		return(chr_control);
	}

	// 敵キャラクターを作る.
	public chrController		createEnemy(string chr_name)
	{
		string		behavior_class_name = "chrBehavior" + chr_name;

		chrController	chr_control = this.create_chr_common(chr_name, "", behavior_class_name);

		return(chr_control);
	}

	// 敵キャラクターを作る.
	public chrController		createEnemy(string chr_name, string controller_class_name, string behavior_class_name)
	{
		return create_chr_common(chr_name, controller_class_name, behavior_class_name);
	}

	// ネットプレイヤーのキャラクターを作る.
	public chrController		createPlayerAsNet(string chr_name)
	{
		chrController	chr_control = this.create_chr_common(chr_name, "chrBehaviorNet");

		return(chr_control);
	}

	// なんちゃってネットプレイヤーのキャラクターを作る.
	public chrController		createPlayerAsFakeNet(string chr_name)
	{
		chrController	chr_control = this.create_chr_common(chr_name, "chrBehaviorFakeNet");

		return(chr_control);
	}

	// 召喚獣を召喚する.
	public chrController	summonBeast(string chr_name, string behavior_name)
	{
		chrController	chr_control = this.create_chr_common(chr_name, behavior_name);

		return(chr_control);
	}

	// NPCのキャラクターを作る.
	public chrController		createNPC(string chr_name)
	{
		chrController	chr_control = this.create_chr_common(chr_name, "chrBehaviorNPC");

		return(chr_control);
	}

	// デフォルトのコントローラクラスで（プレイヤーを）作る
	private chrController		create_chr_common(string name, string behavior_class_name)
	{
		return create_chr_common(name, "chrController", behavior_class_name);
	}
	
	
	// プレイヤーを作る.
	private chrController		create_chr_common(string name, string controller_class_name, string behavior_class_name)
	{
		chrController	chr_control = null;

		do {

			// モデルのプレハブを探す.

			GameObject	prefab = null;
	
			string	prefab_name = "ChrModel_" + name;

			if (this.chr_model_prefabs == null)
			{
				Debug.LogError("chr model prefabs is null");
				break;
			}

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

			rigidbody.constraints = RigidbodyConstraints.None;
       		rigidbody.constraints |= RigidbodyConstraints.FreezeRotationX;
			rigidbody.constraints |= RigidbodyConstraints.FreezeRotationY;
			rigidbody.constraints |= RigidbodyConstraints.FreezeRotationZ;

			// masa コメントアウトしました.
			//rigidbody.useGravity = false;

			// コントローラー.
			// (*)コントローラはローカルヒューマンプレイヤー/ネットヒューマンプレイヤー/AIによらず不変となる要素が記述されたクラス.
	
			chr_control = go.GetComponent<chrController>();

			if(chr_control == null) {

				if(controller_class_name != "") {

					chr_control = go.AddComponent(Type.GetType(controller_class_name)) as chrController;
				}

				// コントローラーが作れなかった＝専用のコントローラーが
				// なかったときは、基底のコントローラーをくっつける.
				if(chr_control == null) {
	
					chr_control = go.AddComponent<chrController>();
				}
			}

			// ビヘイビアー.

			chr_control.behavior = go.GetComponent<chrBehaviorBase>();

			if(chr_control.behavior == null) {

				chr_control.behavior = go.AddComponent(Type.GetType(behavior_class_name)) as chrBehaviorBase;

			} else {

				// ビヘイビアーが最初からくっついていたときは、つくらない.
			}

			chr_control.behavior.control  = chr_control;
			chr_control.behavior.initialize();


		} while(false);

		return(chr_control);
	}

	// アバター名でプレイヤーキャラクターを探す.
	public chrController	findPlayer(string avator_id)
	{
		GameObject[]	charactors = GameObject.FindGameObjectsWithTag("Player");

		chrController	charactor = null;

		foreach(GameObject go in charactors) {

			chrController	chr          = go.GetComponent<chrController>();

			if (chr.global_index < 0) {
			
				break;
			}

			AccountData		account_data = AccountManager.get().getAccountData(chr.global_index);

			if(account_data.avator_id == avator_id) {

				charactor = chr;
				break;
			}
		}

		return(charactor);
	}

	// キャラクターをオブジェクト名で探す.
	public T findCharacter<T>(string name) where T : chrBehaviorBase
	{
		T	behavior = null;

		do {

			GameObject	go = GameObject.Find(name);

			if(go == null) {

				break;
			}

			var	chr = go.GetComponent<chrController>();

			if(chr == null) {

				break;
			}

			behavior = chr.behavior as T;

		} while(false);

		return(behavior);
	}


	// ================================================================ //
	// ビヘイビアー用のコマンド.
	// クエリー系.

	// 問い合わせ　おしゃべり（ふきだし）.
	public QueryTalk	queryTalk(string account_id, string words, bool is_local)
	{
		QueryTalk		query = null;
		
		do {
			
			query = new QueryTalk(account_id, words);

			if (query == null) {

				break;
			}

			QueryManager.get().registerQuery(query);


			if (m_network !=null && is_local) {		
				// 吹き出しの要求を送る.
				ChatMessage chat = new ChatMessage();

				chat.characterId = PartyControl.get().getLocalPlayer().getAcountID();
				chat.message = words;

				ChatPacket packet = new ChatPacket(chat);

				int serverNode = m_network.GetServerNode();
				m_network.SendReliable<ChatMessage>(serverNode, packet);
			}
		} while(false);
		

		return(query);
	}

	// ---------------------------------------------------------------- //

	public void SendAttackData(string charId, int attacKind)
	{
		if (m_network != null) {
			AttackData data = new AttackData();
			
			data.characterId = charId;
			data.attackKind = attacKind;

			AttackPacket packet = new AttackPacket(data);
			m_network.SendUnreliableToAll<AttackData>(packet);
		}
	}
	
	public void SendCharacterCoord(string charId, int index, List<CharacterCoord> character_coord)
	{
		if (m_network != null) {
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
			m_network.SendUnreliableToAll<CharacterData>(packet);
		}
	}

	public void NotifyDamage(string charId, int attacker, float damage)
	{
		if (m_network != null) {
			DamageData data = new DamageData();
			
			data.target = charId;
			data.attacker = attacker;
			data.damage = damage;
			
			DamageNotifyPacket packet = new DamageNotifyPacket(data);
			m_network.SendReliableToAll<DamageData>(packet);
		}
	}
	
	public void NotifyHitPoint(string charId, float hp)
	{
		if(m_network != null) {		
			// パケットデータ作成.
			HpData data = new HpData();
			
			data.characterId = charId;
			data.hp = hp;

			// キャラクターの座標送信.
			HitPointPacket packet = new HitPointPacket(data);
			m_network.SendReliableToAll<HpData>(packet);
		}
	}

	// ---------------------------------------------------------------- //

	public void 	OnReceiveCharacterPacket(int node, PacketId id, byte[] data)
	{
		CharacterDataPacket packet = new CharacterDataPacket(data);
		CharacterData charData = packet.GetPacket();
		
		chrController controller = findPlayer(charData.characterId);
		
		if(controller == null) {
			return;
		}

		// キャラクター座標の補間.
		chrBehaviorNet behavior = controller.behavior as chrBehaviorNet;
		if (behavior != null) {
			behavior.CalcCoordinates(charData.index, charData.coordinates);
		}
	}

	public void 	OnReceiveAttackPacket(int node, PacketId id, byte[] data)
	{
		AttackPacket packet = new AttackPacket(data);
		AttackData attack = packet.GetPacket();

		//Debug.Log("[CLIENT] Receive Attack packet:" + attack.characterId);

		chrController controller = findPlayer(attack.characterId);
		
		if(controller == null) {
			return;
		}
		
		// キャラクター座標の補間.
		chrBehaviorNet behavior = controller.behavior as chrBehaviorNet;
		if (behavior != null) {
			if (attack.attackKind == 0) {
				behavior.cmdShotAttack();
			}
			else {
				behavior.cmdMeleeAttack();
			}
		}
	}

	public void OnReceiveMovingRoomPacket(int node, PacketId id, byte[] data)
	{
#if false
		RoomPacket packet = new RoomPacket(data);
		MovingRoom room = packet.GetPacket();
		
		// 部屋移動のコマンド発行.
		PartyControl.getInstance().cmdMoveRoom(room.keyId);
#endif
	}


	// HP通知情報受信関数.
	public void OnReceiveHitPointPacket(int node, PacketId id, byte[] data)
	{
		HitPointPacket packet = new HitPointPacket(data);
		HpData hpData = packet.GetPacket();

		//Debug.Log("[CLIENT] Receive hitpoint packet:" + hpData.characterId);

		chrBehaviorBase behavior = findCharacter<chrBehaviorBase>(hpData.characterId);

		if (behavior == null) {
			return;
		}

		chrController controller = behavior.control;

		if (controller == null) {
			return;
		}

		if (controller.global_index < 0) {
			return;
		}

		//string log = "[CLIENT] Set HP:" + hpData.characterId + " HP:" + hpData.hp;
		//Debug.Log(log);
		
		// キャラクターのHPを反映.
		controller.setHitPoint(hpData.hp);
	}

	// ダメージ量通知情報受信関数.
	public void OnReceiveDamageDataPacket(int node, PacketId id, byte[] data)
	{
		DamageDataPacket packet = new DamageDataPacket(data);
		DamageData damage = packet.GetPacket();

		//string log = "ReceiveDamageDataPacket:" + damage.target + "(" + damage.attacker + ") Damage:" + damage.damage;
		//Debug.Log(log);

		if (m_network == null || GameRoot.get().isHost()  == false) {
			// ホストへの通知パケットのためほかの端末は無視します.
			return;
		}

		DamageNotifyPacket sendPacket = new DamageNotifyPacket(damage);

		m_network.SendReliableToAll<DamageData>(sendPacket);
	}

	// ダメージ量通知情報受信関数.
	public void OnReceiveDamageNotifyPacket(int node, PacketId id, byte[] data)
	{
		DamageNotifyPacket packet = new DamageNotifyPacket(data);
		DamageData damage = packet.GetPacket();
#if false
		string avator_name = "";
		AccountData	account_data = AccountManager.get().getAccountData(damage.attacker);
		avator_name = account_data.avator_id;

		string log = "ReceiveDamageNotifyPacket:" + damage.target + "(" + damage.attacker + ") Damage:" + damage.damage;
		Debug.Log(log);
#endif
		chrBehaviorEnemy behavior = findCharacter<chrBehaviorEnemy>(damage.target);
		if (behavior == null) {
			return;
		}

		//log = "Cause damage:" + avator_name + " -> " + damage.target + " Damage:" + damage.damage;
		//Debug.Log(log);

		// キャラクターのダメージを反映.
		behavior.control.causeDamage(damage.damage, damage.attacker, false);
	}

#if false
	// 召喚獣出現情報受信関数.
	public void OnReceiveSummonPacket(int node, PacketId id, byte[] data)
	{
		// 召喚獣の管理は PartyControlに変更になりました.
		// そのため, 召喚獣出現パケット受信関数も PartyControlに移動しました.
	}
#endif

	public void 	OnReceiveChatMessage(int node, PacketId id, byte[] data)
	{
		Debug.Log("OnReceiveChatMessage");
		
		ChatPacket packet = new ChatPacket(data);
		ChatMessage chat = packet.GetPacket();
		
		Debug.Log("[CharId]" + chat.characterId);
		Debug.Log("[CharMsg]" + chat.message);
		

		chrController controller = findPlayer(chat.characterId);
		// チャットメッセージ表示のクエリ発行.
		if (controller != null) {
			QueryTalk talk = queryTalk(chat.characterId, chat.message, false);
			if (talk != null) {
				talk.set_done(true);
				talk.set_success(true);
			}
		}
	}

	// ================================================================ //
	
	private void SendGameSyncInfo()
	{
		Debug.Log("[CLIENT]SendGameSyncInfo");
		
		GameSyncInfo data = new GameSyncInfo();
		
		data.seed = 0;
		data.items = new CharEquipment[NetConfig.PLAYER_MAX];
		
		GameSyncPacket packet = new GameSyncPacket(data);
		if (m_network != null) {
			int serverNode = m_network.GetServerNode();
			m_network.SendReliable<GameSyncInfo>(serverNode, packet);
		}
	}

	// ================================================================ //

	private	static CharacterRoot	instance = null;

	public static CharacterRoot	get()
	{
		if(CharacterRoot.instance == null) {

			CharacterRoot.instance = GameObject.Find("CharacterRoot").GetComponent<CharacterRoot>();
		}

		return(CharacterRoot.instance);
	}
	public static CharacterRoot	getInstance()
	{
		return(CharacterRoot.get());
	}
}

