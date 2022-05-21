using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sidekick.Apis.PoeNinja.Api;
using Sidekick.Apis.PoeNinja.Api.Models;
using Sidekick.Apis.PoeNinja.Models;
using Sidekick.Apis.PoeNinja.Repository;
using Sidekick.Common.Game.Items;
using Sidekick.Common.Game.Languages;
using Sidekick.Common.Settings;

namespace Sidekick.Apis.PoeNinja
{
    /// <summary>
    /// https://poe.ninja/swagger
    /// </summary>
    public class PoeNinjaClient : IPoeNinjaClient
    {
        private readonly Uri POE_NINJA_BASE_URL = new("https://poe.ninja/");
        private readonly string leagueUri;
        private readonly ISettings settings;
        private readonly IPoeNinjaRepository repository;
        private readonly IPoeNinjaApiClient poeNinjaApiClient;

        // Poe.ninja uses a different uri for each item type, always in English.
        private readonly Dictionary<ItemType, string> ItemTypesUris = new()
        {
            { ItemType.Oil, "oils" },
            { ItemType.Incubator, "incubators" },
            { ItemType.Scarab, "scarabs" },
            { ItemType.Fossil, "fossils" },
            { ItemType.Resonator, "resonators" },
            { ItemType.Essence, "essences" },
            { ItemType.DivinationCard, "divination-cards" },
            { ItemType.SkillGem, "skill-gems" },
            { ItemType.UniqueMap, "unique-maps" },
            { ItemType.Map, "maps" },
            { ItemType.UniqueJewel, "unique-jewels" },
            { ItemType.UniqueFlask, "unique-flasks" },
            { ItemType.UniqueWeapon, "unique-weapons" },
            { ItemType.UniqueArmour, "unique-armours" },
            { ItemType.UniqueAccessory, "unique-accessories" },
            { ItemType.Beast, "beasts" },
            { ItemType.Currency, "currency" },
            { ItemType.Fragment, "fragments" },
        };

        public PoeNinjaClient(
            ISettings settings,
            IPoeNinjaRepository repository,
            IPoeNinjaApiClient poeNinjaApiClient)
        {
            this.settings = settings;
            this.repository = repository;
            this.poeNinjaApiClient = poeNinjaApiClient;

            leagueUri = GetLeagueUriFromLeagueId(this.settings.LeagueId);
        }

        /// <summary>
        /// Get Poe.ninja's league uri from POE's API league id.
        /// </summary>
        public string GetLeagueUriFromLeagueId(string leagueId)
        {
            return leagueId switch
            {
                "Standard" => "standard",
                "Hardcore" => "hardcore",
                string x when x.Contains("Hardcore") => "challengehc",
                _ => "challenge"
            };
        }

        public async Task Initialize()
        {
            if (!settings.PoeNinja_LastClear.HasValue || settings.PoeNinja_LastClear < DateTimeOffset.Now.AddHours(-8))
            {
                await repository.Clear();
            }
        }

        public async Task<NinjaPrice> GetPriceInfo(Item item)
        {
            var name = item.Original.Name;
            var type = item.Original.Type;

            foreach (var itemType in GetItemTypes(item))
            {
                var repositoryItems = await repository.Load(itemType);
                if (repositoryItems == null)
                {
                    repositoryItems = await poeNinjaApiClient.FetchPrices(itemType);
                }

                if (repositoryItems == null)
                {
                    continue;
                }

                var translations = await repository.LoadTranslations(itemType);
                if (translations.Any(x => x.Translation == name))
                {
                    name = translations.First(x => x.Translation == name).English;
                }

                var query = repositoryItems.AsQueryable().Where(x => x.Name == name || x.Name == type);

                if (item.Properties != null)
                {
                    query = query.Where(x => x.Corrupted == item.Properties.Corrupted
                                          && x.MapTier == item.Properties.MapTier
                                          && x.GemLevel == item.Properties.GemLevel
                                          && x.IsRelic == item.Properties.IsRelic);

                    // Poe.ninja has pricings for <5, 5 and 6 links.
                    // <5 being 0 links in their API.
                    var highestSocketLinks = item.Sockets.GroupBy(x => x.Group)
                                                         .Select(x => x.Count())
                                                         .Max();

                    query = query.Where(x => x.Links == (highestSocketLinks >= 5 ? highestSocketLinks : 0));
                }

                return query.FirstOrDefault();
            }

            return null;
        }

        public Uri GetDetailsUri(NinjaPrice ninjaPrice)
        {
            if (!string.IsNullOrWhiteSpace(ninjaPrice.DetailsId))
            {
                return new Uri(POE_NINJA_BASE_URL, $"{leagueUri}/{ItemTypesUris[ninjaPrice.ItemType]}/{ninjaPrice.DetailsId}");
            }

            return POE_NINJA_BASE_URL;
        }

        private List<ItemType> GetItemTypes(Item item)
        {
            var result = new List<ItemType>();

            if (item.Metadata.Category == Category.Currency)
            {
                result.Add(ItemType.Currency);
                result.Add(ItemType.Fragment);

                result.Add(ItemType.Incubator);
                result.Add(ItemType.Oil);
                result.Add(ItemType.Incubator);
                result.Add(ItemType.Scarab);
                result.Add(ItemType.Fossil);
                result.Add(ItemType.Resonator);
                result.Add(ItemType.Essence);
                result.Add(ItemType.Resonator);
            }
            else if (item.Metadata.Rarity == Rarity.Unique)
            {
                switch (item.Metadata.Category)
                {
                    case Category.Accessory: result.Add(ItemType.UniqueAccessory); break;
                    case Category.Armour: result.Add(ItemType.UniqueArmour); break;
                    case Category.Flask: result.Add(ItemType.UniqueFlask); break;
                    case Category.Jewel: result.Add(ItemType.UniqueJewel); break;
                    case Category.Map: result.Add(ItemType.UniqueMap); break;
                    case Category.Weapon: result.Add(ItemType.UniqueWeapon); break;
                    case Category.ItemisedMonster: result.Add(ItemType.Beast); break;
                }
            }
            else
            {
                switch (item.Metadata.Category)
                {
                    case Category.DivinationCard: result.Add(ItemType.DivinationCard); break;
                    case Category.Map: result.Add(ItemType.Map); break;
                    case Category.Gem: result.Add(ItemType.SkillGem); break;
                    case Category.ItemisedMonster: result.Add(ItemType.Beast); break;
                }
            }

            return result;
        }
    }
}
