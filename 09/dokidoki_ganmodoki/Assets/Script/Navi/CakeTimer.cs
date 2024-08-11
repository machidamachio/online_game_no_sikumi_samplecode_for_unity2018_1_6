using UnityEngine;
using System.Collections;

public class CakeTimer : MonoBehaviour {

	public Texture[]	cookie_icon_textures;		// フェースのクッキー.
	public Texture		hari_texture;				// 針.
	public Texture		hari_shadow_texture;		// 針の影.
	public Texture		transparent_texture;		// 透明テクスチャー.

	public Material		cookie_material;

	protected Sprite2DControl	root_sprite;
	protected Sprite2DControl	hari_sprite;
	protected Sprite2DControl	cookie_sprite;

	protected float		time = 0.0f;
	protected int		cookie_index = 0;

	protected float		local_timer = 0.0f;			// [sec] 針回転用タイマー.

	protected float		x_ofst = -0.01f;
	protected float		y_ofst = -0.4f;

	protected Vector2	hari_position = new Vector2(0.0f + 2.0f, 32.0f - 4.0f)*0.75f*1.0f;
	protected Vector2	hari_size     = new Vector2(32.0f, 64.0f)*0.75f*1.0f;
	protected Vector2	cookie_size   = new Vector2(128.0f, 128.0f)*0.75f*1.0f;

	public bool		is_timer_stopped = false;

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Awake()
	{
		this.root_sprite = Sprite2DRoot.get().createNull();

		this.cookie_sprite = Sprite2DRoot.get().createSprite(this.cookie_icon_textures[0], true);
		this.cookie_sprite.setMaterial(this.cookie_material);
		this.cookie_sprite.setSize(this.cookie_size);
		this.cookie_sprite.setParent(this.root_sprite);
		this.cookie_material.SetTexture("_MainTexture", this.cookie_icon_textures[0]);
		this.cookie_material.SetTexture("_HariTexture", this.hari_shadow_texture);

		this.hari_sprite = Sprite2DRoot.get().createSprite(this.hari_texture, true);
		this.hari_sprite.setSize(this.hari_size);
		this.hari_sprite.setParent(this.root_sprite);
		this.hari_sprite.setPosition(this.hari_position);

		this.root_sprite.setPosition(new Vector2(0.0f, 160.0f));
	}

	void	Start()
	{
	}

	void 	Update()
	{
		float	needle_cycle = 2.3f;

		float	angle = Mathf.Repeat(this.local_timer, needle_cycle)/needle_cycle;

		angle *= -360.0f;

		Vector3	v = Quaternion.AngleAxis(angle, Vector3.forward)*this.hari_position;

		this.hari_sprite.setAngle(angle);
		this.hari_sprite.setPosition(v);

		//

		this.cookie_index = Mathf.FloorToInt(this.time*this.cookie_icon_textures.Length);

		Texture		texture = this.transparent_texture;

		if(this.cookie_index < this.cookie_icon_textures.Length) {

			texture = this.cookie_icon_textures[this.cookie_index];
		}

		this.cookie_material.SetTexture("_MainTexture", texture);

		Matrix4x4	matrix = Matrix4x4.identity;

		matrix *= Matrix4x4.TRS(new Vector3( x_ofst, y_ofst, 0.0f), Quaternion.identity, Vector3.one);
		matrix *= Matrix4x4.TRS(new Vector3( 0.5f, 0.5f, 0.0f), Quaternion.identity, Vector3.one);
		matrix *= Matrix4x4.TRS(new Vector3( 0.0f, 0.0f, 0.0f), Quaternion.identity, new Vector3(cookie_size.x/hari_size.x, cookie_size.y/hari_size.y, 1.0f));
		matrix *= Matrix4x4.TRS(new Vector3( 0.0f, 0.0f, 0.0f), Quaternion.AngleAxis(-angle, Vector3.forward), Vector3.one);
		matrix *= Matrix4x4.TRS(new Vector3(-0.5f,-0.5f, 0.0f), Quaternion.identity, Vector3.one);
		matrix *= Matrix4x4.TRS(new Vector3(0.05f, 0.05f, 0.0f), Quaternion.identity, Vector3.one);

		this.cookie_material.SetMatrix("_HariTextureMatrix", matrix);

		//

		float	timer_prev = this.local_timer;

		if(!this.is_timer_stopped) {

			this.local_timer += Time.deltaTime;
		}

		// タイマーが 1.0 になったあと、最初に 12 時を通過するタイミングで.
		// 針の回転を止める.
		do {

			if(this.is_timer_stopped) {

				break;
			}

			if(this.time < 1.0f) {

				break;
			}

			if(Mathf.FloorToInt(timer_prev/needle_cycle) == Mathf.FloorToInt(this.local_timer/needle_cycle)) {

				break;
			}

			this.is_timer_stopped = true;

			this.local_timer = 0.0f;

		} while(false);
	}

	// ================================================================ //

	// 時刻をセットする(0.0 ～ 1.0)
	public void		setTime(float time)
	{
		this.time = time;
	}

	// 破棄する.
	public void		destroy()
	{
		this.hari_sprite.destroy();
		this.cookie_sprite.destroy();
		this.root_sprite.destroy();

		GameObject.Destroy(this.gameObject);
	}

}
