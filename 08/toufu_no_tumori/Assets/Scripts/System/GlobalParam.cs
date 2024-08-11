using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// シーンをまたいで使いたいパラメーター.
public class GlobalParam : MonoBehaviour {
	
	public	int		global_acount_id	 = 0;			// グローバルでユニークなアカウントの id.
	public	string	account_name  	 	 = "Toufuya";	// アカウント名（＝キャラクター名）.
	public	bool	is_in_my_home	  	 = true;		// 自分の庭にいる？.
	public	bool	skip_enter_event	 = true;		// 到着イベントをスキップする？（ゲーム開始時）.
	public	bool	is_host				 = false;		// ホストとしてプレイしている？.
	public	bool	is_remote_in_my_home = false;		// リモートキャラが自分の庭にいる？.
	public	bool	request_move_home	 = false;		// 庭の移動をリクエストした？.
	public	bool	is_connected		 = false;		// 通信相手と接続した？.
	public	bool	is_disconnected		 = false;		// 通信相手と切断した？.

	public	MovingData	local_moving;					// ローカルキャラの引越し情報.
	public	MovingData	remote_moving;					// リモートキャラの引越し情報.

	// アイテム同期用ハッシュテーブル.
	public Dictionary<string, ItemManager.ItemState> item_table = new Dictionary<string, ItemManager.ItemState>();
	

	private static	GlobalParam instance = null;

	public bool		fadein_start = false;

	// ================================================================ //

	public static GlobalParam	getInstance()
	{
		if(instance == null) {

			GameObject	go = new GameObject("GlobalParam");

			instance = go.AddComponent<GlobalParam>();

			DontDestroyOnLoad(go);
		}

		return(instance);
	}
	public static GlobalParam	get()
	{
		return(GlobalParam.getInstance());
	}
}
