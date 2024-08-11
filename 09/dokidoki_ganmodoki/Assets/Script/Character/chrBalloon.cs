using UnityEngine;
using System.Collections;

public class chrBalloon : MonoBehaviour {

	public Texture texture_main    = null;
	public Texture texture_belo    = null;
	public Texture texture_kado_lu = null;
	public Texture texture_kado_ru = null;
	public Texture texture_kado_ld = null;
	public Texture texture_kado_rd = null;

	public	Vector2		position;			// 位置.
	public	string		text  = "";			// テキスト.
	public	Color		color = Color.red;	// ふきだしのいろ.

	protected float		timer = 0.0f;
	protected float		lifetime;

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
		GameRoot	game_root = GameRoot.getInstance();

		this.texture_main    = game_root.texture_main;
		this.texture_belo    = game_root.texture_belo;
		this.texture_kado_lu = game_root.texture_kado_lu;
		this.texture_kado_ru = game_root.texture_kado_ru;
		this.texture_kado_ld = game_root.texture_kado_ld;
		this.texture_kado_rd = game_root.texture_kado_rd;
	}
	
	void	Update()
	{
		if(this.text != "") {

			this.timer += Time.deltaTime;

			if(this.lifetime > 0.0f && this.timer >= this.lifetime) {

				this.text = "";
			}

		} else {

			this.timer = 0.0f;
		}
	}

	public void		setText(string text, float lifetime = -1.0f)
	{
		this.text     = text;
		this.lifetime = lifetime;
	}
	
	// ふきだしの色をセットする.
	public void		setColor(Color color)
	{
		this.color = color;
	}

	// ================================================================ //

	protected static float	KADO_SIZE = 16.0f;

	void	OnGUI()
	{

		if(this.text != "") {

			Vector2		pos;
	
			pos = this.position;

			// ゆらゆら.

			float	cycle = 4.0f;
			float	t = Mathf.Repeat(this.timer, cycle)/cycle;

			pos.x += 4.0f*Mathf.Sin(t*Mathf.PI*4.0f);
			pos.y += (4.0f*Mathf.Sin(t*Mathf.PI)*4.0f*Mathf.Sin(t*Mathf.PI)) - 100.0f;

			//

			float		font_size   = 13.0f;
			float		font_height = 20.0f;
			Vector2		balloon_size, text_size;

			text_size.x = this.text.Length*font_size;
			text_size.y = font_height;

			balloon_size.x = text_size.x + KADO_SIZE*2.0f;
			balloon_size.y = text_size.y + KADO_SIZE;

			this.disp_balloon(pos, balloon_size, this.color);
	
			Vector2		p;
	
			p.x = pos.x - text_size.x/2.0f;
			p.y = pos.y - text_size.y/2.0f;

			GUI.Label(new Rect(p.x, p.y, text_size.x, text_size.y), this.text);
		}
	}

	// 吹き出しを表示する.
	protected void	disp_balloon(Vector2 position, Vector2 size, Color color)
	{
		GUI.color = color;

		float		kado_size = KADO_SIZE;


		Vector2		p, s;

		s.x = size.x - kado_size*2.0f;
		s.y = size.y;

		// 真ん中.
		p = position - s/2.0f;
		GUI.DrawTexture(new Rect(p.x, p.y, s.x, s.y), this.texture_main);

		// 左.
		p.x = position.x - s.x/2.0f - kado_size;
		p.y = position.y - s.y/2.0f + kado_size;
		GUI.DrawTexture(new Rect(p.x, p.y, kado_size, size.y - kado_size*2.0f), this.texture_main);

		// 右.
		p.x = position.x + s.x/2.0f;
		p.y = position.y - s.y/2.0f + kado_size;
		GUI.DrawTexture(new Rect(p.x, p.y, kado_size, size.y - kado_size*2.0f), this.texture_main);

		// 左上.
		p.x = position.x - s.x/2.0f - kado_size;
		p.y = position.y - s.y/2.0f;
		GUI.DrawTexture(new Rect(p.x, p.y, kado_size, kado_size), this.texture_kado_lu);

		// 右上.
		p.x = position.x + s.x/2.0f;
		p.y = position.y - s.y/2.0f;
		GUI.DrawTexture(new Rect(p.x, p.y, kado_size, kado_size), this.texture_kado_ru);

		// 左下.
		p.x = position.x - s.x/2.0f - kado_size;
		p.y = position.y + s.y/2.0f - kado_size;
		GUI.DrawTexture(new Rect(p.x, p.y, kado_size, kado_size), this.texture_kado_ld);

		// 右下.
		p.x = position.x + s.x/2.0f;
		p.y = position.y + s.y/2.0f - kado_size;
		GUI.DrawTexture(new Rect(p.x, p.y, kado_size, kado_size), this.texture_kado_rd);

		// べろ.
		p.x = position.x - kado_size/2.0f;
		p.y = position.y + s.y/2.0f;
		GUI.DrawTexture(new Rect(p.x, p.y, kado_size, kado_size), this.texture_belo);

		GUI.color = Color.white;
	}


}
