# sdv-waterer

This is a Stardew Valley mod that lets you pay (or not pay) to have your crops watered.

Inspiration comes from the [Replanter](https://www.nexusmods.com/stardewvalley/mods/589) mod. This mod keeps just the auto-watering aspect and charges for it (configurable).

## Config

The `config.json` has the following options:

- `KeyBind` (default: K) - What button the mod should use.
- `Price` (default 0.5) - How much watering each crop costs. Set to 0 to make it free.
  - The final cost of watering everything is rounded down to the nearest whole number.
- `WaterAll` (default false) - Whether all "hoe-d" tiles should be watered, or only the ones where a crop is planted.
  - Set to false (the default) to water crops only.
- `SkipFullyGrown` (default true) - Whether to skip watering fully-grown crops.
- `Message` (default true) - Whether to show a message afterwards showing how many crops were watered and how much it cost.
- `AutoWaterEveryDay` (default false) - Whether to (try to) water crops at the start of every day.

## Note to Developers

I'm not familiar with C# projects so stuff might look/be weird.
