using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ItemManager : MonoBehaviour {

	public	GameObject		item_base_prefab = null;

	public	GameObject[]	item_model_prefabs;

	public Texture			texture_icon_soda_ice = null;
	public Texture			texture_ice_bar = null;

	// ---------------------------------------------------------------- //

	// ---------------------------------------------------------------- //

	public Network			m_network = null;

	public delegate 		void ItemEventHandler(ItemController.State state, string owner_id, string item_id);

	public class ItemState
	{
		public string					item_id = "";						// ユニークな id.
		public ItemController.State		state = ItemController.State.None;	// ネットワーク的なステート.
		public string 					owner = "";							// 作った人（アカウント名）.
	}

	//private Hashtable	item_table = new Hashtable();

	private List<ItemState>			item_request = new List<ItemState>();

	// MonoBehaviour からの継承.
	// ================================================================ //

	void	Start()
	{
		// Networkクラスのコンポーネントを取得.
		GameObject obj = GameObject.Find("Network");
		if (obj != null) {
			m_network = obj.GetComponent<Network> ();
			if (m_network != null) {
				m_network.RegisterReceiveNotification (PacketId.ItemData, OnReceiveItemPacket);
				m_network.RegisterReceiveNotification (PacketId.UseItem, OnReceiveUseItemPacket);
			}
		}

		if (obj == null || m_network == null) {
			Debug.LogError("ItemManager can't find Netowrk gameobject or Network component on the current scene.");
		}
	}
	
	void	Update()
	{
		process_item_request();

		//this.process_item_query();
	}
	
	// ================================================================ //
	
	public void		createItem(string type, string owner)
	{
		this.createItem(type, type, owner);
	}

	// アイテムを作る.
	public void		createItem(string type, string name, string owner)
	{
		do {

			ItemController	item = (GameObject.Instantiate(this.item_base_prefab) as GameObject).GetComponent<ItemController>();

			// モデルのプレハブを探す.
	
			GameObject	item_model_prefab = null;
	
			string	prefab_name = "ItemModel_" + type;
	
			item_model_prefab = System.Array.Find(this.item_model_prefabs, x => x.name == prefab_name);
	
			if(item_model_prefab == null) {
	
				Debug.LogError("Can't find prefab \"" + prefab_name + "\".");
				break;
			}
	
			//
	
			GameObject	item_model_go = GameObject.Instantiate(item_model_prefab) as GameObject;
	
			item_model_go.transform.parent = item.transform;
			item_model_go.transform.localPosition = Vector3.zero;
	
			item.type         = type;
			item.owner_account = owner;
			item.id           = name;
			item.name         = item.id;
	
			item.transform.parent        = this.gameObject.transform;
			item.transform.localPosition = Vector3.zero;
	
			item.GetComponent<Rigidbody>().isKinematic = true;

			// ビヘイビアー.

			// カスタムのビヘイビアーは、コントローラの子供（モデルのプレハブ）にくっついているはず.
			item.behavior = item.gameObject.GetComponentInChildren<ItemBehaviorBase>();

			if(item.behavior == null) {

				item.behavior = item.gameObject.AddComponent<ItemBehaviorBase>();

			} else {

				// ビヘイビアーが最初からくっついていたときは、つくらない.
			}

			item.behavior.controll = item;
			item.behavior.initialize();

		} while(false);
	}

	// Delete Item (FIXME)
	public void		deleteItem(string id)
	{
		foreach (var item in GetComponentsInChildren<ItemController>()) {
			if (item.id == id) {
				Destroy(item.gameObject);
			}
		}
	}

	// アイテムの位置を取得する.
	public bool		getItemPosition(out Vector3 position, string item_id)
	{
		bool	ret = false;

		position = Vector3.zero;

		do {

			ItemController	item = this.find_item(item_id);

			if(item == null) {
	
				break;
			}

			position = item.transform.position;

			ret = true;

		} while(false);

		return(ret);
	}

	// アイテムのコリジョンサイズを取得する.
	public bool		getItemSize(out Vector3 size, string item_id)
	{
		bool	ret = false;

		size = Vector3.zero;

		do {

			ItemController	item = this.find_item(item_id);

			if(item == null) {
	
				break;
			}

			size = item.gameObject.GetComponent<Collider>().bounds.size;

			ret = true;

		} while(false);

		return(ret);
	}

	// アイテムの位置をセットする.
	public void		setPositionToItem(string item_id, Vector3 position)
	{
		do {

			ItemController	item = this.find_item(item_id);

			if(item == null) {
	
				break;
			}

			// 地面に落ちたところの位置を求める.

			Ray			ray    = new Ray(position + Vector3.up*1000.0f, Vector3.down);
			float		radius = item.GetComponent<SphereCollider>().radius;
			RaycastHit	hit;

			// 自分自身にレイがヒットしないように.
			item.GetComponent<Collider>().enabled = false;

			if(Physics.SphereCast(ray, radius, out hit)) {

				position = hit.point;
			}

			item.GetComponent<Collider>().enabled = true;

			item.transform.position = position;

		} while(false);
	}

	// 落ちているアイテムを拾う.
	public ItemController	pickItem(QueryItemPick query, string owner_id, string item_id)
	{
		// プログラムのバグを防ぐため、クエリーを持って
		// いないと拾えないようにしました.

		ItemController	item = null;
		
		do {
			
			// 一応、クエリーの結果もチェックする.
			if(!query.isSuccess()) {

				break;
			}
	
			item = this.find_item(item_id);
			
			if(item == null) {
				
				break;
			}

			item.picker = owner_id;
			item.startPicked();

			if (item_id.StartsWith("key")) {
				PartyControl.get().pickKey(item_id, owner_id);
			}

		} while(false);

		return(item);
	}

	// 持っているアイテムを捨てる.
	public void 	dropItem(string owner_id, string item_id)
	{
		ItemController item = this.find_item(item_id);
		
		if(item == null) {
			
			return;
		}

		item.picker = "";

		if (item_id.StartsWith("key")) {
			PartyControl.get().dropKey(item_id, "");
		}
	}


	// アイテムを探す.
	private	ItemController	find_item(string id)
	{
		ItemController	item = null;

		do {

			Transform	it = this.gameObject.transform.Find(id);

			if(it == null) {

				break;
			}

			item = it.gameObject.GetComponent<ItemController>();

		} while(false);

		return(item);
	}

	// アイテムを探す.
	public ItemController	findItem(string name)
	{
		ItemController	item = null;

		do {

			GameObject[]	items = GameObject.FindGameObjectsWithTag("Item");
			if(items == null) {

				break;
			}

			GameObject	go = System.Array.Find(items, x => x.name == name);
			if(go == null) {

				break;
			}

			item = go.GetComponent<ItemController>();

		} while(false);

		return(item);
	}

	// アイテムを探す.
	public List<ItemController>	findItems(System.Predicate<ItemController> pred)
	{
		GameObject[]			gos   = GameObject.FindGameObjectsWithTag("Item");

		List<ItemController>	items = new List<ItemController>();

		foreach(var go in gos) {

			var	controller = go.GetComponent<ItemController>();

			if(controller == null) {

				continue;
			}
			if(!pred(controller)) {

				continue;
			}

			items.Add(go.GetComponent<ItemController>());
		}

		return(items);
	}

	// アイテムの状態を変更する.
	public void	setItemState(string name, ItemController.State state, string owner)
	{
		GameObject[]	items = GameObject.FindGameObjectsWithTag("Item");

		if (items.Length == 0) {
			return;
		}

		GameObject go = System.Array.Find(items, x => x.name == name);

		if (go == null) {
			return;
		}

		ItemController	item = go.GetComponent<ItemController>();
	
		if (item == null) {
			return;
		}

		item.state = state;
		item.owner_account = owner;

		string log = "Item state changed => " + 
					 "[item:" + name + "]" + 
					 "[state:" + state.ToString() + "]" +
					 "[owner:" + owner + "]";
		Debug.Log(log);
		dbwin.console().print(log);
	}
	
	// アイテムを使う.
	public void		useItem(int slot_index, Item.Favor item_favor, string user_name, string target_name, bool is_local)
	{
		do {

			chrController	user = CharacterRoot.getInstance().findPlayer(user_name);

			if(user == null) {

				break;
			}

			chrController	target = CharacterRoot.getInstance().findPlayer(target_name);

			if(target == null) {

				break;
			}

			//

			if(user_name == PartyControl.get().getLocalPlayer().getAcountID()) {

				user.onUseItemSelf(slot_index, item_favor);

			} else {

				target.onUseItemByFriend(item_favor, user);
			}

			// アイテムの使用を通知する.
			if (is_local) {

				SendItemUseData(item_favor, user_name, target_name);
			}

		} while(false);
	}

	public void SendItemUseData(Item.Favor item_favor, string user_name, string target_name)
	{
		ItemUseData data = new ItemUseData();
		
		Debug.Log("[CLIENT] SendItemUseData: user:" + user_name + " target:" + target_name);
		dbwin.console().print("[CLIENT] SendItemUseData: user:" + user_name + " target:" + target_name);

		data.userId = user_name;
		data.targetId = target_name;
		data.itemCategory = (int)item_favor.category;

		ItemUsePacket packet = new ItemUsePacket(data);
		
		if (m_network != null) {
			int serverNode = m_network.GetServerNode();
			m_network.SendReliable<ItemUseData>(serverNode, packet);
		}
	}


	// ================================================================ //
	// ビヘイビアー用のコマンド.
	// クエリー系.

	// 落ちているアイテムを拾っていい？.
	public QueryItemPick	queryPickItem(string owner_id, string item_id, bool is_local, bool forece_pickup)
	{
		ItemController	item = null;
		QueryItemPick	query = null;
		bool			needMediation = is_local;

		do {
			
			item = this.find_item(item_id);
			
			if(item == null) {

				needMediation = false;
				break;
			}

			// 成長中のものとかは、拾えない.
			if(!item.isPickable() && !forece_pickup) {

				needMediation = false;
				break;
			}

			// もう誰かが持ち中のものは拾えない.
			if(item.picker != "") {

				needMediation = false;
				break;
			}

			query = new QueryItemPick(owner_id, item_id);

			QueryManager.get().registerQuery(query);

			// 取得中の状態に変更する.
			this.setItemState(item_id, ItemController.State.PickingUp, owner_id);

		} while(false);

		if (GameRoot.get().isNowCakeBiking() == false) {
			if(needMediation) {
				// アイテム取得の問い合わせを行う.
				SendItemStateChanged(item_id, ItemController.State.PickingUp, owner_id);
			}
		}

		return(query);
	}

	// 持ってる中のアイテムを破棄.
	public void	cmdDropItem(string owner_id, string item_id, bool local)
	{
		// アイテムのステートをネットに送る.
		SendItemStateChanged(item_id, ItemController.State.Dropping, owner_id);
	}
	

	// ================================================================ //
	
	private void	process_item_request()
	{
		this.item_request.Clear();
	}

	// ================================================================ //
	
	public ItemState FindItemState(string item_name) 
	{
		foreach (ItemState state in GlobalParam.get().item_table.Values) {
			if (item_name.Contains(state.item_id)) {
				return state;
			}
		}

		return default(ItemState);
	}
	
	
	// ================================================================ //
	// 通信に関数する関数群.
	
	
	// アイテムの状態変更通知関数.
	private void SendItemStateChanged(string item_id, ItemController.State state, string owner_id)
	{
		if(m_network == null) {
			return;
		}
		
		Debug.Log("SendItemStateChanged.");
		
		// アイテム取得の問い合わせ.
		ItemData data = new ItemData();
		
		data.itemId = item_id;
		data.ownerId = owner_id;
		data.state = (int)state;
		
		ItemPacket packet = new ItemPacket(data);

		int serverNode = m_network.GetServerNode();
		Debug.Log("ServerNode:" + serverNode);
		m_network.SendReliable<ItemData>(serverNode, packet);
		
		string log = "[CLIENT] SendItemStateChanged " +
			"itemId:" + item_id +
				" state:" + state.ToString() + 
				" ownerId:" + owner_id;
		Debug.Log(log);
		dbwin.console().print(log);
	}

	// ================================================================ //
	
	// アイテム情報パケット取得関数.
	public void OnReceiveItemPacket(int node, PacketId id, byte[] data)
	{
		ItemPacket packet = new ItemPacket(data);
		ItemData item = packet.GetPacket();

		// サーバの状態と同期をとる.
		ItemState istate = new ItemState();
		istate.item_id = item.itemId;
		ItemController.State state = (ItemController.State)item.state;
		istate.state = (state == ItemController.State.Dropped)? ItemController.State.None : state;
		istate.owner = item.ownerId;
		if (GlobalParam.getInstance().item_table.ContainsKey(istate.item_id)) {
			GlobalParam.getInstance().item_table.Remove(istate.item_id); 
		}
		GlobalParam.getInstance().item_table.Add(istate.item_id, istate);
		
		string log = "[CLIENT] Receive itempacket [" +
			"itemId:" + item.itemId +
				" state:" + state.ToString() +
				" ownerId:" + item.ownerId + "]";
		Debug.Log(log);
		dbwin.console().print(log);

		if (state == ItemController.State.Picked) {
			Debug.Log("Receive item pick.");
			dbwin.console().print("Receive item pick.");

			// 応答のあったクエリを検索.
			QueryItemPick	query_pick = QueryManager.get().findQuery<QueryItemPick>(x => x.target == item.itemId);

			bool remote_pick = true;
			
			if (query_pick != null) {
				string account_name = PartyControl.get().getLocalPlayer().getAcountID();
				if (item.ownerId == account_name) {                                                 
					Debug.Log("Receive item pick local:" + item.ownerId);
					dbwin.console().print("Receive item pick local:" + item.ownerId);

					item_query_done(query_pick, true);
					remote_pick = false;
				}
				else {
					Debug.Log("Receive item pick remote:" + item.ownerId);
					dbwin.console().print("Receive item pick remote:" + item.ownerId);

					item_query_done(query_pick, false);
				}
			}
			
			if (remote_pick == true) {
				Debug.Log("Remote pick item:" + item.ownerId);
				dbwin.console().print("Remote pick item:" + item.ownerId);

				// リモートキャラクターに取得させる.
				chrController remote = CharacterRoot.getInstance().findPlayer(item.ownerId);
				if (remote) {
					// アイテム取得のクエリ発行.
					QueryItemPick query = remote.cmdItemQueryPick(item.itemId, false, true);
					if (query != null) {
						item_query_done(query, true);
					}
				}
			}

			// アイテムの取得状態を変更する.
			this.setItemState(item.itemId, ItemController.State.Picked, item.ownerId);
		}
		else if (state == ItemController.State.Dropped) {
			Debug.Log("Receive item drop.");	

			// 応答のあったクエリを検索.
			QueryItemDrop	query_drop = QueryManager.get().findQuery<QueryItemDrop>(x => x.target == item.itemId);

			
			bool remote_drop = true;
			
			if (query_drop != null) {
				// リクエストに対するレスポンスがあった.
				string account_name = AccountManager.get().getAccountData(GlobalParam.get().global_account_id).avator_id;
				if (item.ownerId == account_name) { 
					// 自分が取得した.
					Debug.Log("Receive item drop local:" + item.ownerId);
					item_query_done(query_drop, true);
					remote_drop = false;
				}
				else {
					// 相手が取得した.
					Debug.Log("Receive item pick remote:" + item.ownerId);
					item_query_done(query_drop, false);
				}
			}
			
			if (remote_drop == true) {                                                 
				// リモートキャラクターに取得させる.
				chrController remote = CharacterRoot.getInstance().findPlayer(item.ownerId);
				if (remote) {
					// アイテム取得のクエリ発行.
					Debug.Log ("QuetyitemDrop:cmdItemQueryDrop");
				 	remote.cmdItemDrop(item.itemId, false);
				}
			}
		}
		else {
			Debug.Log("Receive item error.");
		}
	}

	public void OnReceiveUseItemPacket(int node, PacketId id, byte[] data)
	{
		ItemUsePacket packet = new ItemUsePacket(data);
		ItemUseData useData = packet.GetPacket();

		Debug.Log ("Receive UseItemPacket:" + useData.userId + " -> " + 
		           								useData.targetId + " (" + useData.itemCategory + ")");

		chrController	user = CharacterRoot.getInstance().findPlayer(useData.userId);

		AccountData	account_data = AccountManager.get().getAccountData(GlobalParam.getInstance().global_account_id);

		if (user != null && account_data.avator_id != useData.userId) {
			Debug.Log("use item. favor:" + useData.itemFavor);

			Item.Favor	favor = new Item.Favor();
			favor.category = (Item.CATEGORY) useData.itemCategory;
			this.useItem(-1, favor, useData.userId, useData.targetId, false);
		}
		else {
			Debug.Log("Receive packet is already done.");
		}
	}
	
	private void item_query_done(QueryBase query, bool success)
	{
		query.set_done(true);
		query.set_success(success);
		
		Debug.Log("cmdItemQuery done.");
	}
	

	// ================================================================ //
	// インスタンス.

	private	static ItemManager	instance = null;

	public static ItemManager	getInstance()
	{
		if(ItemManager.instance == null) {

			ItemManager.instance = GameObject.Find("Item Manager").GetComponent<ItemManager>();
		}

		return(ItemManager.instance);
	}

	public static ItemManager	get()
	{
		return(ItemManager.getInstance());
	}

}
