using UnityEngine;
using System.Collections;


public struct MouseData
{
	public int		frame;
	public bool		mouseButtonLeft;
	public bool		mouseButtonRight;
	
	public float 	mousePositionX;
	public float 	mousePositionY;
	public float 	mousePositionZ;

    public override string ToString() {
        string str = "";
        str += "frame:" + frame;
        str += " mouseButtonLeft:" + mouseButtonLeft;
        str += " mouseButtonRight:" + mouseButtonRight;
        str += " mousePositionX:" + mousePositionX;
        str += " mousePositionY:" + mousePositionY;
	    str += " mousePositionZ:" + mousePositionZ;
        return str;
    }

};

public struct InputData
{   
	public int 			count;		// データ数.
	public int			flag;		// 各種フラグ.
	public MouseData[] 	datum;		// キー入力情報.
};


public class InputManager : MonoBehaviour {

    MouseData[] m_syncedInputs = new MouseData[2]; //同期済みの入力値.
    MouseData m_localInput; //現在の入力値(これを送信させる).
    

    // Update is called once per frame
    void FixedUpdate() {
        //Debug.Log(gameObject.name + Time.frameCount.ToString() + " scale:" + Time.timeScale.ToString());

        m_localInput.mouseButtonLeft = Input.GetMouseButton(0);
        m_localInput.mouseButtonRight = Input.GetMouseButton(1);


        //マウス座標計算.
        //そのまま入れるとウィンドウサイズ違いで困るので変換してます.
        Vector3 pos = Input.mousePosition;
        Ray ray = Camera.main.ScreenPointToRay(pos);

        Plane plane = new Plane(Vector3.up, Vector3.zero);
        float depth;
        plane.Raycast(ray, out depth);

        Vector3 worldPos = ray.origin + ray.direction * depth;

        m_localInput.mousePositionX = worldPos.x;
        m_localInput.mousePositionY = worldPos.y;
        m_localInput.mousePositionZ = worldPos.z;
    }

    //現在の入力値を返す.
    public MouseData GetLocalMouseData() {
        return m_localInput;
    }

    //同期済みの入力値を返す.
    public MouseData GetMouseData(int id) {
        //		Debug.Log("id:" + id + "' " + inputData.Length);
        return m_syncedInputs[id];
    }

    //同期済みの入力値のセット用.
    public void SetInputData(int id, MouseData data) {
        m_syncedInputs[id] = data;
    }
}

