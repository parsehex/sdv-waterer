using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace Waterer {
	public class ModEntry : Mod {
		private ModConfig config;
		private int cropsWatered = 0;
		private int maxCropsToWater = 0;
		private bool ranOutOfMoney = false;

		private void log(string text) {
			if (!this.config.debug) return;
			this.Monitor.Log(text);
		}

		public override void Entry(IModHelper helper) {
			this.config = this.Helper.ReadConfig<ModConfig>();
			helper.Events.Input.ButtonPressed += this.OnButtonPressed;

			this.log("Mod loaded");
		}

		private void OnButtonPressed(object sender, ButtonPressedEventArgs e) {
			// not in-game
			if (!Context.IsWorldReady) return;

			if (e.Button.ToString() == this.config.KeyBind) {
				this.log($"Pressed {this.config.KeyBind}");

				Farmer farmer = Game1.player;

				if (farmer.Money == 0 && this.config.Price > 0) {
					HUDMessage msg = new HUDMessage("Out of gold!");
					Game1.addHUDMessage(msg, 3); // 3 = error
					return;
				}

				// reset
				this.cropsWatered = 0;
				this.ranOutOfMoney = false;
				this.maxCropsToWater = this.calculateMaxAffordable(farmer);

				this.waterAllCrops(farmer);

				this.chargeFarmer(farmer);
			}
		}

		private void chargeFarmer(Farmer farmer) {
			int cost = this.calculateCost();

			farmer.Money -= cost;
			this.log($"Charged {farmer.Name} {cost}g");

			if (this.ranOutOfMoney) {
				// show out-of-money-message as well as how much got done
				HUDMessage msg = new HUDMessage("Couldn't water everything (no more gold)!");
				Game1.addHUDMessage(msg, 3); // 3 = error
			}

			this.showMessage(cost);
		}

		private void showMessage(int cost) {
			if (!this.config.Message) return;

			string text = "";

			text += $"Watered {this.cropsWatered}";

			if (this.config.WaterAll) {
				text += " tiles";
			} else {
				text += " crops";
			}

			if (cost > 0) {
				text += $", costing {cost}g";
			}

			HUDMessage msg = new HUDMessage(text);
			Game1.addHUDMessage(msg, 2); // 2 = yellow exclamation
		}

		private void waterAllCrops(Farmer farmer) {
			this.log("Watering crops");

			foreach (GameLocation location in Game1.locations) {
				// skip non-farm and non-greenhouse locations
				if (!location.IsFarm && !location.Name.Contains("Greenhouse")) continue;

				this.waterCrops(location, farmer);
			}
		}

		private void waterCrops(GameLocation location, Farmer farmer) {
			// loop through each tile
			foreach (KeyValuePair<Vector2, TerrainFeature> feature in location.terrainFeatures.Pairs) {
				// can't afford any more
				// (-1 is infinity)
				if (this.maxCropsToWater > -1 && this.cropsWatered >= this.maxCropsToWater) {
					this.ranOutOfMoney = true;
					return;
				}

				// skip tiles that are nothing
				if (feature.Value == null) continue;

				// skip non-hoed dirt
				if (!(feature.Value is HoeDirt dirt)) continue;

				// only water crops if WaterAll is false
				if (!this.config.WaterAll && dirt.crop == null) continue;

				// don't re-water
				if (dirt.state.Value == 1) continue;

				dirt.state.Value = 1;
				this.cropsWatered++;
			}
		}

		private int calculateMaxAffordable(Farmer farmer) {
			if (this.config.Price == 0) return -1; // infinity

			int gold = farmer.Money;
			return (int)Math.Floor(gold / this.config.Price);
		}

		private int calculateCost() {
			if (this.config.Price == 0) return 0;

			return (int)Math.Round(this.config.Price * this.cropsWatered);
		}
	}
}
