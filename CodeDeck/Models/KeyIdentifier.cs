using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenMacroBoard.SDK;
using StreamDeckSharp;

namespace CodeDeck.Models
{
	[JsonConverter(typeof(KeyIdentifierConverter))]
	public class KeyIdentifier
	{
		public int x = -1;
		public int y = -1;

		public KeyIdentifier()
		{

		}

		public KeyIdentifier(int x, int y)
		{
			this.x = x;
			this.y = y;
		}

		public KeyIdentifier(string v)
		{
			ParseFromString(v);
		}

		public bool ParseFromString(string value)
		{
			string[] vals = value.Split(",");
			if (vals.Length != 2) return false;
			try
			{
				x = int.Parse(vals[0]);
				y = int.Parse(vals[1]);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static KeyIdentifier? TryParse(string v)
		{
			KeyIdentifier i = new();
			if (i.ParseFromString(v))
				return i;
			return null;
		}

		public override string ToString()
		{
			return $"{x},{y}";
		}

		public bool IsValid(IMacroBoard deck)
		{
			return x >= 0 && y >= 0 && x < deck.Keys.CountX && y < deck.Keys.CountY;
		}

		public int ConvertToIndex(IMacroBoard deck)
		{
			return y * deck.Keys.CountX + x;
		}
	}

	public class KeyIdentifierConverter : JsonConverter<KeyIdentifier>
	{
		public override KeyIdentifier? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			return KeyIdentifier.TryParse(reader.GetString() ?? "");
		}

		public override void Write(Utf8JsonWriter writer, KeyIdentifier value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value.ToString());
		}
	}
}