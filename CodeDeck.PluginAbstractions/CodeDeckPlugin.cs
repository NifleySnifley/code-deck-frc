using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace CodeDeck.PluginAbstractions
{
    /// <summary>
    /// The base class for all plugins
    /// </summary>
    public abstract class CodeDeckPlugin
    {
        public static JsonObject? Settings { get; set; }
    }
}
