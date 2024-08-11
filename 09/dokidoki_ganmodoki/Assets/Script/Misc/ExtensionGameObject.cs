using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 拡張メソッド.
namespace GameObjectExtension {

	static class _List {
		
		// 先頭の要素.
		public static T 		front<T>(this List<T> list)
		{
			return(list[0]);
		}

		// さいごの要素.
		public static T 		back<T>(this List<T> list)
		{
			return(list[list.Count - 1]);
		}
	}

	static class _MonoBehaviour {

		// ポジションをセットする.
		public static void 		setPosition(this MonoBehaviour mono, Vector3 position)
		{
			mono.gameObject.transform.position = position;
		}

		// 位置をゲットする.
		public static Vector3	getPosition(this MonoBehaviour mono)
		{
			return(mono.gameObject.transform.position);
		}

		// ローカルポジションをセットする.
		public static void setLocalPosition(this MonoBehaviour mono, Vector3 local_position)
		{
			mono.gameObject.transform.localPosition = local_position;
		}

		// ローカルポジションをセットする.
		public static void setLocalScale(this MonoBehaviour mono, Vector3 local_scale)
		{
			mono.gameObject.transform.localScale = local_scale;
		}

		// ================================================================ //

		// 親をセットする.
		public static void setParent(this MonoBehaviour mono, GameObject parent)
		{
			if(parent != null) {

				mono.gameObject.transform.parent = parent.transform;

			} else {

				mono.gameObject.transform.parent = null;
			}
		}

		// ================================================================ //
	};

	static class _GameObject {

		// プレハブからインスタンスを生成する.
		public static GameObject	instantiate(this GameObject prefab)
		{
			return(GameObject.Instantiate(prefab) as GameObject);
		}

		// 自分自身を破棄する.
		public static void		destroy(this GameObject go)
		{
			GameObject.Destroy(go);
		}

		// ================================================================ //

		// 表示/非表示をセットする.
		public static void	setVisible(this GameObject go, bool is_visible)
		{
			Renderer[]		renders = go.GetComponentsInChildren<Renderer>();
			
			foreach(var render in renders) {
			
				render.enabled = is_visible;
			}
		}

		// ================================================================ //

		// ポジションをセットする.
		public static void 		setPosition(this GameObject go, Vector3 position)
		{
			go.transform.position = position;
		}

		// 位置をゲットする.
		public static Vector3	getPosition(this GameObject go)
		{
			return(go.transform.position);
		}

		// ローテーションをセットする.
		public static void setRotation(this GameObject go, Quaternion rotation)
		{
			go.transform.rotation = rotation;
		}

		// ローカルポジションをセットする.
		public static void setLocalPosition(this GameObject go, Vector3 local_position)
		{
			go.transform.localPosition = local_position;
		}

		// ローカルポジションをセットする.
		public static void setLocalScale(this GameObject go, Vector3 local_scale)
		{
			go.transform.localScale = local_scale;
		}

		// ================================================================ //

		// 親をセットする.
		public static void setParent(this GameObject go, GameObject parent)
		{
			if(parent != null) {

				go.transform.parent = parent.transform;

			} else {

				go.transform.parent = null;
			}
		}

		// 子供のゲームオブジェクトを探す.
		public static GameObject findChildGameObject(this GameObject go, string child_name)
		{
			GameObject	child_go = null;
			Transform	child    = go.transform.Find(child_name);

			if(child != null) {

				child_go = child.gameObject;
			}

			return(child_go);
		}

		// 子供（と、それ以下のノード）のゲームオブジェクトを探す.
		public static GameObject	findDescendant(this GameObject go, string name)
		{
			GameObject	descendant = null;
	
			descendant = go.findChildGameObject(name);
	
			if(descendant == null) {
	
				foreach(Transform child in go.transform) {
	
					descendant = child.gameObject.findDescendant(name);
	
					if(descendant != null) {
	
						break;
					}
				}
			}
	
			return(descendant);
		}

		// ================================================================ //

		// マテリアルのプロパティを変更する（float）.
		public static void	setMaterialProperty(this GameObject go, string name, float value)
		{
			SkinnedMeshRenderer[]		renders = go.GetComponentsInChildren<SkinnedMeshRenderer>();
			
			foreach(var render in renders) {
			
				foreach(var material in render.materials) {

					material.SetFloat(name, value);
				}
			}
		}

		// マテリアルのプロパティを変更する（Color）.
		public static void	setMaterialProperty(this GameObject go, string name, Color color)
		{
			SkinnedMeshRenderer[]		renders = go.GetComponentsInChildren<SkinnedMeshRenderer>();
			
			foreach(var render in renders) {
			
				foreach(var material in render.materials) {

					material.SetColor(name, color);
				}
			}
		}

	}
};
