using UnityEngine;
using System.Collections;
using System;

public class Timer : MonoBehaviour {
    GameObject[] m_numbers;
    float m_timer;
    bool m_isStop;
    
	// Use this for initialization
	void Start () {
        //タイマー表示のために捕まえておく.
        m_numbers = new GameObject[5];
        for (int i = 0; i < m_numbers.Length; ++i){
            m_numbers[i] = GameObject.Find("Number" + i);
        }
        
        //小数点の表示.
        GameObject dot = GameObject.Find("Dot");
        AsciiCharacter ascii = dot.GetComponent<AsciiCharacter>();
        ascii.SetChar('.');

        //タイマー初期化.
        m_isStop = false;
        m_timer = Time.time;
        UpdateTimer();
        //SetNumber(3 * 1000);
	}
	
	// Update is called once per frame
	void Update () {
        if (m_isStop) {
            return; //停止状態なら何もしない.
        }

        //タイマー表示更新.
        UpdateTimer();
	}


    //タイマー表示更新.
    void UpdateTimer() {
        float time = 3.0f - (Time.time - m_timer);
        if (time < 0.0f) {
            time = 0.0f;
        }

        int count = (int)(time * 1000);
        SetNumber(count);
    }


    //5桁の番号を表示する.
    void SetNumber(int num) {
        foreach (GameObject obj in m_numbers) {
            AsciiCharacter ascii = obj.GetComponent<AsciiCharacter>();
            ascii.SetNumber( num % 10 );

            num /= 10;           
        }
    }

    //表示内容を元に経過時間を取得.
    public float GetNumber() {
        int num = 0;
        for (int i = 0; i < m_numbers.Length; ++i) {
            AsciiCharacter ascii = m_numbers[i].GetComponent<AsciiCharacter>();
            num += ascii.GetNumber() * (int)Math.Pow(10, i);
        }
        return 3.0f - (num / 1000.0f);
    }


    //残り時間が0以下ならtrue.
    public bool IsTimeZero(){
        float time = 3.0f - (Time.time - m_timer);
        return (time < 0.0f);
    }

    //タイマーを停止させる.
    public void Stop() {
        m_isStop = true;
        UpdateTimer();
    }

}
