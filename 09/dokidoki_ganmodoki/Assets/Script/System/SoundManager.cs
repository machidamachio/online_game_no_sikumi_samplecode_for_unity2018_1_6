using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public struct Sound {

	// サウンドアセットと同じ名前にしておいてください.
	public enum ID {

		NONE = -1,

		SYSTEM00 = 0,		// 汎用クリック音.
		TKJ_BGM01,			// 戦闘中のBGM(ジャンケンのものを流用).
		DDG_BGM01,			// ボス戦BGM.
		TKJ_JINGLE01,		// ゲーム開始時のジングル.
		DDG_JINGLE02,		// ゲームクリアジングル.
		DDG_JINGLE03,		// プレイヤー死亡ジングル.
		
		DDG_SE_ENEMY01,		// 敵やられSE.
		DDG_SE_ENEMY02,		// 敵死亡SE.
		DDG_SE_PLAYER01,	// プレイヤー　攻撃時の素振り.
		DDG_SE_PLAYER02,	// プレイヤー　攻撃　相手へのヒット.
		DDG_SE_PLAYER03,	// プレイヤーやられSE.
		DDG_SE_PLAYER04,	// 足音SE(とうふのつもり流用、いらないかも).
		DDG_SE_SYS01,		// アイテム発生SE.
		DDG_SE_SYS02,		// アイテムをとった時のSE.
		DDG_SE_SYS03,		// アイテム使用SE.
		DDG_SE_SYS04,		// 体力回復SE.
		DDG_SE_SYS05,		// ワープSE.
		DDG_SE_SYS06,		// アイス食べ過ぎ.

		DDG_JINGLE04,		// アイスのあたりを引いたジングル.
		
		NUM,
	};

	public enum SLOT {

		NONE = -1,

		BGM = 0,
		SE0,
		SE_GET_ITEM,
		SE_WALK,

		NUM,
	};
};

public class SoundManager : MonoBehaviour {

	public class Slot {

		public AudioSource		source = null;
		public float			timer  = 0.0f;
		public bool				single_shot = false;
	};

	public List<AudioClip>		clips;
	public List<Slot>			slots;

	public bool		is_play_sound = true;

	// ================================================================ //
	// MonoBehaviour からの継承.

	void	Awake()
	{
		// enum と同じ順番になるよう、clip を並び替える.

		var		temp_clips = new List<AudioClip>();

		for(int i = 0;i < (int)Sound.ID.NUM;i++) {

			Sound.ID	sound_id = (Sound.ID)i;

			string	clip_name;

			if(i == (int)Sound.ID.SYSTEM00) {

				clip_name = "all_system00";

			} else {

				clip_name = sound_id.ToString().ToLower();
			}

			AudioClip clip = this.clips.Find(x => x.name == clip_name);

			temp_clips.Add(clip);
		}

		this.clips = temp_clips;
		
		// スロット(AudioSource).

		this.slots = new List<Slot>();

		for(int i = 0;i < (int)Sound.SLOT.NUM;i++) {

			Slot	slot = new Slot();

			slot.source = this.gameObject.AddComponent<AudioSource>();
			slot.timer  = 0.0f;

			this.slots.Add(slot);
		}
		this.slots[(int)Sound.SLOT.BGM].source.loop = true;
		this.slots[(int)Sound.SLOT.SE_GET_ITEM].single_shot = true;
	}

	void	Start()
	{
	}
	
	void	Update()
	{
		if(!this.is_play_sound) {

			this.stopBGM();
		}
	}

	// ================================================================ //

	public void		playBGM(Sound.ID sound_id)
	{
		if(this.is_play_sound) {

			AudioClip		clip = this.clips[(int)sound_id];
	
			if(clip != null) {
	
				Slot	slot = this.slots[(int)Sound.SLOT.BGM];
	
				slot.source.clip = clip;
				slot.source.Play();
			}
		}
	}
	public void		stopBGM()
	{
		this.slots[(int)Sound.SLOT.BGM].source.Stop();
	}

	public void		playSE(Sound.ID sound_id, Sound.SLOT slot_index = Sound.SLOT.SE0)
	{
		if(this.is_play_sound) {

			do {

				AudioClip		clip = this.clips[(int)sound_id];
	
				if(clip == null) {

					break;
				}

				Slot	slot = this.slots[(int)slot_index];

				if(slot.single_shot) {

					if(slot.source.isPlaying) {
	
						break;
					}

					slot.source.clip = clip;
					slot.source.Play();

				} else {

					slot.source.PlayOneShot(clip);
				}

			} while(false);
		}
	}

	// 一定間隔で SE を鳴らす.
	// （毎フレーム呼んでも一定間隔でなります）.
	public void		playSEInterval(Sound.ID sound_id, float interval, Sound.SLOT slot_id)
	{
		if(this.is_play_sound) {

			Slot	slot = this.slots[(int)slot_id];

			if(slot.timer == 0.0f) {
	
				AudioClip		clip = this.clips[(int)sound_id];
		
				if(clip != null) {
		
					slot.source.PlayOneShot(clip);
				}
			}
	
			slot.timer += Time.deltaTime;
	
			if(slot.timer >= interval) {
	
				slot.timer = 0.0f;
			}
		}
	}

	// インターバル SE のタイマーをリセットする.
	public void		stopSEInterval(Sound.SLOT slot_id)
	{
		this.slots[(int)slot_id].timer = 0.0f;
	}

	// ================================================================ //
	// インスタンス.

	private	static SoundManager	instance = null;

	public static SoundManager	getInstance()
	{
		if(SoundManager.instance == null) {

			SoundManager.instance = GameObject.Find("SoundManager").GetComponent<SoundManager>();
		}

		return(SoundManager.instance);
	}

	public static SoundManager	get()
	{
		return(SoundManager.getInstance());
	}
}
