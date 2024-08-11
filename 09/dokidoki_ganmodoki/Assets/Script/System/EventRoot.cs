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
	public virtual void		end() {}
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

				this.current_event.end();
				this.current_event = null;
			}
		}

		// イベント開始.
		if(this.current_event == null) {

			if(this.next_event != null) {
	
				this.current_event = this.next_event;
				this.current_event.initialize();
				this.current_event.start();
	
				this.next_event = null;
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
	public EventBase	getCurrentEvent()
	{
		return(this.current_event);
	}
	public T	getCurrentEvent<T>() where T : EventBase
	{
		T	ev = null;

		if(this.current_event != null) {

			ev = this.current_event as T;
		}

		return(ev);
	}

#if false
	public T		getEventData<T>() where T : MonoBehaviour
	{
		T	event_data = this.gameObject.GetComponent<T>();

		return(event_data);
	}
#endif
#if false
	// イベントボックスを全部アクティブにする.
	public void		activateEventBoxAll()
	{

		 EventBoxLeave[]	boxes = this.getEventBoxes();

		foreach(var box in boxes) {

			box.activate();
		}

	}
#endif
#if false
	// イベントボックスを全部スリープにする.
	public void		deactivateEventBoxAll()
	{

		 EventBoxLeave[]	boxes = this.getEventBoxes();

		foreach(var box in boxes) {

			box.deactivate();
		}

	}
#endif

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

