using UnityEngine;
using System.Collections;


// クエリーの結果を受け取るためのクラス.
public  class QueryBase {

	public QueryBase()
	{
	}

	public virtual string	getType()	{ return("null"); }

	public bool		isDone()	{ return(this.is_done); }
	public bool		isSuccess() { return(this.is_success); }
	public bool		isExpired() { return(this.is_expired); }

	public void		set_done(bool is_done)    	 { this.is_done = is_done; }
	public void		set_success(bool is_success) { this.is_success = is_success; }
	public void		set_expired(bool is_expired) { this.is_expired = is_expired; }

	protected bool		is_done    = false;		// コマンドの実行がおわった？.
	protected bool		is_success = false;		// 成功した？（pick ならアイテムが拾えた）.
	protected bool		is_expired = false;		// もういらない？.

	public float	timer;				// テスト用.
};

// クエリー：アイテムをひろうとき.
public  class QueryItemPick : QueryBase {

	public QueryItemPick(string target)
	{
		this.target = target;
	}

	public override string	getType()	{ return("item.pick"); }

	public string			target  = null;
	public bool				is_anon = false;	// 演出をカットする？.
};

// クエリー：アイテムをすてるとき.
public  class QueryItemDrop : QueryBase {

	public QueryItemDrop(string target)
	{
		this.target = target;
		this.is_drop_done = false;
	}

	public override string	getType()	{ return("item.drop"); }

	public string			target;
	public bool				is_drop_done;		// true ... すでにドロップ済み（サーバーに通達するだけ）.
};

// クエリー：引越しはじめ.
public  class QueryHouseMoveStart : QueryBase {

	public QueryHouseMoveStart(string target)
	{
		this.target = target;
	}

	public override string	getType()	{ return("house-move.start"); }

	public string			target;
};

// クエリー：引越しおしまい.
public  class QueryHouseMoveEnd : QueryBase {

	public QueryHouseMoveEnd()
	{
	}

	public override string	getType()	{ return("house-move.end"); }

};

// クエリー：チャット.
public  class QueryTalk : QueryBase {

	public QueryTalk(string words)
	{
		this.words = words;
	}

	public override string	getType()	{ return("talk"); }

	public string			words;
};

// ---------------------------------------------------------------- //

// 特にやることないかも？.
public class QueryManager : MonoBehaviour {

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start() 
	{
		if(dbwin.root().getWindow("query") == null) {

			this.create_debug_window();
		}
	}
	
	void	Update()
	{
	}

	protected void		create_debug_window()
	{
		var		window = dbwin.root().createWindow("query");

		window.createButton("ひろう")
			.setOnPress(() =>
			{
				chrBehaviorLocal	player = CharacterRoot.get().findCharacter<chrBehaviorLocal>(GameRoot.getInstance().account_name_local);

				player.controll.cmdItemQueryPick("Tarai");
			});

		window.createButton("すてる")
			.setOnPress(() =>
			{
				chrBehaviorLocal	player = CharacterRoot.get().findCharacter<chrBehaviorLocal>(GameRoot.getInstance().account_name_local);

				player.controll.cmdItemQueryDrop();
			});

		window.createButton("ふきだし")
			.setOnPress(() =>
			{
				//chrBehaviorLocal	player = CharacterRoot.get().findCharacter<chrBehaviorLocal>(GameRoot.getInstance().account_name_local);
				chrBehaviorNet	player = CharacterRoot.get().findCharacter<chrBehaviorNet>("Daizuya");

				player.controll.cmdQueryTalk("遠くの人と Talk する", true);
			});

		window.createButton("引越しはじめ")
			.setOnPress(() =>
			{
				chrBehaviorLocal	player = CharacterRoot.get().findCharacter<chrBehaviorLocal>(GameRoot.getInstance().account_name_local);
				//chrBehaviorNet		player = CharacterRoot.get().findCharacter<chrBehaviorNet>(GameRoot.getInstance().account_name_net);

				player.controll.cmdQueryHouseMoveStart("House1");
			});

		window.createButton("引越しおしまい")
			.setOnPress(() =>
			{
				chrBehaviorLocal	player = CharacterRoot.get().findCharacter<chrBehaviorLocal>(GameRoot.getInstance().account_name_local);
				//chrBehaviorNet		player = CharacterRoot.get().findCharacter<chrBehaviorNet>(GameRoot.getInstance().account_name_net);

				player.controll.cmdQueryHouseMoveEnd();
			});
	}

	// ================================================================ //
	// インスタンス.

	private	static QueryManager	instance = null;

	public static QueryManager	getInstance()
	{
		if(QueryManager.instance == null) {

			QueryManager.instance = GameObject.Find("GameRoot").GetComponent<QueryManager>();
		}

		return(QueryManager.instance);
	}
}
