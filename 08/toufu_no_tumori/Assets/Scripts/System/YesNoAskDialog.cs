using UnityEngine;
using System.Collections;

// 『はい/いいえ』選択ダイアローグ.
public class YesNoAskDialog : MonoBehaviour {

	public enum SELECTION {

		NONE = -1,

		YES = 0,
		NO,

		NUM,
	};
	protected SELECTION	selection = SELECTION.NONE;

	protected enum STEP {

		NONE = -1,

		IDLE = 0,			// 実行中じゃない.
		DISPATCH,
		SELECTED,			// どちらかのボタンが押された.
		CLOSE,

		NUM,
	};
	protected Step<STEP>	step = new Step<STEP>(STEP.NONE);

	protected Rect	input_forbidden_area = new Rect((Screen.width - 300)/2.0f, 100, 300, 150);

	public Texture	white_texture;

	protected string	text     = "どっち？";		// メッセージのテキスト.
	protected string	yes_text = "こっち！";		// 「Yes」ボタンのテキスト.
	protected string	no_text  = "あっち！";		// 「No」ボタンのテキスト.
	protected Rect		text_rect = new Rect(0.0f, 0.0f, 100.0f, 10.0f);

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
		this.step.set_next(STEP.IDLE);
	}
	
	void	Update()
	{
		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			case STEP.DISPATCH:
			{
				if(this.selection != SELECTION.NONE) {

					this.step.set_next(STEP.SELECTED);
				}
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {

				case STEP.DISPATCH:
				{
					this.selection = SELECTION.NONE;

					CharacterRoot.get().getGameInput().appendForbiddenArea(this.input_forbidden_area);
				}
				break;

				case STEP.CLOSE:
				{
					CharacterRoot.get().getGameInput().removeForbiddenArea(this.input_forbidden_area);
					this.step.set_next(STEP.IDLE);
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			case STEP.IDLE:
			{
			}
			break;
		}
	}

	void	OnGUI()
	{
		switch(this.step.get_current()) {

			case STEP.DISPATCH:
			{
				Color	org_color = GUI.color;

				// 下じき.
				GUI.color = new Color(1.0f, 1.0f, 1.0f, 0.5f);

				GUI.DrawTexture(this.input_forbidden_area, this.white_texture);

				// テキスト.
				GUI.color = new Color(0.5f, 0.1f, 0.1f, 0.5f);

				GUI.Label(this.text_rect, text);

				GUI.color = org_color;

				if(GUI.Button(new Rect(190, 200, 100, 20), this.yes_text)) {

					this.selection = SELECTION.YES;
				}
				if(GUI.Button(new Rect(350, 200, 100, 20), this.no_text)) {

					this.selection = SELECTION.NO;
				}
			}
			break;
		}
	}

	// ================================================================ //

	// メッセージのテキストをセットする.
	public void		setText(string text)
	{
		this.text = text;

		float	font_width  = 13.0f;
		float	font_height = 20.0f;

		this.text_rect.width  = font_width*this.text.Length;
		this.text_rect.height = font_height;
		this.text_rect.x = Screen.width/2.0f - this.text_rect.width/2.0f;
		this.text_rect.y = 150.0f;
	}

	// ボタンのテキストをセットする.
	public void		setButtonText(string yes_text, string no_text)
	{
		this.yes_text = yes_text;
		this.no_text  = no_text;
	}

	public void		dispatch()
	{
		this.step.set_next(STEP.DISPATCH);
	}
	public void		close()
	{
		this.step.set_next(STEP.CLOSE);
	}

	public bool		isSelected()
	{
		bool	is_selected = false;

		if(this.getSelection() != SELECTION.NONE) {

			is_selected = true;
		}

		return(is_selected);
	}

	public SELECTION	getSelection()
	{
		SELECTION	selection = SELECTION.NONE;

		if(this.step.get_current() == STEP.SELECTED) {

			selection = this.selection;
		}

		return(selection);
	}

	// ================================================================ //

	private	static YesNoAskDialog	instance = null;

	public static YesNoAskDialog	get()
	{
		if(YesNoAskDialog.instance == null) {

			YesNoAskDialog.instance = GameObject.Find("GameRoot").GetComponent<YesNoAskDialog>();
		}

		return(YesNoAskDialog.instance);
	}
}
