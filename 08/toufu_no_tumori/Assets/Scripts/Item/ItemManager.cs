using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ItemManager : MonoBehaviour {

	public	GameObject		item_base_prefab = null;

	public	GameObject[]	item_model_prefabs;
	
	// ---------------------------------------------------------------- //

	private List<QueryBase>		queries = new List<QueryBase>();		// クエリー.

	// ---------------------------------------------------------------- //
	
	public struct ItemState
	{
		public string					item_id;		// ユニークな id.
		public ItemController.State		state;			// ネットワーク的なステート.
		public string 					owner;			// 作った人（アカウント名）.
	}

	private Network 			m_network = null;


	// MonoBehaviour からの継承.
	// ================================================================ //

	void	Start()
	{
		// Networkクラスのコンポーネントを取得.
		GameObject obj = GameObject.Find("Network");

		if(obj != null) {
			m_network = obj.GetComponent<Network>();
			m_network.RegisterReceiveNotification(PacketId.ItemData, OnReceiveItemPacket);
		}
	}
	
	void	Update()
	{
		this.process_item_query();
	}

	// クエリーの更新.
	protected void	process_item_query()
	{
		// フェールセーフ＆開発用.
		foreach(var query in this.queries) {

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
	
	// アイテムを作る.
	public string		createItem(string type, string owner, bool active = true)
	{
		string	ret = "";

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

			item.id            = type;
			item.owner_account = owner;
			item.model         = item_model_go;
	
			item.transform.parent = this.gameObject.transform;
			item.name             = item.id;
	
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
			item.behavior.is_active = active;
			item.behavior.initialize();

			ret = item.id;

		} while(false);

		return(ret);
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

			float		radius = item.GetComponent<SphereCollider>().radius;
			Ray			ray    = new Ray(position + Vector3.up*radius*2.0f, Vector3.down);
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

	// アイテムの表示/非表示を設定
	public void 	setVisible(string name, bool visible)
	{
		ItemController item = ItemManager.get().findItem(name);
		
		if (item == null) {
			return;
		}
		
		item.setVisible(visible);
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

		} while(false);

		return(item);
	}

	// 持っているアイテムを捨てる.
	public void 	dropItem(string owner_id, ItemController item)
	{
		item.picker = "";
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

		GameObject[]	items = GameObject.FindGameObjectsWithTag("Item");

		GameObject		go = System.Array.Find(items, x => x.name == name);

		if(go != null) {

			item = go.GetComponent<ItemController>();
		}

		return(item);
	}

	// アイテムを成長状態にする（拾えるようにする）.
	public void		finishGrowingItem(string item_id)
	{
		ItemController	item = null;
		
		do {
	
			item = this.find_item(item_id);
			
			if(item == null) {
				
				break;
			}
	
			item.finishGrowing();

		} while(false);
	}

	// アイテムのアクティブ/非アクティブ設定.
	public void 	activeItme(string item_id, bool active)
	{
		ItemController	item = null;
		
		do {
			
			item = this.find_item(item_id);
			
			if(item == null) {
				
				break;
			}

			item.behavior.activeItem(active);
			
		} while(false);
	}

	// ================================================================ //
	// ビヘイビアー用のコマンド.
	// クエリー系.

	// 落ちているアイテムを拾っていい？.
	public QueryItemPick	queryPickItem(string owner_id, string item_id, bool local, bool force)
	{
		ItemController		item = null;
		QueryItemPick		query = null;
		bool				notify = false;

		do {
			
			item = this.find_item(item_id);
			
			if(item == null) {
				break;
			}
		
			// 成長中のものとかは拾えない.
			if(!item.isPickable() && !force) {
				
				break;
			}
			
			// もう誰かが持ち中のものは拾えない.
			if(item.picker != "") {
				
				break;
			}
			
			query = new QueryItemPick(item_id);
			
			this.queries.Add(query);
			notify = true;
		} while(false);

		// ローカルキャラクターがアイテムのクエリを発行した時だけサーバへ送信します.
		if(notify && local) {
			// アイテムのステートをネットに送る.
			SendItemStateChanged(item_id, ItemController.State.PickingUp, owner_id);
		}

		return(query);
	}

	// 持ってる中のアイテムを捨ていい？.
	public QueryItemDrop	queryDropItem(string owner_id, ItemController item, bool local)
	{
		QueryItemDrop		query = null;

		do {
			
			// 自分のものじゃないものは拾えない.
			if(item.picker != owner_id) {

				break;
			}

			query = new QueryItemDrop(item.id);

			this.queries.Add(query);

		} while(false);

		if(item != null && local) {
			// アイテムのステートをネットに送る.
			SendItemStateChanged(item.id, ItemController.State.Dropping, owner_id);
		}

		return(query);
	}


	// ================================================================ //

	public ItemState FindItemState(string item_name) 
	{
		foreach (ItemState state in GlobalParam.get().item_table.Values) {
			if (item_name.Contains(state.item_id)) {
				return state;
			}
		}

		ItemState dummy;
		dummy.item_id = "";
		dummy.state = ItemController.State.None;
		dummy.owner = "";

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
		
		m_network.SendReliable<ItemData>(packet);

		string log = "[CLIENT] SendItemStateChanged " +
			"itemId:" + item_id +
				" state:" + state.ToString() + 
				" ownerId:" + owner_id;
		Debug.Log(log);
	}

	// アイテム情報パケット取得関数.
	public void OnReceiveItemPacket(PacketId id, byte[] data)
	{
		ItemPacket packet = new ItemPacket(data);
		ItemData item = packet.GetPacket();

		// サーバの状態と同期をとる.
		ItemState istate = new ItemState();
		istate.item_id = item.itemId;
		ItemController.State state = (ItemController.State)item.state;
		istate.state = (state == ItemController.State.Dropped)? ItemController.State.None : state;
		istate.owner = item.ownerId;
		if (GlobalParam.get().item_table.ContainsKey(item.itemId)) {
			GlobalParam.get().item_table.Remove(istate.item_id); 
		}
		GlobalParam.get().item_table.Add(istate.item_id, istate);

		string log = "[CLIENT] Receive itempacket. " +
			"itemId:" + item.itemId +
				" state:" + state.ToString() +
				" ownerId:" + item.ownerId;
		Debug.Log(log);
		
		if (state == ItemController.State.Picked) {
			Debug.Log("Receive item pick.");

			// 応答のあったクエリを検索.
			QueryItemPick	query_pick = null;
			foreach(var query in this.queries) {
				QueryItemPick pick = query as QueryItemPick;
				if (pick != null && pick.target == item.itemId) {
					query_pick = pick;
					break;
				}
			}

			bool remote_pick = true;

			if (query_pick != null) {
				if (item.ownerId == GlobalParam.get().account_name) {                                                 
					Debug.Log("Receive item pick local:" + item.ownerId);
					item_query_done(query_pick, true);
					remote_pick = false;
				}
				else {
					Debug.Log("Receive item pick remote:" + item.ownerId);
					item_query_done(query_pick, false);
				}
			}

			if (remote_pick == true) {
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
		}
		else if (state == ItemController.State.Dropped) {
			Debug.Log("Receive item drop.");

			// 応答のあったクエリを検索.
			QueryItemDrop	query_drop = null;
			foreach(var query in this.queries) {
				QueryItemDrop drop = query as QueryItemDrop;
				if (drop != null && drop.target == item.itemId) {
					query_drop = drop;
					break;
				}
			}

			bool remote_drop = true;

			if (query_drop != null) {
				// リクエストに対するレスポンスがあった.
				if (item.ownerId == GlobalParam.get().account_name) { 
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
					QueryItemDrop query = remote.cmdItemQueryDrop(false);
					if (query != null) {
						query.is_drop_done = true;
						item_query_done(query, true);
					}
				}
			}
		}
		else {
			Debug.Log("Receive item error.");
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
