using UnityEngine;
using System.Collections;

// エフェクト終了時にゲームオブジェクトを消す.
public class EffectSelfRelease : MonoBehaviour {

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
	}
	
	void	Update()
	{
		if(this.GetComponentInChildren<ParticleSystem>().isStopped) {

			GameObject.Destroy(this.gameObject);
		}
	}
}
