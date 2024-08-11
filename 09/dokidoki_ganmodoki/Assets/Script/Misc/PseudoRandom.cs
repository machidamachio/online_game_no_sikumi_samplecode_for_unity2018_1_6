using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PseudoRandom : MonoBehaviour {

	protected List<Seed>	seeds;					// 乱数のシード　全端末通して、同じ id なら同じシードになる.

	// ================================================================ //

	public void		create()
	{
		this.seeds = new List<Seed>();
	}

	// 乱数生成オブジェクトを作る.
	public Plant	createPlant(string id, int cycle = 16)
	{
		Plant	plant = null;

		do {

			Seed	seed = this.create_seed(id);

			if(seed == null) {

				break;
			}

			plant = new Plant(seed, cycle);

		} while(false);

		return(plant);
	}

	protected Seed	create_seed(string id)
	{
		string	local_account = AccountManager.get().getAccountData(GlobalParam.get().global_account_id).account_id;

		Seed	seed = null;

		seed = this.seeds.Find(x => x.id == id);

		if(seed == null) {

			// 見つからなかったので、作る.
			seed = new Seed(local_account, id);

			this.seeds.Add(seed);

			// [TODO] seeds が全端末で共通になるよう、同期する.

		} else {

			if(seed.creator == local_account) {
	
				// 同じ id のシードを２回以上作ろうとした.
				Debug.LogError("Seed \"" + id + "\" already exist.");
				seed = null;

			} else {

				// 他のプレイヤーが作った同名のシードがあった.
			}
		}

		return(seed);
	}

	// ================================================================ //

	protected static PseudoRandom	instance = null;

	public static PseudoRandom	get()
	{
		if(PseudoRandom.instance == null) {

			GameObject	go = new GameObject("PseudoRandom");

			PseudoRandom.instance = go.AddComponent<PseudoRandom>();
			PseudoRandom.instance.create();
		}

		return(PseudoRandom.instance);
	}

	// ================================================================ //

	// 乱数のシード.
	public class Seed {

		public Seed(string creator, string id)
		{
			this.seed = 0;	// （仮）　タイマーとかにする？.
			this.creator = creator;
			this.id = id;
		}
		public int	getSeed()
		{
			return(this.seed);
		}
		protected int	seed;
		public string	creator;		// 生成したアカウントの名まえ.
		public string	id;				// ユニークな id.　全端末共通でユニーク.
	}

	// 乱数生成オブジェクト.
	public class Plant {

		protected List<float>	values;
		protected int			read_index;
		protected Seed			seed;

		public Plant(Seed seed, int cycle)
		{
			this.seed = seed;

			Random.seed = this.seed.getSeed();

			this.values = new List<float>();

			for(int i = 0;i < cycle;i++) {

				this.values.Add(Random.Range(0.0f, 1.0f));
			}

			this.read_index = 0;
		}

		public float	getRandom()
		{
			float	random = this.values[this.read_index];

			this.read_index = (this.read_index + 1)%this.values.Count;

			return(random);
		}

		public int		getRandomInt(int max)
		{
			int		random = (int)(this.getRandom()*(float)max);

			random %= max;

			return(random);
		}
	}
}


