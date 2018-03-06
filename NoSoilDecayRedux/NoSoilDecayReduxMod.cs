using StardewValley;
using StardewValley.TerrainFeatures;
using SObject = StardewValley.Object;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using PyTK;
using PyTK.Extensions;
using PyTK.Types;
using StardewValley.Buildings;
using StardewValley.Locations;

namespace NoSoilDecayRedux
{
    public class NoSoilDecayReduxMod : Mod
    {
        private readonly string[] FarmLocations = { "Farm", "FarmCave", "Greenhouse" };

        private Dictionary<GameLocation, List<Vector2>> hoeDirtChache;
        private ModConfig Config { get; set; }

        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            SaveEvents.AfterLoad += SaveEvents_AfterLoad;
        }

        public bool ValidLocation(GameLocation location)
        {
            if (!Config.AllowSoilDecayOutsideOfFarm)
                return true;
            return FarmLocations.Contains(location.Name);
        }

        private void SaveEvents_AfterLoad(object sender, System.EventArgs e)
        {
            hoeDirtChache = new Dictionary<GameLocation, List<Vector2>>();

            foreach (var location in GetLocationsAndBuidlings())
            {
                hoeDirtChache[location] = location.terrainFeatures
                    .Where(t => t.Value is HoeDirt)
                    .Select(t => t.Key).ToList();
            }

            new TerrainSelector<HoeDirt>().whenAddedToLocation(AddHoeDirt);
            new TileLocationSelector((l, v) => ValidLocation(l) && hoeDirtChache[l].Contains(v)).whenRemovedFromLocation(restoreTiles);

            /* Legacy Fix */
            "Town".toLocation().objects.Remove(new Vector2(2, 0));
        }

        public void AddHoeDirt(GameLocation location, List<Vector2> spots)
        {
            if (ValidLocation(location))
                spots.ForEach(v => hoeDirtChache[location].AddOrReplace(v));
        }

        public List<GameLocation> GetLocationsAndBuidlings()
        {
            var locations = new List<GameLocation>();

            foreach (GameLocation location in Game1.locations.Where(ValidLocation))
            {
                locations.Add(location);
                if (location is BuildableGameLocation bgl)
                    foreach (Building building in bgl.buildings)
                        if (building.indoors != null)
                            locations.Add(building.indoors);
            }

            return locations;
        }

        private void restoreTiles(GameLocation l, List<Vector2> list)
        {
            if (l != Game1.currentLocation)
            {
                foreach (Vector2 v in list)
                {
                    l.terrainFeatures[v] = Game1.isRaining ? new HoeDirt(1) : new HoeDirt(0);
                    if (l.objects.ContainsKey(v) && l.objects[v] is SObject o && 
                        (o.name.Equals("Weeds") || o.name.Equals("Stone") || o.name.Equals("Twig")))
                        l.objects.Remove(v);
                }
            }
            else
            {
                foreach (Vector2 v in list)
                    hoeDirtChache[l].Remove(v);
            }
        }

    }
    
}
