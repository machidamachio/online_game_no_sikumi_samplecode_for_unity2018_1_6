using UnityEngine;
using System.Collections;

public class EventDataIceAtari : MonoBehaviour {

	public Texture		texture_bikkuri = null;			// "！" マークのふきだし.
	public Texture		texture_ice     = null;
	public Texture		texture_ice_bar = null;
	public Texture		texture_atari   = null;			// 「当たり！」のおうぎのテクスチャー.

	public GameObject	prefab_ice_atari     = null;
	public GameObject	prefab_ice_atari_bar = null;	// 「当たり！」のアイスの棒.

	public Material		material_ice_sprite = null;

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
	}

	void	Update()
	{
	}
}
