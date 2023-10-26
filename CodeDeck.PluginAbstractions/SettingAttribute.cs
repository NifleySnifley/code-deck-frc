using System.Text.Json;
using System.Text.Json.Serialization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace CodeDeck.PluginAbstractions
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SettingAttribute : Attribute
    {
    }
}
