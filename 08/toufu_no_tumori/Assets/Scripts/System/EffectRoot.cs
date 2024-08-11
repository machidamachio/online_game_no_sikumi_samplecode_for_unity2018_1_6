using UnityEngine;
using System.Collections;

public class EffectRoot : MonoBehaviour {

	public GameObject	smoke01Prefab = null;
	public GameObject	smoke02Prefab = null;
	public GameObject	ripplePrefab  = null;

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
	}
	
	void	Update()
	{
	}

	// ================================================================ //

	// アイテムが実に成長するときの煙エフェクトをつくる.
	public GameObject	createSmoke01(Vector3 position)
	{
		GameObject	effect = GameObject.Instantiate(this.smoke01Prefab) as GameObject;

		// エフェクト終了時にゲームオブジェクトを消す.
		effect.AddComponent<EffectSelfRelease>();
		effect.transform.position = position;

		return(effect);
	}

	// 犬がダッシュするとき等の煙エフェクトをつくる.
	public GameObject	createSmoke02(Vector3 position)
	{
		GameObject	effect = GameObject.Instantiate(this.smoke02Prefab) as GameObject;

		effect.AddComponent<EffectSelfRelease>();
		effect.transform.position = position;

		return(effect);
	}

	// 犬がダッシュするとき等の煙エフェクトをつくる.
	public GameObject	createRipple(Vector3 position)
	{
		GameObject	effect = GameObject.Instantiate(this.ripplePrefab) as GameObject;

		effect.AddComponent<EffectSelfRelease>();
		effect.transform.position = position;

		return(effect);
	}

	// ================================================================ //
	// インスタンス.

	private	static EffectRoot	instance = null;

	public static EffectRoot	getInstance()
	{
		if(EffectRoot.instance == null) {

			EffectRoot.instance = GameObject.Find("Effect Root").GetComponent<EffectRoot>();
		}

		return(EffectRoot.instance);
	}

	public static EffectRoot	get()
	{
		return(EffectRoot.getInstance());
	}
}
