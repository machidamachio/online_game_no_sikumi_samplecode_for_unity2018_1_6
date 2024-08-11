using UnityEngine;
using System.Collections;

// 非常にシンプルなムーブメントコントロール.
public class FootSmokeEffectControl : MonoBehaviour {
	public Vector3 velocity;

	// Update is called once per frame
	void Update () {
		transform.position += velocity * Time.deltaTime;
	}
}
