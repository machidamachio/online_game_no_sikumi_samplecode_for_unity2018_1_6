using UnityEngine;
using System.Collections;

// ステータスウインドウ　リモート用.
public class StatusWindowNet : MonoBehaviour {

	public Texture		face_icon_texture;
	public Texture		lace_texture;
	public Texture[]	cookie_icon_textures;

	protected Sprite2DControl	face_sprite;
	protected Sprite2DControl	lace_sprite;
	protected Sprite2DControl	cookie_sprite;

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Awake()
	{
	}

	void	Start()
	{
	}
	
	void 	Update()
	{
	
	}

	// ================================================================ //

	protected float	SCALE = 0.8f;

	public void	create()
	{
		Vector2		root_position = new Vector2(640.0f/2.0f - 100.0f, 0.0f);

		// 下にしくレース.
		this.lace_sprite = Sprite2DRoot.get().createSprite(this.lace_texture, true);
		this.lace_sprite.setSize(new Vector2(96.0f, 96.0f)*SCALE);

		// クッキー.
		this.cookie_sprite = Sprite2DRoot.get().createSprite(this.cookie_icon_textures[0], true);
		this.cookie_sprite.setSize(new Vector2(60.0f, 60.0f)*SCALE);

		// 顔アイコン.
		this.face_sprite = Sprite2DRoot.get().createSprite(this.face_icon_texture, true);
		this.face_sprite.setSize(new Vector2(60.0f, 60.0f)*SCALE);

		this.setPosition(root_position);
	}

	// 表示位置をセットする.
	public void	setPosition(Vector2 root_position)
	{
		this.lace_sprite.setPosition(root_position   + new Vector2(0.0f, 0.0f));
		this.face_sprite.setPosition(root_position   + new Vector2(20.0f, 20.0f)*SCALE);
		this.cookie_sprite.setPosition(root_position + new Vector2(0.0f, 0.0f));
	}

	// ヒットポイントをセットする.
	public void	setHP(float hp)
	{
		// クッキー.

		int		cookie_sprite_sel = 0;
		bool	is_visible = true;

		if(hp > 80.0f) {

			cookie_sprite_sel = 0;

		} else if(hp > 50.0f) {

			cookie_sprite_sel = 1;

		} else if(hp > 20.0f) {

			cookie_sprite_sel = 2;

		} else if(hp > 0.0f) {

			cookie_sprite_sel = 3;

		} else {

			is_visible = false;
		}

		this.cookie_sprite.setTexture(this.cookie_icon_textures[cookie_sprite_sel]);
		this.cookie_sprite.setVisible(is_visible);
	}
}
