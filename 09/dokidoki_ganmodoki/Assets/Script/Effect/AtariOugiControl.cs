using UnityEngine;
using System.Collections;

// アイスが当たったときのおうぎ.
public class AtariOugiControl : MonoBehaviour {

	public float	timer = 0.0f;

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Awake()
	{
	}

	void	Start()
	{
	}
	
	void	Update()
	{
		// UV スクロール.

		float	cycle = 1.0f;

		float	u_offset = ipCell.get().setInput(this.timer).repeat(cycle).quantize(0.25f).getCurrent();

		Renderer[]	renderers = this.gameObject.GetComponentsInChildren<Renderer>();

		foreach(var renderer in renderers) {

			renderer.material.SetTextureOffset("_MainTex", new Vector2(u_offset, 0.0f));
		}

		this.timer += Time.deltaTime;
	}
}
