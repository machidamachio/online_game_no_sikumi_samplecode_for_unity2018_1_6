﻿using UnityEngine;
using System.Collections;

/** くるくる回って倒れる */
public class LightDamage : MonoBehaviour {
    // Use this for initialization
    void Start() {
        gameObject.AddComponent<Rigidbody>();

        GetComponent<Rigidbody>().AddTorque(Vector3.up * -10);
        GetComponent<Rigidbody>().AddForce(Vector3.right);

        //gameObject.GetComponent<Player>().ChangeAnimation(Player.Motion.Damage);
    }

    // Update is called once per frame
    void Update() {
    }

}
