
	class ModConfig {
		// key to press
		public string KeyBind { get; set; } = "K";

		// how much to charge for each watering
		// the final cost is rounded to the nearest whole number
		public float Price { get; set; } = 0.5f;

		// whether to water all Hoed Dirt tiles
		// true = water all Hoed Dirt tiles
		// false = only water Hoed Dirt where a crop is planted
		public bool WaterAll { get; set; } = false;

		// whether to show a message in-game after the deed is done
		public bool Message { get; set; } = true;

		public bool debug {get;set;} = false;
	}
