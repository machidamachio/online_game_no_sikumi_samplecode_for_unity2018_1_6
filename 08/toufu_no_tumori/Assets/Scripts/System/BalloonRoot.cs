using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ふきだし.
public class ChattyBalloon {

	public ChattyBalloon(BalloonRoot root)
	{
		this.root     = root;
		this.priority = 0;
	}

	protected BalloonRoot	root = null;

	protected static float	KADO_SIZE = 16.0f;

	protected bool		is_visible = true;

	protected string	text = "";
	protected int		priority;			// 描画プライオリティ（小さい方が前）.
	protected Vector2	position;
	protected Color		color = Color.red;	// ふきだしのいろ.

	public Vector2		balloon_size, text_size;

	public Vector2		draw_pos;
	public Rect			draw_rect;

	protected float		timer = 0.0f;

	// ================================================================ //

	// 毎フレームの実行処理.
	public void		execute()
	{
		if(this.is_visible && this.text != "") {

			this.draw_pos = this.position;

			// ゆらゆら.

			float	cycle = 4.0f;
			float	t = Mathf.Repeat(this.timer, cycle)/cycle;

			this.draw_pos.x += 4.0f*Mathf.Sin(t*Mathf.PI*4.0f);
			this.draw_pos.y += (4.0f*Mathf.Sin(t*Mathf.PI)*4.0f*Mathf.Sin(t*Mathf.PI));

			//

			this.draw_rect.x = this.draw_pos.x - this.text_size.x/2.0f;
			this.draw_rect.y = this.draw_pos.y - this.text_size.y/2.0f;
			this.draw_rect.width  = this.text_size.x;
			this.draw_rect.height = this.text_size.y;


			//

			this.timer += Time.deltaTime;

		} else {

			this.timer = 0.0f;
		}
	}

	// 描画する.
	public void		draw()
	{
		if(this.is_visible && this.text != "") {

			this.disp_balloon(this.draw_pos, this.balloon_size, this.color);

			GUI.color = Color.white;	
			GUI.Label(this.draw_rect, this.text);
		}
	}

	// 吹き出し（文字以外）を表示する.
	protected void		disp_balloon(Vector2 position, Vector2 size, Color color)
	{
		GUI.color = color;

		float		kado_size = KADO_SIZE;
		Vector2		p, s;

		s.x = size.x - kado_size*2.0f;
		s.y = size.y;

		// 真ん中.
		p = position - s/2.0f;
		GUI.DrawTexture(new Rect(p.x, p.y, s.x, s.y), this.root.texture_main);

		// 左.
		p.x = position.x - s.x/2.0f - kado_size;
		p.y = position.y - s.y/2.0f + kado_size;
		GUI.DrawTexture(new Rect(p.x, p.y, kado_size, size.y - kado_size*2.0f), this.root.texture_main);

		// 右.
		p.x = position.x + s.x/2.0f;
		p.y = position.y - s.y/2.0f + kado_size;
		GUI.DrawTexture(new Rect(p.x, p.y, kado_size, size.y - kado_size*2.0f), this.root.texture_main);

		// 左上.
		p.x = position.x - s.x/2.0f - kado_size;
		p.y = position.y - s.y/2.0f;
		GUI.DrawTexture(new Rect(p.x, p.y, kado_size, kado_size), this.root.texture_kado_lu);

		// 右上.
		p.x = position.x + s.x/2.0f;
		p.y = position.y - s.y/2.0f;
		GUI.DrawTexture(new Rect(p.x, p.y, kado_size, kado_size), this.root.texture_kado_ru);

		// 左下.
		p.x = position.x - s.x/2.0f - kado_size;
		p.y = position.y + s.y/2.0f - kado_size;
		GUI.DrawTexture(new Rect(p.x, p.y, kado_size, kado_size), this.root.texture_kado_ld);

		// 右下.
		p.x = position.x + s.x/2.0f;
		p.y = position.y + s.y/2.0f - kado_size;
		GUI.DrawTexture(new Rect(p.x, p.y, kado_size, kado_size), this.root.texture_kado_rd);

		// べろ.
		p.x = position.x - kado_size/2.0f;
		p.y = position.y + s.y/2.0f;
		GUI.DrawTexture(new Rect(p.x, p.y, kado_size, kado_size), this.root.texture_belo);
	}

	// ================================================================ //

	// 表示/非表示する.
	public void		setVisible(bool is_visible)
	{
		this.is_visible = is_visible;
	}

	// テキストをセットする.
	public void		setText(string text)
	{
		this.text = text;

		if(this.text != "") {

			float		font_size   = 13.0f;
			float		font_height = 20.0f;

			this.text_size.x = this.text.Length*font_size;
			this.text_size.y = font_height;

			this.balloon_size.x = text_size.x + KADO_SIZE*2.0f;
			this.balloon_size.y = text_size.y + KADO_SIZE;
		}
	}

	// テキストをクリアーする（非表示にする）.
	public void		clearText()
	{
		this.text = "";
	}

	// テキストをゲットする.
	public string	getText()
	{
		return(this.text);
	}

	// 位置をセットする.
	public void		setPosition(Vector2 position)
	{
		this.position = position;
	}

	// ふきだしの色をセットする.
	public void		setColor(Color color)
	{
		this.color = color;
	}

	// 描画プライオリティをセットする.
	public void		setPriority(int priority)
	{
		this.priority = priority;
		this.root.setSortRequired();
	}

	// 描画プライオリティをゲットする.
	public int		getPriority()
	{
		return(this.priority);
	}

	// ================================================================ //


};

// ふきだし管理クラス.
public class BalloonRoot : MonoBehaviour {

	public Texture texture_main    = null;
	public Texture texture_belo    = null;
	public Texture texture_kado_lu = null;
	public Texture texture_kado_ru = null;
	public Texture texture_kado_ld = null;
	public Texture texture_kado_rd = null;

	public List<ChattyBalloon>	balloons = new List<ChattyBalloon>();

	protected bool		is_sort_required = false;

	public void		setSortRequired()
	{
		this.is_sort_required = true;
	}

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
		this.is_sort_required = true;
	}
	
	void	Update()
	{
		// 描画順でソート.
		if(this.is_sort_required) {

			// プライオリティの数値が小さいものが後に描画されるよう、
			// プライオリティの大きい順にソートする.
			this.balloons.Sort((x, y) => -(x.getPriority() - y.getPriority()));

			this.is_sort_required = false;
		}

		foreach(var balloon in this.balloons) {

			balloon.execute();
		}
	}

	void	OnGUI()
	{
		Color	color_org = GUI.color;

		foreach(var balloon in this.balloons) {

			balloon.draw();
		}

		GUI.color = color_org;
	}

	// ふきだしをつくる.
	public ChattyBalloon	createBalloon()
	{
		ChattyBalloon	balloon = new ChattyBalloon(this);

		balloon.setPriority(0);

		this.balloons.Add(balloon);

		return(balloon);
	}

	// ================================================================ //
	// インスタンス.

	private	static BalloonRoot	instance = null;

	public static BalloonRoot	get()
	{
		if(BalloonRoot.instance == null) {

			BalloonRoot.instance = GameObject.Find("BalloonRoot").GetComponent<BalloonRoot>();
		}

		return(BalloonRoot.instance);
	}
}
