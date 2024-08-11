using UnityEngine;
using System.Collections;
using MathExtension;

// 2D スプライト.
public class Sprite2DControl : MonoBehaviour {
	
	protected Vector2		size = Vector2.zero;

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
	}
	
	void	Update()
	{
	}

	// ================================================================ //

	// 位置をセットする.
	public void	setPosition(Vector2 position)
	{
		this.transform.localPosition = new Vector3(position.x, position.y, this.transform.localPosition.z);
	}

	// 位置をゲットする.
	public Vector2	getPosition()
	{
		return(this.transform.localPosition.xy());
	}

	// 奥行き値をセットする.
	public void	setDepth(float depth)
	{
		Vector3		position = this.transform.localPosition;

		position.z = depth;

		this.transform.localPosition = position;
	}

	// 奥行き値をゲットする.
	public float	getDepth()
	{
		return(this.transform.localPosition.z);
	}

	// [degree] アングル（Z軸周りの回転）をセットする.
	public void	setAngle(float angle)
	{
		this.transform.localRotation = Quaternion.AngleAxis(angle, Vector3.forward);
	}

	// スケールをセットする.
	public void	setScale(Vector2 scale)
	{
		this.transform.localScale = new Vector3(scale.x, scale.y, 1.0f);
	}
	
	// サイズをセットする.
	public void		setSize(Vector2 size)
	{
		Sprite2DRoot.get().setSizeToSprite(this, size);
	}
	// サイズをゲットする.
	public Vector2 getSize()
	{
		return(this.size);
	}
	
	// 頂点カラーをセットする.
	public void		setVertexColor(Color color)
	{
		Sprite2DRoot.get().setVertexColorToSprite(this, color);
	}

	// 頂点カラーのアルファーをセットする.
	public void		setVertexAlpha(float alpha)
	{
		Sprite2DRoot.get().setVertexColorToSprite(this, new Color(1.0f, 1.0f, 1.0f, alpha));
	}

	// テクスチャーをセットする.
	public void		setTexture(Texture texture)
	{
		this.GetComponent<MeshRenderer>().material.mainTexture = texture;
	}
	// テクスチャーをセットする（サイズも変更）.
	public void		setTextureWithSize(Texture texture)
	{
		this.GetComponent<MeshRenderer>().material.mainTexture = texture;
		Sprite2DRoot.get().setSizeToSprite(this, new Vector2(texture.width, texture.height));
	}

	// テクスチャーをゲットする.
	public Texture	getTexture()
	{
		return(this.GetComponent<MeshRenderer>().material.mainTexture);
	}

	// マテリアルをセットする.
	public void		setMaterial(Material material)
	{
		this.GetComponent<MeshRenderer>().material = material;
	}
	// マテリアルをゲットする.
	public Material		getMaterial()
	{
		return(this.GetComponent<MeshRenderer>().material);
	}

	// 左右/上下反転する.
	public void		setFlip(bool horizontal, bool vertical)
	{
		Vector2		scale  = Vector2.one;
		Vector2		offset = Vector2.zero;

		if(horizontal) {

			scale.x  = -1.0f;
			offset.x = 1.0f;
		}
		if(vertical) {

			scale.y = -1.0f;
			offset.y = 1.0f;
		}

		this.GetComponent<MeshRenderer>().material.mainTextureScale  = scale;
		this.GetComponent<MeshRenderer>().material.mainTextureOffset = offset;
	}

	// ポイントがスプライトの上にある？.
	public bool		isContainPoint(Vector2 point)
	{
		bool	ret = false;

		Vector2		position = this.transform.localPosition.xy();
		Vector2		scale    = this.transform.localScale.xy();

		do {

			if(point.x < position.x - this.size.x/2.0f*scale.x || position.x + this.size.x/2.0f*scale.x < point.x) {

				break;
			}
			if(point.y < position.y - this.size.y/2.0f*scale.y || position.y + this.size.y/2.0f*scale.y < point.y) {

				break;
			}

			ret = true;

		} while(false);

		return(ret);
	}

	// 表示/非表示をセットする.
	public void		setVisible(bool is_visible)
	{
		this.GetComponent<MeshRenderer>().enabled = is_visible;
	}
	// 表示中？.
	public bool		isVisible()
	{
		return(this.GetComponent<MeshRenderer>().enabled);
	}

	// 頂点の位置をゲットする.
	public Vector3[]	getVertexPositions()
	{
		return(Sprite2DRoot.get().getVertexPositionsFromSprite(this));
	}

	// 頂点の位置をセットする(2D).
	public void		setVertexPositions(Vector2[] positions)
	{
		Vector3[]		positions_3d = new Vector3[positions.Length];

		for(int i = 0;i < positions.Length;i++) {

			positions_3d[i] = positions[i];
		}
		Sprite2DRoot.get().setVertexPositionsToSprite(this, positions_3d);
	}

	// 頂点の位置をセットする(3D).
	public void		setVertexPositions(Vector3[] positions)
	{
		Sprite2DRoot.get().setVertexPositionsToSprite(this, positions);
	}

	// メッシュの頂点数をゲットする.
	public int		getDivCount()
	{
		return(this.div_count);
	}

	// 破棄する.
	public void		destroy()
	{
		GameObject.Destroy(this.gameObject);
	}

	public void		setParent(Sprite2DControl parent)
	{
		this.transform.parent = parent.transform;
	}

	// ================================================================ //
	// Sprite2DRoot よう.

	// サイズをセットする.
	public void	internalSetSize(Vector2 size)
	{
		this.size = size;
	}

	protected int	div_count = 2;

	public void	internalSetDivCount(int div_count)
	{
		this.div_count = div_count;
	}

}
