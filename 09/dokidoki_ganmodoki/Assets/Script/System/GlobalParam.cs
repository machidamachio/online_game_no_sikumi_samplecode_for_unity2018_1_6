using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

// シーンをまたいで使いたいパラメーター.
public class GlobalParam : MonoBehaviour {
	
	public	int			global_account_id	= 0;			// グローバルでユニークなアカウントの id.

	public	bool		is_host				= false;		// ホストとしてプレイしている？.

	// アイテム同期用ハッシュテーブル.
	public Dictionary<string, ItemManager.ItemState> item_table = new Dictionary<string, ItemManager.ItemState>();
	
	// 初期装備保存.
	public SHOT_TYPE[]	shot_type = new SHOT_TYPE[NetConfig.PLAYER_MAX];

	private static		GlobalParam instance = null;

	public int			floor_number = 0;

	public bool			fadein_start = false;

	public bool[]		db_is_connected = new bool[NetConfig.PLAYER_MAX];

	// 通信で使用する乱数のシード値.
	public int			seed = 0;

	// ================================================================ //
	
	public void		create()
	{
		for(int i = 0;i < this.db_is_connected.Length;i++) {

			this.db_is_connected[i] = false;
		}

		for(int i = 0;i < shot_type.Length;i++) {
			this.shot_type[i] = SHOT_TYPE.NEGI;
		}
	}

	// ================================================================ //

	public static GlobalParam	get()
	{
		if(instance == null) {

			GameObject	go = new GameObject("GlobalParam");

			instance = go.AddComponent<GlobalParam>();
			instance.create();

			DontDestroyOnLoad(go);
		}

		return(instance);
	}
	public static GlobalParam	getInstance()
	{
		return(GlobalParam.get());
	}

	// FIXME : floor_number を加算して、次のレベルを決め、ロードもする. 開発用にここに書いたが...
	public void GoToNextLevel()
	{
		floor_number++;

		switch (floor_number)
		{
		case 1:
			SceneManager.LoadScene("WeaponSelectScene");
			break;
		case 2:
			SceneManager.LoadScene("GameScene");
			break;
		case 3:
			SceneManager.LoadScene("BossCene");
			break;
		}
	}
}
