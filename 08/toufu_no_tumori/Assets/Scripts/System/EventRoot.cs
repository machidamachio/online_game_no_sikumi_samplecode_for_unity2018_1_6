using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// イベントのベースクラス.
public class EventBase {

	protected GameObject	data_holder;

	// ================================================================ //

	public EventBase() {}

	public virtual void		initialize() {}
	public virtual void		start() {}
	public virtual void		execute() {}
	public virtual void		onGUI() {}

	// イベントが実行中？.
	public virtual bool		isInAction()
	{
		return(false);
	}

	protected Vector3	get_locator_position(string locator)
	{
		Vector3		pos = this.data_holder.transform.Find(locator).position;

		return(pos);
	}

};

// イベントの管理.
public class EventRoot : MonoBehaviour {

	protected EventBase		next_event    = null;		// 実行開始するイベント.
	protected EventBase		current_event = null;		// 実行中のイベント.

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Start()
	{
	}
	
	void	Update()
	{
		// イベント終了.
		if(this.current_event != null) {

			if(!this.current_event.isInAction()) {
	
				this.current_event = null;

				this.activateEventBoxAll();
			}
		}

		// イベント開始.
		if(this.current_event == null) {

			if(this.next_event != null) {
	
				this.current_event = this.next_event;
				this.current_event.initialize();
				this.current_event.start();
	
				this.next_event = null;
	
				this.deactivateEventBoxAll();
			}
		}

		// イベント実行.
		if(this.current_event != null) {

			this.current_event.execute();
		}
	}

	void	OnGUI()
	{
		if(this.current_event != null) {

			this.current_event.onGUI();
		}
	}

	// ================================================================ //

	// イベントを開始する.
	public T		startEvent<T>() where T : EventBase, new()
	{
		T	new_event = null;

		if(this.next_event == null) {

			new_event       = new T();
			this.next_event = new_event;
		}

		return(new_event);
	}

	// イベントボックスを全部アクティブにする.
	public void		activateEventBoxAll()
	{
		 EventBoxLeave[]	boxes = this.getEventBoxes();

		foreach(var box in boxes) {

			box.activate();
		}
	}

	// イベントボックスを全部スリープにする.
	public void		deactivateEventBoxAll()
	{
		 EventBoxLeave[]	boxes = this.getEventBoxes();

		foreach(var box in boxes) {

			box.deactivate();
		}
	}
	
	// イベントボックスを全部ゲットする.
	public EventBoxLeave[]	getEventBoxes()
	{
		GameObject[]	gos = GameObject.FindGameObjectsWithTag("EventBox");

		gos = System.Array.FindAll(gos, x => x.GetComponent<EventBoxLeave>() != null);

		EventBoxLeave[]	boxes= new EventBoxLeave[gos.Length];

		foreach(var i in System.Linq.Enumerable.Range(0, gos.Length)) {

			boxes[i] = gos[i].GetComponent<EventBoxLeave>();
		}

		return(boxes);
	}

	// ================================================================ //
	// インスタンス.

	private	static EventRoot	instance = null;

	public static EventRoot	get()
	{
		if(EventRoot.instance == null) {

			EventRoot.instance = GameObject.Find("EventRoot").GetComponent<EventRoot>();
		}

		return(EventRoot.instance);
	}
}

