using UnityEngine;
using System.Collections;
using System.Collections.Generic;



namespace SimpleSpline {

	// 制御点.
	public class ControlVertex {
	
		public int			index;
		public Vector3		position;
		public Vector3		tangent;
		public float		tension;
	};

	// カーブをなぞる.
	public class Tracer {

		public Curve			curve = null;

		public ControlVertex	cv = new ControlVertex();
		public bool				is_ended = false;
		public float			t = -1.0f;
		public float			distance = 0.0f;

		public void		attach(Curve curve)
		{
			this.curve = curve;
			this.restart();
		}
		public void		restart()
		{
			this.is_ended = false;
			this.t        = -1.0f;
			this.distance = 0.0f;
			this.cv       = new ControlVertex();
		}


		public ControlVertex	getCurrent()
		{
			return(this.cv);
		}

		// 現在位置を指定する（スタート地点からの道のりで）.
		public void		setCurrentByDistance(float distance)
		{
			this.proceedToDistance(distance);
			this.is_ended = false;
		}

		// 現在位置から進む（パラメーター指定）.
		public void		proceed(float dt)
		{
			if(!this.is_ended) {

				if(this.t < 0.0f) {

					this.t = 0.0f;

				} else {

					this.t += dt;
				}

				if(this.t >= this.curve.getEnd()) {

					this.t = this.curve.getEnd();
					this.is_ended = true;
				}

				this.cv = this.curve.calcVertex(this.t);
			}
		}

		// 現在位置から進む（スタート地点からの道のり）.
		public void		proceedToDistance(float dist)
		{
			if(this.distance < 0.0f) {

				this.distance = 0.0f;
				this.t        = 0.0f;
			}

			this.proceedByDistance(dist - this.distance);

			this.distance = dist;
		}

		// 現在位置から進む（現在位置からの道のり）.
		public void		proceedByDistance(float dist)
		{
			do {

				if(this.is_ended) {

					break;
				}

				ControlVertex	cv0;

				if(this.t < 0.0f) {

					this.t = 0.0f;
				}

				cv0 = this.curve.calcVertex(this.t);
				this.cv = cv0;

				//

				float		dt  = dist/this.curve.calcTotalDistance()*this.curve.getEnd();
				float		sdt = dt >= 0.0f ? 1.0f : -1.0f;
							dt  = Mathf.Abs(dt);
				float		dt0 = dt;
				float		t0  = this.t;

				float		advanced  = 0.0f;
				float		advanced0 = advanced;
				int			i;

				// 差大繰り返し回数.
				int			max_times = Mathf.CeilToInt(this.curve.getEnd()/dt);

				max_times = max_times >= 1 ? max_times : 1;

				for(i = 0;i < max_times;i++) {

					t0 = this.t;
					this.t += sdt*dt;

					if(this.t >= this.curve.getEnd() && sdt >= 0.0f) {
	
						this.t = this.curve.getEnd();

						if(dt < dt0) {

							this.is_ended = true;
						}

						dt = Mathf.Max(0.0f, this.t - t0);

					} else if(this.t < 0.0f && sdt < 0.0f) {

						this.t = 0.0f;

						if(dt < dt0) {

							this.is_ended = true;
						}

						dt = Mathf.Max(0.0f, Mathf.Abs(this.t - t0));
					}

					this.cv = this.curve.calcVertex(this.t);

					advanced0 = advanced;
					advanced += Vector3.Distance(cv0.position, this.cv.position);

					if(this.is_ended) {

						break;
					}

					if(advanced >= Mathf.Abs(dist)) {

						// 行き過ぎた場合.
						// 進む距離を半分にして、直前の場所からやり直し.

						dt *= 0.5f;

						if(dt < dt0/100.0f) {

							break;
						}

						this.t = t0;
						advanced = advanced0;

					} else {

						cv0 = this.cv;
					}
				}

			} while(false);
		}

		public bool		isEnded()
		{
			return(this.is_ended);
		}
	};

	// カーブ.
	public class Curve {
	
		public List<ControlVertex>	cvs = new List<ControlVertex>();

		public void		appendCV(Vector3 position, Vector3 slope)
		{
			ControlVertex	cv = new ControlVertex();

			cv.index    = this.cvs.Count;
			cv.position = position;
			cv.tension  = slope.magnitude;
			cv.tangent  = slope.normalized;

			this.cvs.Add(cv);
		}

		public float	getEnd()
		{
			float	end = (float)(this.cvs.Count - 1);

			return(end);
		}
	
		// カーブ上の点を求める.
		public ControlVertex	calcVertex(float t)
		{
			ControlVertex	cv = null;
	
			int		segment_index = Mathf.FloorToInt(t);
	
			if(segment_index < 0) {
	
				cv = this.cvs[0];
	
			} else if(this.cvs.Count - 1 <= segment_index) {
	
				cv = this.cvs[this.cvs.Count - 1];
	
			} else {
	
				float[]		pos_k = new float[4];
				float[]		tan_k  = new float[4];
		
				ControlVertex	cv0 = this.cvs[segment_index];
				ControlVertex	cv1 = this.cvs[segment_index + 1];
		
				float		local_param = t - (float)segment_index;
		
				Curve.calc_konst(pos_k, tan_k, local_param);
		
				cv = Curve.lerp(cv0, cv1, pos_k, tan_k);
			}
	
			return(cv);
		}

		// カーブの総道のりを計算する.
		public float	calcTotalDistance()
		{
			float		distance = 0.0f;
	
			ControlVertex	cv_prev = this.calcVertex(0.0f);
	
			float	t     = 0.0f;
			float	dt    = 0.1f;
			float	t_max = (float)(this.cvs.Count - 1);
	
			int		safe_count = Mathf.CeilToInt(t_max/dt) + 1;
	
			for(int i = 0;i < safe_count;i++) {
	
				ControlVertex	cv_current = this.calcVertex(t);
	
				distance += Vector3.Distance(cv_prev.position, cv_current.position);
	
				if(t >= t_max) {
	
					break;
				}
	
				t += dt;
	
				t = Mathf.Min(t, t_max);

				cv_prev = cv_current;
			}
	
			return(distance);
		}
	
		// ================================================================ //
	
		// 区間を補間する.
		public  static ControlVertex	lerp(ControlVertex cv0, ControlVertex cv1, float[] pos_k, float[] tan_k)
		{
			ControlVertex	dest = new ControlVertex();
	
			dest.position = cv0.position*pos_k[0] + cv1.position*pos_k[1] + cv0.tangent*cv0.tension*pos_k[2] + cv1.tangent*cv1.tension*pos_k[3];
			dest.tangent  = cv0.position*tan_k[0] + cv1.position*tan_k[1] + cv0.tangent*cv0.tension*tan_k[2] + cv1.tangent*cv1.tension*tan_k[3];
	
			return(dest);
		}
	
		// スプラインの係数を求める.
		public static void calc_konst(float[] dest_pos_k, float[] dest_tan_k, float t)
		{
			dest_pos_k[0] =  2.0f*t*t*t - 3.0f*t*t     + 1.0f;
			dest_pos_k[1] = -2.0f*t*t*t + 3.0f*t*t;
			dest_pos_k[2] =       t*t*t - 2.0f*t*t + t;
			dest_pos_k[3] =       t*t*t -      t*t;
		
			dest_tan_k[0] =  6.0f*t*t - 6.0f*t;
			dest_tan_k[1] = -6.0f*t*t + 6.0f*t;
			dest_tan_k[2] =  3.0f*t*t - 4.0f*t + 1.0f;
			dest_tan_k[3] =  3.0f*t*t - 2.0f*t;
		}
	};

}; // namespace SimpleSpline 
