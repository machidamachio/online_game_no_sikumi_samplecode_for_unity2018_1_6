using UnityEngine;
using System.Collections;

// 実行ステップ管理.
public class Step<T> where T : struct {

	public Step(T none)
	{
		this.none = none;
		init();
	}

	public void	init()
	{
		this.previous = this.none;
		this.current  = this.none;
		this.next     = this.none;

		this.previous_time = -1.0f;
		this.time = 0.0f;
		this.count = 0;

		this.status.is_changed = false;

		this.delay.delay = -1.0f;
		this.delay.next  = this.none;
	}

	public void	release()
	{
		this.init();
	}

	// 次のステップをセットする.
	public void	set_next(T step)
	{
		this.next = step;
	}
	// 次のステップをゲットする.
	public T	get_next()
	{
		return(this.next);
	}

	// delay[sec] 待ってから次のステップに遷移する.
	public void	set_next_delay(T step, float delay)
	{
		this.next = this.none;

		this.delay.delay = delay;
		this.delay.next  = step;
	}

	// 現在のステップをゲットする.
	public T	get_current()
	{
		return(this.current);
	}
	// 前のステップをゲットする.
	public T	get_previous()
	{
		return(this.previous);
	}

	// ステップが遷移した瞬間？.
	public bool	is_changed()
	{
		return(this.status.is_changed);
	}

	// [sec] ステップ内の経過時間をゲットする.
	public float	get_time()
	{
		return(this.time);
	}

	// [sec] 前回実行時の経過時間をゲットする.
	public float	get_previous_time()
	{
		return(this.previous_time);
	}

	// 遷移判定.
	public T	do_transition()
	{
#if true
		return(this.do_transition_internal());
#else
		T	step;

		step = this.current;

		return(step);
#endif
	}

	// 遷移判定（内部遷移のみ）.
	public T	do_transition_internal()
	{
		T	step;

		if(!this.delay.next.Equals(this.none)) {

			step = this.none;

			if(this.delay.delay <= 0.0f) {

				this.next = this.delay.next;
				this.delay.delay = -1.0f;
				this.delay.next  = this.none;
			}

		} else {

			if(this.next.Equals(this.none)) {
	
				step = this.current;
	
			} else {
	
				// 遷移することが決まっている（外部からのリクエスト）.
				// 場合はやらない.
	
				step = this.none;
			}
		}

		return(step);
	}

	// 開始.
	public T		do_initialize()
	{
		T	step;

		if(!this.next.Equals(this.none)) {

			step = this.next;

			this.previous = this.current;
			this.current  = this.next;
			this.next     = this.none;
			this.time     = -1.0f;
			this.count    = 0;

			this.status.is_changed = true;

		} else {

			// 開始するものがない（遷移が起きなかった）.
			//
			step = this.none;

			this.status.is_changed = false;
		}

		return(step);
	}

	// 実行.
	public T		do_execution(float passage_time)
	{
		T	step;

		if(this.delay.delay >= 0.0f) {

			this.delay.delay -= passage_time;

			step = this.none;

		} else {

			if(!this.current.Equals(this.none)) {
	
				step = this.current;
	
			} else {
	
				step = this.none;
			}
	
			this.count++;
	
			this.previous_time = this.time;
	
			if(this.time < 0.0f) {
	
				this.time = 0.0f;
	
			} else {
	
				this.time += passage_time;
			}
		}

		return(step);
	}

	// 一時停止（ステップなし）したいときに使う.
	public void		sleep()
	{
		this.current = this.none;
	}

	// 時刻 time をまたいだ瞬間？.
	public bool		is_acrossing_time(float time)
	{
		bool	ret = (this.previous_time < time && time <= this.time);

		return(ret);
	}

	public bool		is_acrossing_cycle(float cycle)
	{
		bool	ret = (Mathf.Ceil(this.previous_time/cycle) < Mathf.Ceil(this.time/cycle));

		return(ret);
	}

	// ---------------------------------------------------------------- //

	protected	T		previous;
	protected	T		current;
	protected	T		next;

	protected	T		none;

	protected 	float		time;					// STEP が変わってからの経過時間.
	protected 	float		previous_time;			// 前回 do_execution() したときの time.
	protected 	int			count;

	protected struct Status {

		public	bool		is_changed;
	};
	protected Status	status;

	protected struct Delay {

		public float		delay;
		public T			next;
	};
	protected Delay	delay;
};

// 使い方.
#if false

		// ---------------------------------------------------------------- //
		// 次の状態に移るかどうかを、チェックする.

		switch(this.step.do_transition()) {

			case STEP.IDLE:
			{
			}
			break;
		}

		// ---------------------------------------------------------------- //
		// 状態が遷移したときの初期化.

		while(this.step.get_next() != STEP.NONE) {

			switch(this.step.do_initialize()) {
	
				case STEP.STAND:
				{
				}
				break;
			}
		}

		// ---------------------------------------------------------------- //
		// 各状態での実行処理.

		switch(this.step.do_execution(Time.deltaTime)) {

			case STEP.STAND:
			{
			}
			break;
		}

#endif

