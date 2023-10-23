using System.Text.Json.Nodes;
using SixLabors.ImageSharp;

namespace CodeDeck.PluginAbstractions
{
    public class TileStyle
    {
        public Color? BackgroundColor { get; set; } = null;
        public Color? TextColor { get; set; } = null;
        public Image? Image { get; set; } = null;
        public string? Text { get; set; } = null;
        public float? FontSize { get; set; } = null;
        public string? Font { get; set; } = null;

        public static TileStyle LoadFrom(Tile t)
        {
            return new TileStyle
            {
                BackgroundColor = t.BackgroundColor,
                TextColor = t.TextColor,
                Image = t.Image,
                Text = t.Text,
                FontSize = t.FontSize,
                Font = t.Font
            };
        }

        public TileStyle AddDefault(TileStyle def)
        {
            BackgroundColor ??= def.BackgroundColor;
            TextColor ??= def.TextColor;
            Image ??= def.Image;
            Text ??= def.Text;
            FontSize ??= def.FontSize;
            Font ??= def.Font;
            return this;
        }

        public void Apply(Tile t)
        {
            t.BackgroundColor = BackgroundColor;
            t.TextColor = TextColor;
            t.Text = Text;
            t.FontSize = FontSize;
            t.Font = Font;
            t.Image = Image;
        }
    }

    /// <summary>
    /// Represents a Tile, a Tile can react to key presses or just display data
    /// </summary>
    public class Tile
    {
        /// <summary>
        /// Set by the framework, notifies the framework that the tile needs to be updated on the Deck
        /// </summary>
        public Action? NotifyChange { get; set; }


        private string? _text;
        public string? Text
        {
            get { return _text; }
            set
            {
                _text = value;
                NotifyChange?.Invoke();
            }
        }

        private Color? _textColor;
        public Color? TextColor
        {
            get => _textColor; set
            {
                _textColor = value;
                NotifyChange?.Invoke();
            }
        }

        private Color? _bgColor;
        public Color? BackgroundColor
        {
            get => _bgColor; set
            {
                _bgColor = value;
                NotifyChange?.Invoke();
            }
        }

        private string? _font;
        public string? Font
        {
            get => _font; set
            {
                _font = value;
                NotifyChange?.Invoke();
            }
        }

        private float? _fontSize;
        public float? FontSize
        {
            get => _fontSize; set
            {
                _fontSize = value;
                NotifyChange?.Invoke();
            }
        }

        private Image? _image;
        public Image? Image
        {
            get => _image; set
            {
                _image = value;
                NotifyChange?.Invoke();
            }
        }

        private int? _imagePadding;
        public int? ImagePadding
        {
            get => _imagePadding; set
            {
                _imagePadding = value;
                NotifyChange?.Invoke();
            }
        }

        private bool? _showIndicator;
        public bool? ShowIndicator
        {
            get => _showIndicator; set
            {
                _showIndicator = value;
                NotifyChange?.Invoke();
            }
        }

        private Color? _indicatorColor;
        public Color? IndicatorColor
        {
            get => _indicatorColor; set
            {
                _indicatorColor = value;
                NotifyChange?.Invoke();
            }
        }

        // TODO: Convert to JsonObject, then it can be parsed into each plugin's tile instance with no typing issues and no further parsing required
        public JsonObject? Settings { get; set; }

        public Tile() { }

        public virtual async Task Init(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }

        public virtual async Task DeInit()
        {
            await Task.CompletedTask;
        }

        public virtual async Task OnTilePressDown(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }

        public virtual async Task OnTilePressUp(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }
}
