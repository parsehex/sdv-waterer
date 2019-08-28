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
		private bool autoWatering = false;
		private int cropsSkipped = 0;

		private void log(string text) {
			if (!this.config.debug) return;
			this.Monitor.Log(text);
		}

		public override void Entry(IModHelper helper) {
			this.config = this.Helper.ReadConfig<ModConfig>();

			helper.Events.Input.ButtonPressed += this.OnButtonPressed;
			helper.Events.GameLoop.DayStarted += this.OnDayStarted;

			this.log("Mod loaded");

			if (this.config.debug) this.log("Debug mode enabled");
		}

		private void OnButtonPressed(object sender, ButtonPressedEventArgs e) {
			// not in-game
			if (!Context.IsWorldReady) return;

			// not keybind button
			if (e.Button.ToString() != this.config.KeyBind) return;

			this.resetState();

			this.log($"Pressed {this.config.KeyBind}");

			this.activate();
		}

		private void OnDayStarted(object sender, DayStartedEventArgs e) {
			if (!this.config.AutoWaterEveryDay) return;

			this.resetState();

			this.log("Auto-watering");
			this.autoWatering = true;

			this.activate();
		}

		private void activate() {
			Farmer farmer = Game1.player;

			if (farmer.Money == 0 && this.config.Price > 0) {
				HUDMessage msg = new HUDMessage("Out of gold!", 3); // 3 = error
				Game1.addHUDMessage(msg);
				return;
			}

			this.waterAllCrops(farmer);
			this.chargeFarmer(farmer);
		}

		private void resetState() {
			this.cropsWatered = 0;
			this.ranOutOfMoney = false;
			this.autoWatering = false;
			this.cropsSkipped = 0;
			this.maxCropsToWater = this.calculateMaxAffordable(Game1.player);
		}

		private void chargeFarmer(Farmer farmer) {
			int cost = this.calculateCost();

			farmer.Money -= cost;
			this.log($"Charged {farmer.Name} {cost}g");

			if (this.ranOutOfMoney) {
				// show out-of-money-message as well as how much got done
				HUDMessage msg = new HUDMessage("Couldn't water everything (no more gold)!", 3); // 3 = error
				Game1.addHUDMessage(msg);
			}

			if (this.cropsWatered == 0 && !this.ranOutOfMoney) {
				HUDMessage msg = new HUDMessage("There's nothing to water", 3); // 3 = error
				Game1.addHUDMessage(msg);
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

			if (this.cropsSkipped > 0) {
				text += $" ({this.cropsSkipped} skipped)";
			}

			if (cost > 0) {
				text += $", costing {cost}g";
			}

			HUDMessage msg = new HUDMessage(text, 2); // 2 = yellow exclamation
			Game1.addHUDMessage(msg);
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

				Crop crop = dirt.crop;

				// if WaterAll is false, only water crops
				if (!this.config.WaterAll && crop == null) {
					this.log("Skipping non-crop");
					continue;
				}

				// skip fully-grown crops with SkipFullyGrown
				// (always water regrowing crops though)
				// NOTE crop.fullyGrown seems to always return false
				if (
					this.config.SkipFullyGrown &&
					(crop?.regrowAfterHarvest.Value == -1 && crop?.currentPhase == crop?.phaseDays.Count - 1)
				) {
					this.log("Skipping fully-grown crop");
					continue;
				}

				// don't re-water
				if (dirt.state.Value == 1) {
					this.log("Skipping already-watered crop");
					continue;
				}

				if (this.autoWatering && !this.shouldAutoWaterCrop(location, crop)) {
					this.log("Not auto-watering crop");
					this.cropsSkipped++;
					continue;
				}

				dirt.state.Value = 1;
				this.cropsWatered++;
			}
		}

		// avoid auto watering plants that won't be harvestable by the end of the season
		private bool shouldAutoWaterCrop(GameLocation location, Crop crop) {
			// crops grow anytime in the greenhouse
			if (location.Name.Contains("Greenhouse")) return true;

			int totalDays = 0;
			foreach (int p in crop.phaseDays) {
				if (p == 99999) continue; // what is this?

				totalDays += p;
			}

			int remainingSeasonDays = 28 - Game1.dayOfMonth;

			// easy check
			if (remainingSeasonDays >= totalDays) return true;

			// crop grows in next season
			if (crop.seasonsToGrowIn.Contains(this.getNextSeason(Game1.currentSeason))) return true;

			int remainingGrowthDays;
			if (crop.regrowAfterHarvest != -1 && crop.currentPhase >= crop.phaseDays.Count - 1) {
				// crop is in regrowth phase
				// NOTE dayOfCurrentPhase counts down once regrowing starts
				remainingGrowthDays = crop.regrowAfterHarvest - (crop.regrowAfterHarvest - crop.dayOfCurrentPhase);
			} else {
				remainingGrowthDays = totalDays - crop.dayOfCurrentPhase;
			}

			return remainingSeasonDays >= remainingGrowthDays;
		}

		private string getNextSeason(string season) {
			switch (season) {
				case "spring": {
					return "summer";
				}
				case "summer": {
					return "fall";
				}
				case "fall": {
					return "winter";
				}
				case "winter": {
					return "spring";
				}
				default: {
					return "spring";
				}
			}
		}

		private int calculateMaxAffordable(Farmer farmer) {
			if (this.config.Price == 0) return -1; // infinity

			int gold = farmer.Money;
			return (int)Math.Floor(gold / this.config.Price);
		}

		private int calculateCost() {
			if (this.config.Price == 0) return 0;
			if (this.cropsWatered == 0) return 0;

			 // charge at least 1g
			return Math.Max(1, (int)Math.Round(this.config.Price * this.cropsWatered));
		}
	}
}
