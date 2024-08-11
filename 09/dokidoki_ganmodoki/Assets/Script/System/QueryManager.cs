using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// クエリーの結果を受け取るためのクラス.
public  class QueryBase {

	public QueryBase(string account_id)
	{
		this.account_id = account_id;
	}

	public virtual string	getType()	{ return("null"); }

	public bool		isDone()		{ return(this.is_done); }
	public bool		isSuccess()		{ return(this.is_success); }
	public bool		isExpired()		{ return(this.is_expired); }
	public bool		isNotifyOnly()	{ return(this.is_notify_only);  }

	public void		setNotifyOnly(bool is_notify_only) { this.is_notify_only = is_notify_only; }

	public void		set_done(bool is_done)    	     { this.is_done = is_done; }
	public void		set_success(bool is_success)     { this.is_success = is_success; }
	public void		set_expired(bool is_expired)     { this.is_expired = is_expired; }

	protected bool		is_done    = false;		// コマンドの実行がおわった？.
	protected bool		is_success = false;		// 成功した？（pick ならアイテムが拾えた）.
	protected bool		is_expired = false;		// もういらない？.
	protected bool		is_notify_only = false;	// 通知のみ　同期待ちをしない.

	public string		account_id;				// このクエリーを作成した人.
	public float		timer;	
	public float		timeout = 5.0f;			// このクエーのタイムアウト.
};

// クエリー：アイテムをひろうとき.
public  class QueryItemPick : QueryBase {

	public QueryItemPick(string acount_id, string target) : base(acount_id)
	{
		this.target = target;
	}

	public override string	getType()	{ return("item.pick"); }

	public string			target  = null;
};

// クエリー：アイテムを使ったとき.
public  class QueryItemDrop : QueryBase {

	public QueryItemDrop(string acount_id, string target) : base(acount_id)
	{
		this.target = target;
		this.is_notify_only = true;
	}

	public override string	getType()	{ return("item.drop"); }

	public string			target;
	public bool				is_drop_done;		// true ... すでにドロップ済み（サーバーに通達するだけ）.
};

// クエリー：モンスターリスポーン.
public  class QuerySpawn : QueryBase {
	
	public QuerySpawn(string acount_id, string monster_id) : base(acount_id)
	{
		this.monster_id = monster_id;
	}
	
	public override string	getType()	{ return("spawn"); }
	
	public string			monster_id;
};

// クエリー：チャット.
public  class QueryTalk : QueryBase {

	public QueryTalk(string acount_id, string words) : base(acount_id)
	{
		this.words = words;
	}

	public override string	getType()	{ return("talk"); }

	public string			words;
};

// クエリー：武器選択シーンでドアに入った.
public  class QuerySelectDone : QueryBase {

	public QuerySelectDone(string acount_id) : base(acount_id)
	{
	}

	public override string	getType()	{ return("select.done"); }
};

// クエリー：武器選択シーンおしまい（全員ドアに入った）.
public  class QuerySelectFinish : QueryBase {

	public QuerySelectFinish(string acount_id) : base(acount_id)
	{
	}

	public override string	getType()	{ return("select.finish"); }
};

// クエリー：召喚獣.
public  class QuerySummonBeast : QueryBase {

	public QuerySummonBeast(string acount_id, string type) : base(acount_id)
	{
		this.is_notify_only = true;
		this.type = type;
	}

	public override string	getType()	{ return("summon.beast"); }

	public string	type = "Dog";		// 召喚獣のタイプ（いぬ or ねこ）.
};

// クエリー：ケーキをとった数.
public  class QueryCakeCount: QueryBase {

	public QueryCakeCount(string acount_id, int count) : base(acount_id)
	{
		this.count = count;
	}

	public override string	getType()	{ return("cake.count"); }

	public int	count = 0;			// ケーキをとった数.
};

// ---------------------------------------------------------------- //

// クエリーマネージャー.
public class QueryManager : MonoBehaviour {

	protected Network 				m_network = null;

	protected List<QueryBase>		queries = new List<QueryBase>();		// クエリー.

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start() 
	{
		if(dbwin.root().getWindow("query") == null) {

			this.create_debug_window();
		}

		// Networkクラスのコンポーネントを取得.
		GameObject	obj = GameObject.Find("Network");
		
		if(obj != null) {

			this.m_network = obj.GetComponent<Network>();
		}
	}
	
	void	Update()
	{
		this.process_query();
	}

	// クエリーを登録する.
	public void	registerQuery(QueryBase query)
	{
		// ケーキバイキング中は通信しなくてもケーキが拾えるように.
		if(GameRoot.get().isNowCakeBiking()) {

			if(query is QueryItemPick) {

				query.set_done(true);
				query.set_success(true);
			}
		}

		this.queries.Add(query);
	}

	// 完了したクエリーを探す.
	public List<QueryBase>	findDoneQuery(string account_id)
	{
		List<QueryBase>		done_queries = this.queries.FindAll(x => (x.account_id == account_id && x.isDone()));

		return(done_queries);
	}

	// 特定の型の、完了したクエリーを探す.
	public List<QueryBase>	findDoneQuery<T>() where T : QueryBase
	{
		List<QueryBase>		done_queries = this.queries.FindAll(x => ((x as T) != null && x.isDone()));

		return(done_queries);
	}

	// クエリーを探す.
	public T	findQuery<T>(System.Predicate<T> pred) where T : QueryBase
	{
		T		query = null;

		query = this.queries.Find(x => ((x as T) != null) && pred(x as T)) as T;

		return(query);
	}

	// クエリーの更新.
	protected void	process_query()
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
				if(query.timer > query.timeout) {

					query.set_done(true);
					query.set_success(false);
				}
			}
		}

		// 使い終わったクエリーを削除する.
		this.queries.RemoveAll(x => x.isExpired());
	}

	protected void		create_debug_window()
	{

		var		window = dbwin.root().createWindow("query");

		window.createButton("select.done")
			.setOnPress(() =>
			{
				for(int i = 0;i < NetConfig.PLAYER_MAX;i++) {

					var	query = new QuerySelectDone(AccountManager.get().getAccountData(i).account_id);

					QueryManager.get().registerQuery(query);
				}
			});
		window.createButton("select.finish")
			.setOnPress(() =>
			{
				var	query = new QuerySelectFinish("Daizuya");

				QueryManager.get().registerQuery(query);
			});

		window.createButton("summon dog")
			.setOnPress(() =>
			{
				QuerySummonBeast	query_summon = new QuerySummonBeast("Daizuya", "Dog");

				QueryManager.get().registerQuery(query_summon);
			});

		window.createButton("summon neko")
			.setOnPress(() =>
			{
				QuerySummonBeast	query_summon = new QuerySummonBeast("Daizuya", "Neko");

				QueryManager.get().registerQuery(query_summon);
			});

		window.createButton("cake count")
			.setOnPress(() =>
			{
				for(int i = 0;i < PartyControl.get().getFriendCount();i++) {

					chrBehaviorPlayer	friend = PartyControl.get().getFriend(i);

					QueryCakeCount	query_cake = new QueryCakeCount(friend.getAcountID(), (i + 1)*10);

					QueryManager.get().registerQuery(query_cake);
				}
			});

#if false
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

#endif
	}

	// ================================================================ //
	// インスタンス.

	private	static QueryManager	instance = null;

	public static QueryManager	get()
	{
		if(QueryManager.instance == null) {

			QueryManager.instance = GameObject.Find("GameRoot").GetComponent<QueryManager>();
		}

		return(QueryManager.instance);
	}
}
