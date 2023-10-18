using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OpenMacroBoard.SDK;

namespace CodeDeck.Models.Configuration
{
    /// <summary>
    /// Configuration class for a complete Deck
    /// </summary>
    public class StreamDeckConfiguration
    {
        [JsonInclude]
        public int Rotation = 0;

        public string? DevicePath { get; set; }

        public int Brightness { get; set; } = 75;

        public string? FallbackFont { get; set; } = "Twemoji Mozilla";

        public List<PluginConfiguration> Plugins { get; set; } = new();

        public List<Profile> Profiles { get; set; } = new();

        public void PrepareConfiguration(ILogger<object> l, IMacroBoard deck)
        {
            foreach (Profile prof in Profiles)
                foreach (Page pg in prof.Pages)
                {
                    int keylen = pg.Keys.Count();
                    pg.Keys = pg.Keys.Where(k => k.ID != null && k.ID.IsValid(deck)).ToList();
                    if (pg.Keys.Count() != keylen)
                        l.LogWarning("Some keys with invalid locations were removed");
                    foreach (Key key in pg.Keys)
                    {
                        // Console.WriteLine($"ID: {key.ID?.x}, {key.ID?.y}");
                        key.Index = (key.ID ?? new KeyIdentifier()).ConvertToIndex(deck);
                    }
                }

        }
    }
}
