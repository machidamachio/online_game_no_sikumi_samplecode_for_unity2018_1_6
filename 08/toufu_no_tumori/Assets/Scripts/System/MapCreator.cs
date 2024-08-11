using UnityEngine;
using System.Collections;

public class MapCreator : MonoBehaviour {

	private	GameObject[]	characteres = null;
	private	GameObject[]	items = null;
	private string			level_texts = "";
	protected string		current_map_name = "Field";

	public TextAsset		level_data = null;
	public bool				is_test_scene = false;		// "TestItemScene"？


	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
		// "TestItemScene" ならセーブ.
		if(this.is_test_scene) {

			this.saveLevel();
		}

		string		map_owner_name = GlobalParam.get().account_name;

		if(!GlobalParam.get().is_in_my_home) {

			map_owner_name = GameRoot.getPartnerAcountName(map_owner_name);
		}

		string	active_map_name   = MapCreator.getHomeMapName(map_owner_name);
		string	inactive_map_name = MapCreator.getHomeMapName(GameRoot.getPartnerAcountName(map_owner_name));

		this.current_map_name = active_map_name;

		GameObject.Find(active_map_name).SetActive(true);
		GameObject.Find(inactive_map_name).SetActive(false);
	}
	
	void	Update()
	{
	}
	
	void	OnGUI()
	{
		if(this.is_test_scene) {

			GUI.TextField(new Rect(10, 10, 400, 400), this.level_texts);
		}
	}

	// ================================================================ //

	public string	getCurrentMapName()
	{
		return(this.current_map_name);
	}

	// キャラクターのホームマップ（自分の村）の名前をゲットする.
	public static string	getHomeMapName(string account_name)
	{
		string		map_name = "";

		if(account_name == "Toufuya") {

			map_name = "Field";

		} else {

			map_name = "Field_v2";
		}

		return(map_name);
	}

	// シーンに配置されているキャラ、アイテムからレベルデーターを作る.
	public void	saveLevel()
	{
		this.characteres = GameObject.FindGameObjectsWithTag("Charactor");
		this.items       = GameObject.FindGameObjectsWithTag("Item");

		this.level_texts = "";

		foreach(var chr in this.characteres) {

			this.level_texts += chr.name;
			this.level_texts += "\t";
			this.level_texts += Mathf.Round(chr.transform.position.x);
			this.level_texts += "\t";
			this.level_texts += Mathf.Round(chr.transform.position.z);
			this.level_texts += "\t";
			this.level_texts += Mathf.Round(chr.transform.rotation.eulerAngles.y);
			

			this.level_texts += "\n";
		}

		foreach(var item in this.items) {

			this.level_texts += item.name;
			this.level_texts += "\t";
			this.level_texts += Mathf.Round(item.transform.position.x);
			this.level_texts += "\t";
			this.level_texts += Mathf.Round(item.transform.position.z);
			

			this.level_texts += "\n";
		}
	}

	// ---------------------------------------------------------------- //
	// レベルデーターをロードして、キャラ/アイテムを配置する.
	public void	loadLevel(string local_account, string net_account, bool create_npc)
	{
		this.level_texts = this.level_data.text;

		string[]	lines = this.level_texts.Split('\n');

		foreach(var line in lines) {

			if(line == "") {

				continue;
			}

			string[]	words = line.Split();

			if(words.Length < 3) {

				continue;
			}

			string	name = words[0];

			if(name.StartsWith("ChrModel_")) {

				// キャラクター.
				this.create_character(words, local_account, net_account, create_npc);

			} if(name.StartsWith("ItemModel_")) {

				bool active = true;

				ItemManager.ItemState istate = ItemManager.get().FindItemState(name);

				// マップに紐づかないアイテムの生成の制御.
				if (current_map_name == "Field" && name == "ItemModel_Yuzu") {
					if (istate.state != ItemController.State.Picked) {
						// 誰も所有していなくてマップに紐づいたアイテムではないので生成しない.
//						continue;
						active = false;
					}
					else {
						if (istate.owner == net_account && GameRoot.get().net_player == null) {
							// 所有しているリモートキャラクターがこのフィールドにはいない.
//							continue;
							active = false;
						}
					}
				}
				else if (current_map_name == "Field_v2" && name == "ItemModel_Negi") {
					if (istate.state != ItemController.State.Picked) {
						// 誰も所有していなくてマップに紐づいたアイテムではないので生成しない.
						//continue;
						active = false;
					}
					else {
						if (istate.owner == net_account && GameRoot.get().net_player == null) {
							// 所有しているリモートキャラクターがこのフィールドにはいない.
							//continue;
							active = false;
						}
					}
				}

				// マップに紐づくアイテムが取得されているときの制御.
				if (current_map_name == "Field" && name == "ItemModel_Negi" ||
				    current_map_name == "Field_v2" && name == "ItemModel_Yuzu") {
					// マップに紐づいたアイテムでも持ち出されているときは生成しない.
					if (istate.state == ItemController.State.Picked &&
					    istate.owner == net_account && 
					    GlobalParam.get().is_in_my_home != GlobalParam.get().is_remote_in_my_home) {
						// 他の人が所有していて別のマップにいるときにアイテムではないので生成しない.
						//continue;
						active = false;
					}
				}

				// アイテム.
				string item_name = this.create_item(words, local_account, active);

				ItemController	new_item = ItemManager.get().findItem(item_name);

				if(new_item != null) {

					bool	is_exportable = false;
					string	production    = "";

					switch(new_item.name) {

						case "Negi":
						{
							is_exportable = true;
							production = "Field";
						}
						break;

						case "Yuzu":
						{
							is_exportable = true;
							production = "Field_v2";
						}
						break;
					}

					new_item.setExportable(is_exportable);
					new_item.setProduction(production);
				}
			}
		}
	}

	// キャラクターを作る.
	protected void		create_character(string[] words, string local_account, string net_account, bool create_npc)
	{
		string	chr_name = words[0];

		chr_name = chr_name.Remove(0, "ChrModel_".Length);

		chrController	chr = null;
		Vector3			pos;
		float			direction;

		if(chr_name == local_account) {

			chr = CharacterRoot.getInstance().createPlayerAsLocal(chr_name);

		} else if(chr_name == net_account) {

			if(create_npc) {

				chr = CharacterRoot.getInstance().createPlayerAsNet(chr_name);
			}

		} else {

			chr = CharacterRoot.getInstance().createNPC(chr_name);
		}

		if(chr != null) {

			pos.x = float.Parse(words[1]);
			pos.y = 0.0f;
			pos.z = float.Parse(words[2]);

			if(words.Length >= 4) {

				direction = float.Parse(words[3]);

			} else {

				direction = 0.0f;
			}

			chr.cmdSetPosition(pos);
			chr.cmdSetDirection(direction);
		}
	}

	// アイテムを作る.
	protected string		create_item(string[] words, string owner, bool active)
	{
		string	item_name = words[0];

		item_name = item_name.Remove(0, "ItemModel_".Length);

		string		item = "";
		Vector3		pos;
		
		item = ItemManager.getInstance().createItem(item_name, owner, active);

		if(item != "") {

			pos.x = float.Parse(words[1]);
			pos.y = 0.0f;
			pos.z = float.Parse(words[2]);

			ItemManager.getInstance().setPositionToItem(item, pos);
		}

		return item;
	}

	// ================================================================ //
	// インスタンス.

	private	static MapCreator	instance = null;

	public static MapCreator	getInstance()
	{
		if(MapCreator.instance == null) {

			MapCreator.instance = GameObject.Find("MapCreator").GetComponent<MapCreator>();
		}

		return(MapCreator.instance);
	}

	public static MapCreator	get()
	{
		return(MapCreator.getInstance());
	}
}
