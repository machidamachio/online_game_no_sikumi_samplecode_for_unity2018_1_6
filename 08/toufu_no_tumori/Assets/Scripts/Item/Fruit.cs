using UnityEngine;
using System.Collections;

public class Fruit : MonoBehaviour {

	public Kind		m_fruitKind;
	
	private State	m_state = State.None;
	
	private Owner	m_owner = Owner.None;
	
	
	public enum Kind
	{
		None = 0,		// 何もない.
		Apple,			// リンゴ.
		Cherry,			// さくらんぼ.
		Orange,			// みかん.
	};
	
	public enum State
	{
		None = 0,
		Growing,
		Idle,
		PickingUp,
		Picked,
		PuttingDown,
		PutDown,
	};
	
	
	public enum Owner
	{
		None = 0,
		Player1,
		Player2,
		AppleTree,
		CherryTree,
		OrangeTree,
	}

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
		// 育成中か所有されているときは非表示にします.
		
	}
	
	public Kind GetFruitKind()
	{
		return m_fruitKind;
	}
	
	public void SetState(State state)
	{
		m_state = state;	
	}
	
	public State GetState()
	{
		return m_state;
	}
	
	public void SetOwner(Owner owner)
	{
		m_owner = owner;	
	}
	
	public Owner GetOwner()
	{
		return m_owner;
	}
	
}
