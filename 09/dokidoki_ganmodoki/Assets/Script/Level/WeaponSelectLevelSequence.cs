using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GameObjectExtension;
using MathExtension;

// 武器選択シーンのシーケンス制御.
public class WeaponSelectLevelSequence : SequenceBase {

	public enum STEP {

		NONE = -1,

		IDLE = 0,
		DEMO0,				// かぶさん登場デモ.
		SELECT_WEAPON,		// 武器アイテムひろうまで.
		PICKUP_KEY,			// カギ拾うまで.
		ENTER_DOOR,			// ドアに入るまで.
		TRANSPORT,			// フロアー移動イベント.
		WAIT_FRIEND,		// 他プレイヤー待ち.
		FINISH,

		NUM,
	};
	public Step<STEP>	step = new Step<STEP>(STEP.NONE);

	//===================================================================
	// フロアーキー周りのプロパティ

	protected string	key_item_name = "key04";
	protected string	key_instance_name = "";

	//===================================================================

	protected chrBehaviorLocal	player = null;
	protected chrBehaviorKabu	kabusan = null;

	public GameObject	spotlight_prefab = null;
	public GameObject	spotlight_kabusan_prefab = null;
	public GameObject	spotlight_key_prefab = null;

	protected GameObject		spotlight_player = null;
	protected GameObject		spotlight_kabusan = null;
	protected GameObject[]		spotlight_items = null;
	protected GameObject		spotlight_key = null;

	protected bool[]			select_done_players = null;				// 各プレイヤーがドアに入った？.
	protected bool				select_scene_finish = false;			// 武器選択シーンおしまい？.

	protected List<SelectingIcon>	selecting_icons;

	//===================================================================

	// 通信モジュール.
	protected Network			m_network;

	//===================================================================
	
	// デバッグウインドウ生成時に呼ばれる.
	public override void		createDebugWindow(dbwin.Window window)
	{
		window.createButton("クリアー")
			.setOnPress(() =>
			{
				this.step.set_next(STEP.FINISH);
			});
	}

	// レベル開始時に呼ばれる.
	public override void		start()
	{
		this.player = PartyControl.get().getLocalPlayer();
		this.kabusan = CharacterRoot.get().findCharacter<chrBehaviorKabu>("NPC_Kabu_San");

		// スポットライト.

		this.spotlight_player  = this.spotlight_prefab.instantiate();
		this.spotlight_kabusan = this.spotlight_kabusan_prefab.instantiate();

		this.spotlight_items = new GameObject[2];
		for(int i = 0;i < 2;i++) {

			this.spotlight_items[i] = this.spotlight_prefab.instantiate();
		}
		this.spotlight_items[0].setPosition(WeaponSelectMapInitializer.getNegiItemPosition().Y(4.0f));
		this.spotlight_items[1].setPosition(WeaponSelectMapInitializer.getYuzuItemPosition().Y(4.0f));

		this.spotlight_key = this.spotlight_key_prefab.instantiate();
		this.spotlight_key.SetActive(false);

		// 『各プレイヤーの選択が終わった？』フラグ.

		this.select_done_players = new bool[NetConfig.PLAYER_MAX];

		for(int i = 0;i < this.select_done_players.Length;i++) {

			if(GameRoot.get().isConnected(i)) {

				this.select_done_players[i] = false;

			} else {

				// 参加していないプレイヤーは『選択済み』にしておく.
				this.select_done_players[i] = true;
			}
		}
		
		// 他のプレイヤーの状況を表すアイコンを生成する.
		this.create_selecting_icons();

		// Networkクラスのコンポーネントを取得.
		GameObject	obj = GameObject.Find("Network");
		
		if(obj != null) {
			
			this.m_network = obj.GetComponent<Network>();

			if (this.m_network != null) {
				m_network.RegisterReceiveNotification(PacketId.GameSyncInfo, OnReceiveSyncPacket);
			}
		}

		this.step.set_next(STEP.DEMO0);
	}

	// 毎フレーム呼ばれる.
	public override void		execute()
	{
		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			// かぶさん登場デモ.
			case STEP.DEMO0:
			{
				if(EventRoot.get().getCurrentEvent() == null) {

					this.step.set_next(STEP.SELECT_WEAPON);
				}
			}
			break;

			// 武器アイテムひろうまで.
			case STEP.SELECT_WEAPON:
			{
				// 武器アイテムを拾ったら次へ.
				if(this.player.getShotType() != SHOT_TYPE.EMPTY) {

					// 拾われなかった武器アイテムを削除する.
					List<ItemController>	shot_items = ItemManager.get().findItems(x => x.name.StartsWith("shot"));

					foreach(var item in shot_items) {

						ItemManager.get().deleteItem(item.name);
					}

					this.select_done_players[this.player.getGlobalIndex()] = true;

					// 武器アイテムのスポットライトを消して、鍵の位置にスポットライト.
					this.spotlight_items[0].setPosition(WeaponSelectLevelSequence.getKeyStayPosition().Y(4.0f));
					this.spotlight_items[1].SetActive(false);

					this.step.set_next(STEP.PICKUP_KEY);
				}
			}
			break;

			// カギ拾うまで.
			case STEP.PICKUP_KEY:
			{
				if(ItemManager.get().findItem(this.key_instance_name) == null) {

					this.spotlight_items[0].SetActive(false);
					this.step.set_next(STEP.ENTER_DOOR);
				}
			}
			break;

			// ドアに入るまで.
			case STEP.ENTER_DOOR:
			{
				TransportEvent	ev = EventRoot.get().getCurrentEvent<TransportEvent>();

				if(ev != null) {

					ev.setEndAtHoleIn(true);

					this.step.set_next(STEP.TRANSPORT);
				}
			}
			break;

			// フロアー移動イベント.
			case STEP.TRANSPORT:
			{
				TransportEvent	ev = EventRoot.get().getCurrentEvent<TransportEvent>();

				if(ev == null) {

					// 初期装備の同期待ちクエリを発行する.
					var	query = new QuerySelectFinish(this.player.control.getAccountID());

					// クエリーのタイムアウトを延長.
					query.timeout = 20.0f;

					QueryManager.get().registerQuery(query);
					
					
					// 選択した武器をゲームサーバへ通知する.
					if (this.m_network != null) {
						CharEquipment equip = new CharEquipment();
						
						equip.globalId = GlobalParam.get().global_account_id;
						equip.shotType = (int) this.player.getShotType();
						
						Debug.Log("[CLIENT] Send equip AccountID:" + equip.globalId + " ShotType:" + equip.shotType);

						EquipmentPacket packet = new EquipmentPacket(equip);
						int serverNode = this.m_network.GetServerNode();
						this.m_network.SendReliable<CharEquipment>(serverNode, packet);
					}
					else {
						query.set_done(true);
						query.set_success(true);
					}

					this.step.set_next(STEP.WAIT_FRIEND);
				}
			}
			break;

			// 他プレイヤー待ち.
			case STEP.WAIT_FRIEND:
			{
				// ダンジョンへ移動するシグナルを待つ.
				if(this.select_scene_finish) {

					// 念のため、選択済みになっていないアイコンを強制的に選択済み表示に
					// してしまう.
					foreach(var icon in this.selecting_icons) {
	
						if(icon.step.get_current() == SelectingIcon.STEP.HAI) {
	
							continue;
						}
						icon.beginHai();
					}

					this.step.set_next_delay(STEP.FINISH, 2.0f);
				}
			}
			break;
		}
				
		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {

				// かぶさん登場デモ.
				case STEP.DEMO0:
				{
					EventRoot.get().startEvent<EventWeaponSelect>();
				}
				break;

				// カギ拾うまで.
				case STEP.PICKUP_KEY:
				{
					// フロアーキーが上から降ってくる.
					this.create_floor_key();
				}
				break;

				// ドアに入るまで.
				case STEP.ENTER_DOOR:
				{
					this.spotlight_key.SetActive(true);
				}
				break;

				// フロアー移動イベント.
				case STEP.TRANSPORT:
				{
					this.kabusan.onBeginTransportEvent();
				}
				break;

				// 他プレイヤー待ち.
				case STEP.WAIT_FRIEND:
				{	
					// 他のプレイヤーの状況を表すアイコンの表示を開始する.
					foreach(var icon in this.selecting_icons) {
			
						icon.setVisible(true);
					}
				}
				break;

				case STEP.FINISH:
				{
					GameRoot.get().setNextScene("GameScene");
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			// 他プレイヤー待ち.
			case STEP.WAIT_FRIEND:
			{
				foreach(var icon in this.selecting_icons) {

					if(icon.step.get_current() == SelectingIcon.STEP.UUN) {

						continue;
					}
					if(this.select_done_players[icon.player_index]) {

						icon.beginHai();
					}
				}
			}
			break;
		}

		// ---------------------------------------------------------------- //

		this.spotlight_player.setPosition(this.player.control.getPosition().Y(4.0f));
		this.spotlight_kabusan.setPosition(this.kabusan.control.getPosition().Y(8.0f));

		this.update_queries();
	}

	// ---------------------------------------------------------------- //

	// 他のプレイヤーの状況を表すアイコンを生成する.
	protected void		create_selecting_icons()
	{
		this.selecting_icons = new List<SelectingIcon>();

		for(int i = 0;i < NetConfig.PLAYER_MAX;i++) {

			if(!GameRoot.get().isConnected(i)) {

				continue;
			}

			this.selecting_icons.Add(Navi.get().createSelectingIcon(i));
		}

		switch(this.selecting_icons.Count) {

			case 1:
			{
				this.selecting_icons[0].setPosition(new Vector3(0.0f, 0.0f, 0.0f));
			}
			break;

			case 2:
			{
				this.selecting_icons[0].setPosition(new Vector3(-75.0f, 0.0f, 0.0f));
				this.selecting_icons[0].setFlip(true);
				this.selecting_icons[1].setPosition(new Vector3(75.0f, 0.0f, 0.0f));
			}
			break;

			case 3:
			{
				this.selecting_icons[0].setPosition(new Vector3(-75.0f,   0.0f, 0.0f));
				this.selecting_icons[0].setFlip(true);
				this.selecting_icons[1].setPosition(new Vector3( 75.0f,  50.0f, 0.0f));
				this.selecting_icons[2].setPosition(new Vector3(150.0f, -50.0f, 0.0f));
			}
			break;

			case 4:
			{
				this.selecting_icons[0].setPosition(new Vector3(-75.0f,  50.0f, 0.0f));
				this.selecting_icons[0].setFlip(true);
				this.selecting_icons[1].setPosition(new Vector3(-150.0f, -50.0f, 0.0f));
				this.selecting_icons[1].setFlip(true);
				this.selecting_icons[2].setPosition(new Vector3( 75.0f,  50.0f, 0.0f));
				this.selecting_icons[3].setPosition(new Vector3(150.0f, -50.0f, 0.0f));
			}
			break;
		}

		foreach(var icon in this.selecting_icons) {

			icon.setVisible(false);
		}
	}

	// クエリーを更新する.
	private void	update_queries()
	{

		List<QueryBase>		done_queries = QueryManager.get().findDoneQuery<QuerySelectFinish>();

		foreach(var query in done_queries) {

			QuerySelectFinish	query_select = query as QuerySelectFinish;

			if(query_select == null) {

				continue;
			}

			switch(query_select.getType()) {

				case "select.finish":
				{
					// 『武器選択シーン終わり』シグナルを受け取る.
					this.select_scene_finish = true;
				}
				break;
			}

			// 用済みになったので、削除する.
			query_select.set_expired(true);
		}
	}

	// フロアーキーが上から降ってくる.
	protected void create_floor_key()
	{
		this.key_instance_name = key_item_name + "." + this.player.getAcountID();

		ItemManager.get().createItem(key_item_name, this.key_instance_name, PartyControl.get().getLocalPlayer().getAcountID());
		ItemManager.get().setPositionToItem(this.key_instance_name, WeaponSelectLevelSequence.getKeyStayPosition());

		ItemController	item_key = ItemManager.get().findItem(this.key_instance_name);

		item_key.behavior.beginFall();
	}

	public static Vector3	getKeyStayPosition()
	{
		return(new Vector3(0.0f, 0.0f, 0.0f));
	}


	// ---------------------------------------------------------------- //
	// パケット受信関数.

	// 同期まちパケット受信.
	public void OnReceiveSyncPacket(int node, PacketId id, byte[] data)
	{
		Debug.Log("[CLIENT]OnReceiveSyncPacket");
		
		GameSyncPacket packet = new GameSyncPacket(data);
		GameSyncInfo sync = packet.GetPacket();

		GlobalParam.get().seed = sync.seed;

		// 初期装備を保存する.
		for (int i = 0; i < sync.items.Length; ++i) {

			CharEquipment equip = sync.items[i];

			GlobalParam.get().shot_type[equip.globalId] = (SHOT_TYPE)equip.shotType;
			this.select_done_players[equip.globalId] = true;

			Debug.Log("[CLIENT] AccountID:" + equip.globalId + " ShotType:" + equip.shotType);
		}

		// 応答のあったクエリを検索.
		string account_id = this.player.control.getAccountID();
		QuerySelectFinish	query = QueryManager.get().findQuery<QuerySelectFinish>(x => x.account_id == account_id);

		if (query != null) {
			Debug.Log("[CLIENT]QuerySelectDone done");
			query.set_done(true);
			query.set_success(true);
		}
		
		Debug.Log("[CLIENT]Recv seed:" + sync.seed);
	}
}
