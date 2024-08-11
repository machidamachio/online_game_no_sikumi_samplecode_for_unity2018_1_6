using UnityEngine;
using System.Collections;
using GameObjectExtension;


public enum YELL_WORD {

	NONE = -1,
	READY = 0,		// 『レディ！』.
	OYATU,			// 『おやつタイム！』.
	OSIMAI,			// 『おしまい』.
	TIMEUP,			// 『タイムアップ～』.

	CAKE_COUNT,		// 順位＋ケーキをとった数.

	NUM,
}

public enum YELL_FONT {

	NONE = -1,

	KATA_RE = 0,	// "レ".
	KATA_DE,		// "デ".
	KATA_S_I,		// "ィ".
	BIKKURI,		// "！".

	HIRA_O,			// "お".
	HIRA_YA,		// "や".
	HIRA_TU,		// "つ".
	KATA_TA,		// "タ".
	KATA_I,			// "イ".
	KATA_MU,		// "ム".

	HIRA_SI,		// "し".
	HIRA_MA,		// "ま".
	HIRA_I,			// "い".

	KATA_A,			// "ア".
	KATA_S_TU,		// "ッ".
	KATA_PU,		// "プ".

	KARA,			// "～".

	HIRA_S_BAN,		// "ばん".
	KATA_S_KO,		// "コ"
	NUM,
}

[System.Serializable]
public class YellFontData {

	public YELL_FONT	font;
	public Texture		texture;
	public bool			is_small;
}


public class Navi : MonoBehaviour {

	// プレハブ.

	public GameObject	ready_yell_prefab;				// 『レディ！』.

	public GameObject	status_window_local_prefab;
	public GameObject	status_window_net_prefab;

	public GameObject	marker_prefab;					// プレイヤーの位置をねぎ指すマーカー.
	public GameObject	kabusan_speech_prefab;			// 武器選択シーンでのかぶさんの吹き出し.

	public GameObject	selecting_icon_prefab;			// 武器選択シーンでの、他のプレイヤーが選択中？.
	public GameObject	cake_timer_prefab;				// ケーキバイキングのタイマー.

	// テクスチャー.

	public Texture[]	face_icon_textures;				// プレイヤーの顔アイコン.
	public Texture[]	cookie_icon_textures;			// クッキー.
	public Texture[]	number_textures;				// 数字　０～９.
	public Texture		lace_texture;					// レース.
	public Texture		toufuya_icon_texture;			// エール用アイコン　とうふやさん.
	public Texture		kabusan_icon_texture;			// エール用アイコン　かぶさん.

	public Texture[]	marker_karada_textures;			// マーカー　体.
	public Texture[]	marker_ude_textures;			// マーカー　腕.
	public Texture[]	marker_ude_under_textures;		// マーカー　腕　アンダー.

	public Texture[]	uun_textures;					// 武器選択中アイコン　考え中.
	public Texture[]	hai_textures;					// 武器選択中アイコン　決まり！.

	// フォント.
	public YellFontData[]	yell_fonts;

	protected YELL_FONT[]	yell_word_ready;
	protected YELL_FONT[]	yell_word_oyatu;
	protected YELL_FONT[]	yell_word_osimai;
	protected YELL_FONT[]	yell_word_timeup;
	protected YELL_FONT[]	yell_word_cake_count;

	//

	protected YellDisp		ready_yell     = null;
	protected KabusanSpeech	kabusan_speech = null;

	protected StatusWindowLocal		stat_win_local;
	protected StatusWindowNet[]		stat_wins_net;

	protected Marker	player_marker;					// プレイヤーの位置をねぎ指すマーカー.

	protected int[]		player_gindex;

	protected SelectingIcon[]	selecting_icons;

	protected CakeTimer			cake_timer;

	// ================================================================ //
	// MonoBehaviour からの継承.

	public SelectingIcon		createSelectingIcon(int account_global_index)
	{
		SelectingIcon	selecting = this.selecting_icon_prefab.instantiate().GetComponent<SelectingIcon>();

		selecting.uun_texture  = this.uun_textures[account_global_index];
		selecting.hai_texture  = this.hai_textures[account_global_index];
		selecting.player_index = account_global_index;
		selecting.create();

		this.selecting_icons[account_global_index] = selecting;

		return(selecting);
	}

	void	Awake()
	{
		this.selecting_icons = new SelectingIcon[NetConfig.PLAYER_MAX];

		// エールで表示する単語.

		this.yell_word_ready = new YELL_FONT[4];
		this.yell_word_ready[0] = YELL_FONT.KATA_RE;
		this.yell_word_ready[1] = YELL_FONT.KATA_DE;
		this.yell_word_ready[2] = YELL_FONT.KATA_S_I;
		this.yell_word_ready[3] = YELL_FONT.BIKKURI;

		this.yell_word_oyatu = new YELL_FONT[7];
		this.yell_word_oyatu[0] = YELL_FONT.HIRA_O;
		this.yell_word_oyatu[1] = YELL_FONT.HIRA_YA;
		this.yell_word_oyatu[2] = YELL_FONT.HIRA_TU;
		this.yell_word_oyatu[3] = YELL_FONT.KATA_TA;
		this.yell_word_oyatu[4] = YELL_FONT.KATA_I;
		this.yell_word_oyatu[5] = YELL_FONT.KATA_MU;
		this.yell_word_oyatu[6] = YELL_FONT.BIKKURI;

		this.yell_word_osimai = new YELL_FONT[4];
		this.yell_word_osimai[0] = YELL_FONT.HIRA_O;
		this.yell_word_osimai[1] = YELL_FONT.HIRA_SI;
		this.yell_word_osimai[2] = YELL_FONT.HIRA_MA;
		this.yell_word_osimai[3] = YELL_FONT.HIRA_I;

		this.yell_word_timeup = new YELL_FONT[7];
		this.yell_word_timeup[0] = YELL_FONT.KATA_TA;
		this.yell_word_timeup[1] = YELL_FONT.KATA_I;
		this.yell_word_timeup[2] = YELL_FONT.KATA_MU;
		this.yell_word_timeup[3] = YELL_FONT.KATA_A;
		this.yell_word_timeup[4] = YELL_FONT.KATA_S_TU;
		this.yell_word_timeup[5] = YELL_FONT.KATA_PU;
		this.yell_word_timeup[6] = YELL_FONT.KARA;

		// ケーキバイキング後のランキング表示.
		// 順位の数字等はダミーの文字をセットしておいて、あとで入れ替える.
		//
		this.yell_word_cake_count = new YELL_FONT[9];
		this.yell_word_cake_count[0] = YELL_FONT.KARA;			// "1" ～ "4" 順位.
		this.yell_word_cake_count[1] = YELL_FONT.HIRA_S_BAN;	// "ばん".
		this.yell_word_cake_count[2] = YELL_FONT.KATA_S_I;		// スペース.
		this.yell_word_cake_count[3] = YELL_FONT.KARA;			// 顔アイコン.
		this.yell_word_cake_count[4] = YELL_FONT.KATA_S_I;		// スペース.
		this.yell_word_cake_count[5] = YELL_FONT.KARA;			// ケーキをとった個数　１００の位.
		this.yell_word_cake_count[6] = YELL_FONT.KARA;			// ケーキをとった個数　　１０の位.
		this.yell_word_cake_count[7] = YELL_FONT.KARA;			// ケーキをとった個数　　　１の位.
		this.yell_word_cake_count[8] = YELL_FONT.KATA_S_KO;		// "コ".
	}

	void	Start()
	{
	}

	void 	Update()
	{
		if(Input.GetKeyDown(KeyCode.A)) {

			//YellDisp	yell = this.createCakeCount(1, 1, 32);

			//yell.setPosition(Vector3.up*64.0f);
			//this.dispatchYell(YELL_WORD.READY);
		}

		if(this.stat_win_local != null) {

			this.stat_win_local.setHP(PartyControl.get().getLocalPlayer().control.vital.getHitPoint());
		}

		//

		if(this.stat_wins_net != null) {

			for(int i = 0;i < this.stat_wins_net.Length;i++) {

				this.stat_wins_net[i].setHP(PartyControl.get().getFriend(i).control.vital.getHitPoint());
			}
		}
	}

	// ================================================================ //

	// エールをゲットする.
	public YellDisp		getYell()
	{
		return(this.ready_yell);
	}

	// エールを削除する.
	public void			destoryYell()
	{
		if(this.ready_yell != null) {

			this.ready_yell.destroy();
			this.ready_yell = null;
		}
	}

	// フォントデーターをゲットする.
	public YellFontData		getYellFontData(YELL_FONT font)
	{
		return(System.Array.Find(this.yell_fonts, x => x.font == font));
	}

	// ステータスウインドウをつくる.
	public void		createStatusWindows()
	{
		int		local_gindex = PartyControl.get().getLocalPlayer().control.global_index;

		int		friend_count = PartyControl.get().getFriendCount();

		int[]	friend_gindex = new int[friend_count];

		for(int i = 0;i < friend_count;i++) {

			friend_gindex[i] = PartyControl.get().getFriend(i).control.global_index;
		}

		// ステータスウインドウ　ローカルプレイヤー用.

		GameObject	go;

		go = GameObject.Instantiate(this.status_window_local_prefab) as GameObject;

		this.stat_win_local = go.GetComponent<StatusWindowLocal>();

		this.stat_win_local.face_icon_texture    = this.face_icon_textures[local_gindex];
		this.stat_win_local.cookie_icon_textures = this.cookie_icon_textures;
		this.stat_win_local.number_textures      = this.number_textures;
		this.stat_win_local.lace_texture         = this.lace_texture;
		this.stat_win_local.create();
		this.stat_win_local.setPosition(new Vector2(640.0f/2.0f - 70.0f, 480.0f/2.0f - 70.0f));

		// ステータスウインドウ　リモートプレイヤー用.


		this.stat_wins_net = new StatusWindowNet[friend_count];

		Vector2		position = new Vector2(640.0f/2.0f - 60.0f, 60.0f);

		for(int i = 0;i < friend_count;i++) {

			go = GameObject.Instantiate(this.status_window_net_prefab) as GameObject;
	
			StatusWindowNet	stat_win_net = go.GetComponent<StatusWindowNet>();
	
			stat_win_net.face_icon_texture    = this.face_icon_textures[friend_gindex[i]];
			stat_win_net.cookie_icon_textures = this.cookie_icon_textures;
			stat_win_net.lace_texture         = this.lace_texture;
			stat_win_net.create();

			stat_win_net.setPosition(position);

			this.stat_wins_net[i] = stat_win_net;

			position.y -= 96.0f;
		}
	}

	// 「レディ！」とかを表示する.
	public void		dispatchYell(YELL_WORD word)
	{
		do {

			if(this.ready_yell != null) {

				break;
			}

			GameObject	go = this.ready_yell_prefab.instantiate();

			if(go == null) {

				break;
			}

			this.ready_yell = go.GetComponent<YellDisp>();
			this.ready_yell.icon_texture = this.toufuya_icon_texture;
			this.ready_yell.word = word;

			switch(word) {

				default:
				case YELL_WORD.READY:
				{
					this.ready_yell.yell_words = this.yell_word_ready;
				}
				break;

				case YELL_WORD.OYATU:
				{
					this.ready_yell.yell_words = this.yell_word_oyatu;
				}
				break;

				case YELL_WORD.OSIMAI:
				{
					this.ready_yell.yell_words = this.yell_word_osimai;
				}
				break;

				case YELL_WORD.TIMEUP:
				{
					this.ready_yell.yell_words = this.yell_word_timeup;
				}
				break;
			}

			this.ready_yell.create();

		} while(false);
	}

	// ケーキバイキングのタイマーをつくる.
	public CakeTimer	createCakeTimer()
	{
		this.cake_timer = this.cake_timer_prefab.instantiate().GetComponent<CakeTimer>();

		return(this.cake_timer);
	}

	public CakeTimer	getCakeTimer()
	{
		return(this.cake_timer);
	}

	// ケーキバイキングのランキング表示をつくる.
	public YellDisp		createCakeCount(int rank, int account_global_index, int count)
	{
		YellDisp	cake_count = null;

		do {

			GameObject	go = this.ready_yell_prefab.instantiate();

			if(go == null) {

				break;
			}

			cake_count = go.GetComponent<YellDisp>();;

			cake_count.yell_words = this.yell_word_cake_count;

			cake_count.icon_texture = this.kabusan_icon_texture;
			cake_count.word = YELL_WORD.CAKE_COUNT;
			cake_count.create();

			cake_count.getMoji(0).moji_texture = this.number_textures[rank];

			// スペースをあけるためのダミー.
			cake_count.getMoji(2).moji_mae_texture = null;
			cake_count.getMoji(2).moji_texture = null;

			cake_count.getMoji(3).moji_texture = this.face_icon_textures[account_global_index];

			// スペースをあけるためのダミー.
			cake_count.getMoji(4).moji_mae_texture = null;
			cake_count.getMoji(4).moji_texture = null;

			if(count >= 100) {

				cake_count.getMoji(5).moji_texture = this.number_textures[(count/100)%10];

			} else {

				cake_count.getMoji(5).moji_texture = null;
			}
			if(count >= 10) {

				cake_count.getMoji(6).moji_texture = this.number_textures[(count/10)%10];

			} else {

				cake_count.getMoji(6).moji_texture = null;
			}

			cake_count.getMoji(7).moji_texture = this.number_textures[(count/1)%10];

		} while(false);

		return(cake_count);
	}

	// プレイヤーマーカーを表示する.
	public void		dispatchPlayerMarker()
	{
		if(this.player_marker == null) {

			GameObject	go = this.marker_prefab.instantiate();

			if(go != null) {

				this.player_marker = go.GetComponent<Marker>();

				// テクスチャーをセットして、生成する.

				int		gidx = PartyControl.get().getLocalPlayer().getGlobalIndex();

				this.player_marker.karada_texture = this.marker_karada_textures[gidx];
				this.player_marker.ude_texture    = this.marker_ude_textures[gidx];
				this.player_marker.under_texture  = this.marker_ude_under_textures[gidx];

				this.player_marker.create();
			}
		}
	}

	// かぶさんの吹き出しを表示する.
	public void		dispatchKabusanSpeech()
	{
		if(this.kabusan_speech == null) {

			GameObject	go = this.kabusan_speech_prefab.instantiate();

			if(go != null) {

				this.kabusan_speech = go.GetComponent<KabusanSpeech>();
			}
		}
	}

	// かぶさんの吹き出しを非表示にする.
	public void		finishKabusanSpeech()
	{
		if(this.kabusan_speech != null) {

			this.kabusan_speech.destroy();
			this.kabusan_speech = null;
		}
	}

	// ================================================================ //
	// インスタンス.

	private	static Navi	instance = null;

	public static Navi	get()
	{
		if(Navi.instance == null) {

			Navi.instance = GameObject.Find("Navi").GetComponent<Navi>();
		}

		return(Navi.instance);
	}
}
