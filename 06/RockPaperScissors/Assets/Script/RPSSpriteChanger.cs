using UnityEngine;
using System.Collections;

/** グーチョキパーの表示切替をしたいときに使う */
public class RPSSpriteChanger : MonoBehaviour {
    public Sprite m_rockSprite;
    public Sprite m_paperSprite;
    public Sprite m_scissorSprite;


	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
    
    public void SetSprite(RPSKind rps) {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();

        switch (rps) {
        case RPSKind.None:
            renderer.sprite = null;
            break;
        case RPSKind.Rock:
            renderer.sprite = m_rockSprite;
            break;
        case RPSKind.Paper:
            renderer.sprite = m_paperSprite;
            break;
        case RPSKind.Scissor:
            renderer.sprite = m_scissorSprite;
            break;
        }
    }

}
