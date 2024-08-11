using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Character {

// 召喚獣の種類.
public enum BEAST_TYPE {

	NONE = -1,

	DOG = 0,	// 犬.
	NEKO,		// ねこ.

	NUM,
}

}

public class PartyControl : MonoBehaviour {

	protected List<chrBehaviorPlayer>	players = new List<chrBehaviorPlayer>();

	protected chrBehaviorBase			beast;					// 召喚獣（仲間）.

	protected float 					summon_time = 0.0f;		// 召喚時間.

	protected const float				SUMMON_INTERVAL = 30.0f;

	protected const float				SUMMON_TIME_CONDITION = 5.0f;	

	protected RoomController			current_room;
	protected RoomController			next_room;

	// ドアの開閉を管理.
	protected Dictionary<string, bool>	door_state = new Dictionary<string, bool>();

	// 今いる部屋の鍵を持ってる？.

	protected Dictionary<string, int>	has_key  = new Dictionary<string, int>();


	protected Network 					m_network = null;

	protected enum SUMMON_STATE
	{
		INTERVAL = 0,			// 出現までの待ち時間.
		CHECK_APPEAR,			// 出現チェック.
		APPEAR,					// 出現中.
	}

	protected	SUMMON_STATE			state = SUMMON_STATE.INTERVAL;


	protected Character.BEAST_TYPE		request_summon_beast = Character.BEAST_TYPE.NONE;	// 召喚獣（デバッグ用）.
	

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{

		if(dbwin.root().getWindow("party") == null) {

			this.create_debug_window();
		}

		// Networkクラスのコンポーネントを取得.
		GameObject obj = GameObject.Find("Network");
		
		if(obj != null) {
			m_network = obj.GetComponent<Network>();
			if (m_network != null) {
				m_network.RegisterReceiveNotification(PacketId.MovingRoom, OnReceiveMovingRoomPacket);
				// 召喚獣の管理は PartyControlに変更になりました.
				// そのため, 召喚獣出現パケット受信関数は CharacterRoot から PartyControlへ移動になりました.
				m_network.RegisterReceiveNotification(PacketId.Summon, OnReceiveSummonPacket);
			}
		}

		this.next_room = null;
	}
	
	void	Update()
	{
		// クエリーを更新する.
		this.update_queries();


		// 召喚獣の処理.
		switch (this.state) {

			case SUMMON_STATE.INTERVAL:
			{
				this.summon_time += Time.deltaTime;
				
				if (this.summon_time > SUMMON_INTERVAL) {
					this.state = SUMMON_STATE.CHECK_APPEAR;
				}
			}
			break;

			case SUMMON_STATE.CHECK_APPEAR:
			{
				// 召喚獣出現チェック.
				this.checkSummonBeast();
			}
			break;

			case SUMMON_STATE.APPEAR:
			{
				// 召喚獣解除チェック.
				this.summon_time += Time.deltaTime;
				
				if (this.summon_time > SUMMON_INTERVAL) {
					
					this.unsummonBeast();
				}	
			}
			break;
		}

		if (this.beast == null) {

		}
		else {

		}
	}

	// ---------------------------------------------------------------- //
	// クエリーを更新する.

	protected void	update_queries()
	{
	}

	// ================================================================ //

	// ローカルプレイヤーを作る.
	public void		createLocalPlayer(int account_global_index)
	{
		if(this.players.Count == 0) {

			AccountData	account_data = AccountManager.get().getAccountData(account_global_index);

			string		avator_name = "Player_" + account_data.avator_id;

			chrBehaviorLocal	local_player = CharacterRoot.getInstance().createPlayerAsLocal(avator_name).GetComponent<chrBehaviorLocal>();

			local_player.control.local_index = 0;
			local_player.control.global_index = account_global_index;

			local_player.position_in_formation = this.getInFormationOffset(account_global_index);

			SHOT_TYPE shot_type = GlobalParam.get().shot_type[account_global_index];
			local_player.changeBulletShooter(shot_type);

			this.players.Add(local_player);
		}
	}

	// ネットプレイヤーを作る.
	public void		createNetPlayer(int account_global_index)
	{
		chrBehaviorLocal	local_player = this.players[0].GetComponent<chrBehaviorLocal>();

		chrBehaviorNet		net_player = null;

		int		local_index = this.players.Count;

		AccountData	account_data = AccountManager.get().getAccountData(account_global_index);

		string		avator_name = "Player_" + account_data.avator_id;

		net_player = CharacterRoot.getInstance().createPlayerAsNet(avator_name).GetComponent<chrBehaviorNet>();

		net_player.control.local_index  = local_index;
		net_player.control.global_index = account_global_index;
		net_player.local_player          = local_player;

		net_player.position_in_formation = this.getInFormationOffset(account_global_index);

		SHOT_TYPE shot_type = GlobalParam.get().shot_type[account_global_index];
		net_player.changeBulletShooter(shot_type);

		net_player.transform.Translate(this.getLocalPlayer().control.getPosition() + net_player.position_in_formation);

		this.players.Add(net_player);
	}

	// ネットプレイヤーを削除する.
	public void		deleteNetPlayer(int account_global_index)
	{
		do {

			chrBehaviorPlayer	friend = this.getFriendByGlobalIndex(account_global_index);

			if(friend == null) {

				break;
			}

			this.players.Remove(friend);

			GameObject.Destroy(friend.gameObject);

		} while(false);
	}

	// なんちゃってネットプレイヤーを作る.
	public void		createFakeNetPlayer(int account_global_index)
	{
		chrBehaviorLocal	local_player = this.players[0].GetComponent<chrBehaviorLocal>();

		chrBehaviorFakeNet	net_player = null;

		int		local_index = this.players.Count;

		AccountData	account_data = AccountManager.get().getAccountData(account_global_index);

		string		avator_name = "Player_" + account_data.avator_id;

		net_player = CharacterRoot.getInstance().createPlayerAsFakeNet(avator_name).GetComponent<chrBehaviorFakeNet>();

		net_player.control.local_index  = local_index;
		net_player.control.global_index = account_global_index;
		net_player.local_player          = local_player;

		net_player.position_in_formation = this.getInFormationOffset(account_global_index);

		net_player.transform.Translate(this.getLocalPlayer().control.getPosition() + net_player.position_in_formation);

		this.players.Add(net_player);
	}

	// 召喚獣の出現チェック.
	private void 	checkSummonBeast()
	{
		if(m_network == null) {
			return;
		}

		bool isInRange = true;

		if (this.players.Count <= 1) {
			// １人の時は出現しない.
			return;
		}

		chrBehaviorLocal local_behavior = this.getLocalPlayer();

		Vector3 local_pos = local_behavior.transform.position;

		for (int gid = 0; gid < NetConfig.PLAYER_MAX; ++gid) {

			int node = m_network.GetClientNode(gid);

			if (m_network.IsConnected(node) == false) {
				continue;
			}

			chrBehaviorPlayer remote_behavior = this.getFriendByGlobalIndex(gid);

			if (remote_behavior == null) {
				isInRange = false;
				break;
			}

			// 5m範囲内に集合していれば出現する.
			Vector3 remote_pos = remote_behavior.transform.position;
			if ((local_pos - remote_pos).magnitude > 5.0f) {
				isInRange = false;
				break;
			}

		}

		if (isInRange) {
			this.summon_time += Time.deltaTime;
		}
		else {
			// 出現条件をリセット.
			this.summon_time = 0.0f;
		}

		if (this.summon_time > SUMMON_TIME_CONDITION) {
			Character.BEAST_TYPE type = (Random.Range(0, 10) < 7)? Character.BEAST_TYPE.DOG : Character.BEAST_TYPE.NEKO;
			notifySummonBeast(type);
			this.summon_time = 0.0f;
		}
	}

	// 召喚獣を召喚する.
	public void	notifySummonBeast(Character.BEAST_TYPE beast_type)
	{
		string				beast_name = "";

		do {
			
			if(this.beast != null) {
				
				break;
			}
			
			switch(beast_type) {
					
				case Character.BEAST_TYPE.DOG:
				{
					beast_name = "Dog";
				}
				break;
				
				case Character.BEAST_TYPE.NEKO:
				{
					beast_name = "Neko";
				}
				break;
			}
			
			if(beast_name == "") {
				
				break;
			}

			// 召喚獣出現の通知を行う.
			if (m_network != null) {
				
				SummonData data = new SummonData();
				
				data.summon = beast_name;
				
				SummonPacket packet = new SummonPacket(data);
				int serverNode = m_network.GetServerNode();
				m_network.SendReliable<SummonData>(serverNode, packet);
				
				Debug.Log("[CLIENT] send summon beast:" + beast_name);
			}

		} while(false);
	}


	// 召喚獣を召喚する.
	public void		summonBeast(string beast_name)
	{

		do {

			if(this.beast != null) {

				break;
			}

			if(beast_name == "") {

				break;
			}

			string		avator_name   = "Beast_" + beast_name;
			string		behavior_name = "chrBehaviorBeast_" + beast_name;

			chrController	chr = CharacterRoot.getInstance().summonBeast(avator_name, behavior_name);

			if(chr == null) {

				break;
			}
			
			this.beast = chr.behavior;

			this.beast.control.cmdSetPositionAnon(this.getLocalPlayer().control.getPosition() + Vector3.back*4.0f);

			this.summon_time = 0.0f;

			this.state = SUMMON_STATE.APPEAR;

			//Debug.Log("[CLIENT] Summon beast:" + beast_name);

		} while(false);
	}

	// 召喚解除.
	private void	unsummonBeast()
	{
		if (this.beast == null) {
			return;
		}

		this.summon_time += Time.deltaTime;

		string beast_name = this.beast.GetComponent<chrController>().name;
		GameObject go = GameObject.Find(beast_name);
		if (go != null) {

			GameObject.Destroy(go);
		}

		this.beast = null;

		this.summon_time = 0.0f;

		//Debug.Log("[CLIENT] Unsummon beast:" + beast_name);

		this.state = SUMMON_STATE.INTERVAL;
	}

	// ================================================================ //

	// プレイヤーキャラクター全部を取得する.
	public List<chrBehaviorPlayer>	getPlayers()
	{
		return(this.players);
	}

	// パーティーメンバーの数を取得する.
	public int	getPlayerCount()
	{
		return(this.players.Count);
	}

	// 自分以外のパーティーメンバーの数を取得する.
	public int	getFriendCount()
	{
		return(this.players.Count - 1);
	}

	// 自分以外のパーティーメンバーを取得する.
	public chrBehaviorPlayer	getFriend(int friend_index)
	{
		chrBehaviorPlayer	friend = null;

		if(0 <= friend_index && friend_index < this.players.Count - 1) {

			friend = this.players[friend_index + 1];
		}

		return(friend);
	}

	// 自分以外のパーティーメンバーをグローバルインデックスで取得する.
	public chrBehaviorPlayer	getFriendByGlobalIndex(int friend_global_index)
	{
		chrBehaviorPlayer	friend = null;

		foreach(var player in this.players) {

			if((player as chrBehaviorLocal) != null) {

				continue;
			}

			if(player.control.global_index != friend_global_index) {

				continue;
			}

			friend = player;
			break;
		}

		return(friend);
	}

	// ローカルプレイヤーを取得する.
	public chrBehaviorLocal		getLocalPlayer()
	{
		chrBehaviorLocal	player = null;

		if(this.players.Count > 0) {

			player = this.players[0].GetComponent<chrBehaviorLocal>();
		}

		return(player);
	}

	// アカウント名を使用してプレイヤーを検索し、取得します.
	public chrBehaviorPlayer getPlayerWithAccountName(string account_name)
	{
		foreach (chrBehaviorPlayer player in players) {

			string player_name = player.name.Replace("Player_", "");
			if (player_name == account_name) {

				return player;
			}
		}

		return null;
	}

	// プレイヤーがカギを持った.
	public void	pickKey(string key_name, string account_name) 
	{
		chrBehaviorPlayer player = getPlayerWithAccountName(account_name);

		if (player == null) {
			return;
		}

		string[] key_str = key_name.Split('_');
		string key_id = key_str[0];

		if (has_key.ContainsKey(key_id) == false) {
			has_key.Add(key_id, player.control.local_index);
		}
		else {
			has_key[key_id] = player.control.local_index;
		}
	}

	public void	dropKey(string key_id, string account_name) 
	{
		chrBehaviorPlayer player = getPlayerWithAccountName(account_name);
		
		if (player == null) {
			return;
		}
		
		if (has_key.ContainsKey(key_id)) {
			has_key.Remove(key_id);
		}
	}

	// プレイヤーがカギを持っているか.
	public bool	hasKey(int  account_global_index, string door_id) 
	{
		string[] key_id = door_id.Split('_');

		if (has_key.ContainsKey(key_id[0]) == false) {
			return false;
		}

		return (has_key[key_id[0]] == account_global_index);
	}

	// ================================================================ //

	// スタート位置のオフセットを求める.
	// （プレイヤー同士が重ならないようにするための）.
	public Vector3	getPositionOffset(int account_global_index)
	{
		Vector3		offset = Vector3.zero;

		switch(account_global_index) {

			case 0: offset = new Vector3( 2.0f, 0.0f,  2.0f);	break;
			case 1:	offset = new Vector3(-2.0f, 0.0f,  2.0f);	break;
			case 2:	offset = new Vector3( 2.0f, 0.0f, -2.0f);	break;
			case 3:	offset = new Vector3(-2.0f, 0.0f, -2.0f);	break;
		}

		return(offset);
	}

	// フォーメーション移動中（主に FakeNet）の位置オフセット.
	public Vector3	getInFormationOffset(int account_global_index)
	{
		Vector3		offset = this.getPositionOffset(account_global_index);

		Vector3		local_player_offset = this.getPositionOffset(GlobalParam.getInstance().global_account_id);

		return(offset - local_player_offset);
	}

	// ================================================================ //

	// 『今いる部屋』をセットする.
	public void		setCurrentRoom(RoomController room)
	{
		this.current_room = room;
		this.next_room    = room;

		MapCreator.get().SetCurrentRoom(this.current_room);
	}

	// 『今いる部屋』をゲットする.
	public RoomController	getCurrentRoom()
	{
		return(this.current_room);
	}

	// 『次の部屋』をセットする.
	public void		setNextRoom(RoomController room)
	{
		this.next_room = room;
	}

	// 『次の部屋』をゲットする.
	public RoomController	getNextRoom()
	{
		return(this.next_room);
	}

	// ================================================================ //

	// ドアの開閉を実行.
	// 開閉の制御をPartyControlから行うよりもDoorControlから監視した方が.
	// 都合がよいため開閉状態を管理してDoorControlから状態を取得するように.
	// 仕様を変更しました.
	public void 	cmdMoveRoom(string key_id)
	{
		if (door_state.ContainsKey(key_id) == false) {
			string log = "[CLIENT] Add open door:" + key_id;
			Debug.Log(log);

			door_state.Add(key_id, true);
		}
	}

	public bool	isDoorOpen(string door_id)
	{
		string log = "[CLIENT] Search open door:";

		if (door_id == null) {
			// 無名ドアは開いていることにする.
			log += "(null)";
			Debug.Log(log);

			return true;
		}

		string[] key_id = door_id.Split('_');

		log += key_id[0] + "[" + door_state.ContainsKey(key_id[0]) + "]";
		//Debug.Log(log);

		return door_state.ContainsKey(door_id);//key_id[0]);
	}

	public void clearDoorState(string door_id)
	{
		string log = "[CLIENT] clear open state:";
	
		do {
			if (door_id == null) {
				// 無名ドアは無視する.
				log += null;
				break;
			}
			
			door_state.Remove(door_id);
			log += door_id;

		} while (false);

		Debug.Log(log);

	}

	// ================================================================ //
	// 部屋移動の通知パケット受信関数.

	public void OnReceiveMovingRoomPacket(int node, PacketId id, byte[] data)
	{
		RoomPacket packet = new RoomPacket(data);
		MovingRoom room = packet.GetPacket();

		string log = "[CLIENT] Receive moving room packet: " + room.keyId;
		Debug.Log(log);

		// 部屋移動のコマンド発行.
		cmdMoveRoom(room.keyId);
	}

	// 召喚獣出現情報受信関数.
	// 召喚獣は PartyControlクラスで管理することになりましたので
	// こちらに移動しました.
	public void OnReceiveSummonPacket(int node, PacketId id, byte[] data)
	{		
		SummonPacket packet = new SummonPacket(data);
		SummonData summon = packet.GetPacket();

		string log = "[CLIENT] Receive summon packet: " + summon.summon;
		Debug.Log(log);

		if (this.beast != null) {
			// すでに出現しているので受信パケットは無視する.
			Debug.Log("[CLIENT] Beast is already summoned.");
			return;
		}

		// 召喚獣を出現させる
		this.summonBeast(summon.summon);
	}

	// ================================================================ //


	protected void		create_debug_window()
	{
		var		window = dbwin.root().createWindow("party");

		window.createButton("次の人")
			.setOnPress(() =>
			{
				GlobalParam.getInstance().global_account_id = (GlobalParam.getInstance().global_account_id + 1)%4;
				GlobalParam.getInstance().fadein_start = false;
	
				GameRoot.get().restartGameScane();
			});

		window.createButton("助けて～")
			.setOnPress(() =>
			{
				int		friend_count = PartyControl.get().getFriendCount();

				if(friend_count < 3) {

					int		friend_global_index = (GlobalParam.getInstance().global_account_id + friend_count + 1)%4;

					this.createFakeNetPlayer(friend_global_index);
				}
			});

		window.createButton("ばいば～い")
			.setOnPress(() =>
			{
				int		friend_count = PartyControl.get().getFriendCount();

				if(friend_count >= 1) {

					chrBehaviorPlayer	player = this.getFriend(0);

					int		friend_global_index = player.control.global_index;

					this.deleteNetPlayer(friend_global_index);
				}
			});

		window.createButton("集合！")
			.setOnPress(() =>
			{
				int		friend_count = this.getFriendCount();

				for(int i = 0;i < friend_count;i++) {

					chrBehaviorFakeNet	friend = this.getFriend(i) as chrBehaviorFakeNet;

					if(friend == null) {

						continue;
					}

					friend.in_formation = true;
				}
			});

		window.createButton("解散！")
			.setOnPress(() =>
			{
				int		friend_count = this.getFriendCount();

				for(int i = 0;i < friend_count;i++) {

					chrBehaviorFakeNet	friend = this.getFriend(i) as chrBehaviorFakeNet;

					if(friend == null) {

						continue;
					}

					friend.in_formation = false;
				}
			});

		window.createButton("オヤジ犬！")
			.setOnPress(() =>
			{
				notifySummonBeast(Character.BEAST_TYPE.DOG);
				//this.request_summon_beast = Character.BEAST_TYPE.DOG;
			});

		window.createButton("おばにゃん！")
			.setOnPress(() =>
			{
				notifySummonBeast(Character.BEAST_TYPE.NEKO);
				//this.request_summon_beast = Character.BEAST_TYPE.NEKO;
			});

		window.createButton("召喚解除")
			.setOnPress(() =>
			            {
				unsummonBeast();
			});
	}

	// ================================================================ //
	// インスタンス.

	private	static PartyControl	instance = null;

	public static PartyControl	getInstance()
	{
		if(PartyControl.instance == null) {

			PartyControl.instance = GameObject.Find("GameRoot").GetComponent<PartyControl>();
		}

		return(PartyControl.instance);
	}

	public static PartyControl	get()
	{
		return(PartyControl.getInstance());
	}
}

