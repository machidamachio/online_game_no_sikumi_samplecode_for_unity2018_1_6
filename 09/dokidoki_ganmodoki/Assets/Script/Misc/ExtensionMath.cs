using UnityEngine;
using System.Collections;


public class MathUtility {

	// ２点感の距離（XZ成分のみ）を求める.
	public static float		calcDistanceXZ(Vector3 from, Vector3 to)
	{
		Vector3		v = to - from;

		v.y = 0.0f;

		return(v.magnitude);
	}
	// from から to に向かうベクトルの Y アングルを求める.
	public static float		calcDirection(Vector3 from, Vector3 to)
	{
		Vector3		v = to - from;

		float	dir  = Mathf.Atan2(v.x, v.z)*Mathf.Rad2Deg;

		dir = MathUtility.snormDegree(dir);

		return(dir);
	}

	// degree を -180.0f ～ 180.0f の範囲におさめる.
	public static float		snormDegree(float degree)
	{
		if(degree > 180.0f) {

			degree -= 360.0f;

		} else if(degree < -180.0f) {

			degree += 360.0f;
		}

		return(degree);
	}

	// degree を 0.0f ～ 360.0f の範囲におさめる.
	public static float		unormDegree(float degree)
	{
		if(degree > 360.0f) {

			degree -= 360.0f;

		} else if(degree < 0.0f) {

			degree += 360.0f;
		}

		return(degree);
	}

	public static float		remap(float a0, float a1, float x, float b0, float b1)
	{
		return(Mathf.Lerp(b0, b1, Mathf.InverseLerp(a0, a1, x)));
	}
}

// 拡張メソッド.
namespace MathExtension {

	static class Vector {

		public static Vector3	XZ(this Vector3 v, float x, float z)
		{
			return(new Vector3(x, v.y, z));
		}

		// Vector3.Y()
		// Y成分をセットする.
		public static Vector3	Y(this Vector3 v, float y)
		{
			return(new Vector3(v.x, y, v.z));
		}

		// Vector3.xy()
		// xy 成分から Vector2 をつくる.
		public static Vector2	xy(this Vector3 v)
		{
			return(new Vector2(v.x, v.y));
		}
		// Vector3.xz()
		// xz 成分から Vector2 をつくる.
		public static Vector2	xz(this Vector3 v)
		{
			return(new Vector2(v.x, v.z));
		}
	};
};
