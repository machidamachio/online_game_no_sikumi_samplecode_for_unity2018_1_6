using UnityEngine;
using System.Collections;

// ステータスウインドウ　ローカルプレイヤー用.
public class StatusWindowLocal : MonoBehaviour {

	public Texture		face_icon_texture;
	public Texture		lace_texture;
	public Texture[]	cookie_icon_textures;

	public Texture[]	number_textures;

	protected Sprite2DControl	face_sprite;
	protected Sprite2DControl	lace_sprite;
	protected Sprite2DControl	cookie_sprite;
	protected Sprite2DControl[]	digit_sprites;

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

	protected const float	SCALE = 0.8f;

	public void	create()
	{
		Vector2		root_position = new Vector2(640.0f/2.0f - 100.0f, 480.0f/2.0f - 100.0f);

		// 下に敷くレース.
		this.lace_sprite = Sprite2DRoot.get().createSprite(this.lace_texture, true);
		this.lace_sprite.setSize(new Vector2(128.0f, 128.0f)*SCALE);

		// クッキー（大まかなHP）.
		this.cookie_sprite = Sprite2DRoot.get().createSprite(this.cookie_icon_textures[0], true);
		this.cookie_sprite.setSize(new Vector2(80.0f, 80.0f)*SCALE);

		// 顔アイコン.
		this.face_sprite = Sprite2DRoot.get().createSprite(this.face_icon_texture, true);
		this.face_sprite.setSize(new Vector2(80.0f, 80.0f)*SCALE);

		// 数値（３桁分）.

		this.digit_sprites = new Sprite2DControl[3];

		for(int i = 0;i < 3;i++) {

			Sprite2DControl	digit = Sprite2DRoot.get().createSprite(this.number_textures[0], true);

			digit.setSize(new Vector2(48.0f, 48.0f)*SCALE);

			this.digit_sprites[i] = digit;
		}

		this.setPosition(root_position);
	}

	// 表示位置をセットする.
	public void	setPosition(Vector2 root_position)
	{
		this.lace_sprite.setPosition(root_position + new Vector2(0.0f, 0.0f));

		this.cookie_sprite.setPosition(root_position + new Vector2(0.0f, 0.0f));
		this.face_sprite.setPosition(root_position + new Vector2(35.0f, 35.0f)*SCALE);

		//

		Vector3	digit_position;
		Vector3	center = root_position;

		center.x += 70.0f*SCALE;
		center.y -= 30.0f*SCALE;

		center.y += 140.0f*SCALE;

		float	angle = -45.0f;

		for(int i = 0;i < this.digit_sprites.Length;i++) {

			Sprite2DControl	digit = this.digit_sprites[i];

			digit_position = center + Quaternion.AngleAxis(angle, Vector3.forward)*Vector3.down*140.0f*SCALE;

			digit.setPosition(digit_position);
			digit.setAngle(angle);

			if(i == 0) {

				angle += 10.0f;

			} else {

				angle += 15.0f;
			}
		}
	}

	// ヒットポイントをセットする.
	public void	setHP(float hp)
	{
		// 各桁の数値（０～９）を求める.

		int		as_int = (int)hp;
		int[]	digit = new int[3];

		for(int i = 2;i >= 0;i--) {

			digit[i] = as_int%10;

			as_int /= 10;
		}

		// テクスチャー（０～９）をセットしつつ、.
		// 表示する必要のない桁（99 以下のときの百の位など）を調べる.

		bool	disp_digit = false;

		for(int i = 0;i < 3;i++) {

			if(i == 2 || digit[i] > 0) {

				disp_digit = true;
			}

			this.digit_sprites[i].setVisible(disp_digit);
			this.digit_sprites[i].setTexture(this.number_textures[digit[i]]);
		}

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
