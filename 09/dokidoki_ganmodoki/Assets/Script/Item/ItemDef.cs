using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Item {


public enum CATEGORY {

	NONE = -1,

	KEY = 0,		// ドアのカギ.
	FLOOR_KEY,		// フロアー移動ドアのカギ.
	SODA_ICE,		// ソーダアイス　使うと体力回復.
	CANDY,			// ぺろぺろキャンディー.
	FOOD,			// 食べ物　その場で体力回復.
	WEAPON,			// ぶき.
	ETC,			// その他.

	NUM,
};

// Slot の ID
public enum SLOT_TYPE {

	NONE = -1,

	KEY = 0,			// ルームキー.
	FLOOR_KEY,			// フロアーキー.
	CANDY,				// ぺろぺろキャンディー.
	MISC,				// はんよう.

	NUM,
};

// アイテムを持っているときに、キャラクターに付与される特典.
public class Favor {

	public Favor()
	{
		this.option0 = (object)"";
	}

	public Favor	clone()
	{
		return(this.MemberwiseClone() as Favor);
	}

	public CATEGORY	category = CATEGORY.NONE;

	public object	option0;		// オプショナルなパラメーター　その１　（アイスの当たり/はずれなど）
};

// キャラクターがアイテムを持てるところ.
public class Slot {

	public string			item_id  = "";
	public Favor			favor    = null;		// アイテムの効果.
	public bool				is_using = false;		// 使用中？.

	public Slot()
	{
		this.initialize();
	}

	// 初期化する.
	public void	initialize()
	{
		this.item_id = "";
		this.favor = null;
		this.is_using = false;
	}

	// 空き？.
	public bool isVacant()
	{
		return(this.favor == null);
	}
};
	
// ひとりのキャラクターが持てるアイテム.
public class SlotArray {

	public const int	MISC_NUM = 2;

	public Slot			candy;		// ぺろぺろキャンディー.
	public List<Slot>	miscs;		// 汎用.

	public SlotArray()
	{
		this.candy = new Slot();
		this.miscs = new List<Slot>();

		for(int i = 0;i < MISC_NUM;i++) {

			this.miscs.Add(new Slot());
		}
	}

	// はん用の空きスロットを探す.
	public int	getEmptyMiscSlot()
	{
		int		slot_index = this.miscs.FindIndex(x => x.isVacant());

		return(slot_index);
	}
};

// キーのいろ.
public enum KEY_COLOR {

	NONE = -1,

	PINK = 0,		// "key00"
	YELLOW,			// "key01"
	GREEN,			// "key02"
	BLUE,			// "key03"

	PURPLE,			// "key04" フロアーキー.

	NUM,
};

// キー用のメソッド.
public class Key {

	// キーのタイプ名（プレハブの名前）をゲットする.
	public static string	getTypeName(KEY_COLOR color)
	{
		string	type_name = "key" + ((int)color).ToString("D2");

		return(type_name);
	}

	// タイプ名からキーカラーをゲットする.
	public static KEY_COLOR	getColorFromTypeName(string type_name)
	{
		KEY_COLOR	color = KEY_COLOR.NONE;

		do {

			if(!type_name.StartsWith("key")) {

				break;
			}

			string	color_string = type_name.Substring(3, 2);
			int		color_int;

			if(!int.TryParse(color_string, out color_int)) {

				break;
			}

			color = (KEY_COLOR)color_int;

		} while(false);

		return(color);
	}

	// キーのインスタンスの名前をゲットする.
	public static string	getInstanceName(KEY_COLOR color, Map.RoomIndex room_index)
	{
		string	instance_name = Key.getTypeName(color) + "_" + room_index.x.ToString() + room_index.z.ToString();

		return(instance_name);
	}

	// インスタンス名からキーカラーをゲットする.
	public static KEY_COLOR	getColorFromInstanceName(string name)
	{
		return(Key.getColorFromTypeName(name));
	}
}

// ショットチェンジアイテム用のメソッド.
public class Weapon {

	// アイテムの名まえからショットタイプをゲットする.
	public static SHOT_TYPE		getShotType(string name)
	{
		SHOT_TYPE	shot_type = SHOT_TYPE.NONE;

		do {
			char[] delimiterChars = { '_', '.' };
				string[] item_name = name.Split(delimiterChars);

			if(item_name.Length < 3) {

				break;
			}
			if(item_name[0] != "shot") {

				break;
			}

			switch(item_name[1]) {
	
				case "negi":
				{
					shot_type = SHOT_TYPE.NEGI;
				}
				break;
	
				case "yuzu":
				{
					shot_type = SHOT_TYPE.YUZU;
				}
				break;
			}

		} while(false);

		return(shot_type);
	}
}

} // namespace Item {

public class ItemDef : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
