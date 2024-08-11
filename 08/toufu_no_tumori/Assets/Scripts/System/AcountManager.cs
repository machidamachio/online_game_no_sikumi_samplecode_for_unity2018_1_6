using UnityEngine;
using System.Collections;

public class AcountData {

	public string	account_id;			// アカウント名。今回はキャラクター名で固定（"Toufuya" など）.
	public int		global_index;		// 全端末通してユニークなインデックス.
	public int		local_index;		// 端末内でのインデックス。ローカルプレイヤーが０。端末ごとに異なる.

	public string	avator_id;			// アバター名（"Toufuya" など）。account_id と同じ.
	public string	label;				// 日本語表記.

	public Color	favorite_color;		// お気に入りのいろ.
}

public class AcountManager : MonoBehaviour {

	protected AcountData[]	account_datas = null;

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
		this.account_datas = new AcountData[4];

		for(int i = 0;i < 4;i++) {

			this.account_datas[i] = new AcountData();
			this.account_datas[i].global_index = i;

			// 実際にプレイヤーが端末に接続したときに決める.
			this.account_datas[i].local_index  = -1;
		}

		this.account_datas[0].account_id = "Toufuya";
		this.account_datas[0].avator_id = this.account_datas[0].account_id;
		this.account_datas[0].label     = "とうふや";
		this.account_datas[0].favorite_color = Color.cyan;

		this.account_datas[1].account_id = "Daizuya";
		this.account_datas[1].avator_id = this.account_datas[1].account_id;
		this.account_datas[1].label     = "だいずや";
		this.account_datas[1].favorite_color = Color.green;

		this.account_datas[2].account_id = "Zundaya";
		this.account_datas[2].avator_id = this.account_datas[2].account_id;
		this.account_datas[2].label     = "ずんだや";
		this.account_datas[2].favorite_color = Color.cyan;

		this.account_datas[3].account_id = "Irimameya";
		this.account_datas[3].avator_id = this.account_datas[3].account_id;
		this.account_datas[3].label     = "いりまめや";
		this.account_datas[3].favorite_color = Color.green;
	}
	
	void	Update()
	{
	}

	// ================================================================ //

	public AcountData		getAccountData(int global_index)
	{
		return(this.account_datas[global_index]);
	}

	public AcountData		getAccountData(string account_id)
	{
		foreach (AcountData account in this.account_datas) {
			if (account.account_id == account_id) {
				return account;
			}
		}

		return this.account_datas[0];
	}

	// ================================================================ //

	private	static AcountManager	instance = null;

	public static AcountManager	getInstance()
	{
		if(AcountManager.instance == null) {

			AcountManager.instance = GameObject.Find("GameRoot").GetComponent<AcountManager>();
		}

		return(AcountManager.instance);
	}

	public static AcountManager	get()
	{
		return(AcountManager.getInstance());
	}

}

