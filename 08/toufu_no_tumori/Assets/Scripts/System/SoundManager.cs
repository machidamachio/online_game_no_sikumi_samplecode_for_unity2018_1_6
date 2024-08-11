using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public struct Sound {

	// サウンドアセットと同じ名前にしておいてください.
	public enum ID {

		NONE = -1,

		SYSTEM00 = 0,		// 汎用クリック音.
		TFT_BGM01,			// BGM.
		SMN_JINGLE01,		// 誰かが遊びにきたときのジングル.
		TFT_SE01,			// アイテムをとった時.
		DDG_SE_PLAYER04,	// 足音（いらないかも）.
		TFT_SE02A,			// 足音１.
		TFT_SE02B,			// 足音２.
		NUM,
	};

	public enum SLOT {

		NONE = -1,

		BGM = 0,
		SE0,
		SE_WALK0,
		SE_WALK1,

		NUM,
	};
};

public class SoundManager : MonoBehaviour {

	public class Slot {

		public AudioSource		source = null;
		public float			timer  = 0.0f;		// インターバル再生用タイマー.
		public int				sel    = 0;			// インターバル再生用サウンドインデックス.
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
	
			if(clip != null && this.slots != null) {
	
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

	public void		playSE(Sound.ID sound_id)
	{
		if(this.is_play_sound) {

			AudioClip		clip = this.clips[(int)sound_id];
	
			if(clip != null && this.slots != null) {
	
				this.slots[(int)Sound.SLOT.SE0].source.PlayOneShot(clip);
			}
		}
	}

	// 一定間隔で SE を鳴らす.
	// （毎フレーム呼んでも一定間隔でなります）.
	public void		playSEInterval(Sound.ID sound_id, float interval, Sound.SLOT slot_id)
	{
		if(this.is_play_sound && this.slots != null) {

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

	// 一定間隔で SE を順番に鳴らす.
	// （毎フレーム呼んでも一定間隔でなります）.
	public void		playSEInterval(Sound.ID[] sound_ids, float interval, Sound.SLOT slot_id)
	{
		if(this.is_play_sound && this.slots != null) {

			Slot	slot = this.slots[(int)slot_id];

			if(slot.timer == 0.0f) {
	
				AudioClip		clip = this.clips[(int)sound_ids[slot.sel]];
		
				if(clip != null) {
		
					slot.source.PlayOneShot(clip);
				}

				slot.sel = (slot.sel + 1)%sound_ids.Length;
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
		if (this.slots != null) {
			this.slots[(int)slot_id].timer = 0.0f;
		}
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
