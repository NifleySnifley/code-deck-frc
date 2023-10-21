using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace CodeDeck.Models.Configuration
{
    public class PluginConfiguration
    {
        public string? Name { get; set; }

        public JsonObject? Settings { get; set; }
    }
}
