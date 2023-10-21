using CodeDeck.PluginAbstractions;
using System;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using NetworkTables;
using System.Collections.Generic;
using System.Linq;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using System.Text.Json;
using System.Net.Security;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using System.Text.Unicode;

namespace CodeDeck.Plugins.Plugins.FRC
{
	public class FRCPlugin : CodeDeckPlugin
	{
		static bool ntinit = false;

		static FRCPlugin()
		{
		}

		static void InitNT()
		{
			if (!ntinit)
			{
				NetworkTable.SetClientMode();
				int team = Settings?["Team"]?.GetValue<int>() ?? 2530;
				NetworkTable.SetTeam(team);
				NetworkTable.SetNetworkIdentity("FRCDeck");
				if (Settings?.ContainsKey("IP") ?? false)
					NetworkTable.SetIPAddress(Settings?["IP"]?.GetValue<string>() ?? "localhost");
				NetworkTable.Initialize();
				ntinit = true;
			}
		}

		public class NTValueWrapper
		{
			string? path;
			static Dictionary<string, NTValueWrapper> remotes = new();

			public NTValueWrapper()
			{

			}

			public NTValueWrapper(string path)
			{
				this.path = path;
				NtCore.AddEntryListener(path, (uid, key, value, flags) =>
			{
				ChangeListener?.Invoke(this, EventArgs.Empty);
			}, NotifyFlags.NotifyNew | NotifyFlags.NotifyUpdate | NotifyFlags.NotifyLocal | NotifyFlags.NotifyImmediate);
			}

			public static NTValueWrapper GetWrapper(string path)
			{
				if (remotes.ContainsKey(path))
				{
					return remotes[path];
				}
				else
				{
					return new NTValueWrapper(path);
				}
			}

			public static NTValueWrapper Empty()
			{
				return new NTValueWrapper();
			}

			public event EventHandler? ChangeListener;

			public Value value
			{
				get
				{
					if (path is not null)
						return NtCore.GetEntryValue(path);
					return new Value();
				}
				set
				{
					if (path is not null)
						NtCore.SetEntryValue(path, value);
				}
			}
		}

		public static NtType ParseNTType(string typename)
		{
			return new Dictionary<string, NtType>
			{
				["int"] = NtType.Double,
				["uint"] = NtType.Double,
				["float"] = NtType.Double,
				["double"] = NtType.Double,
				["number"] = NtType.Double,
				["string"] = NtType.String,
				["bool"] = NtType.Boolean,
				["boolean"] = NtType.Boolean,
				["double[]"] = NtType.DoubleArray,
				["float[]"] = NtType.DoubleArray,
				["number[]"] = NtType.DoubleArray,
				["int[]"] = NtType.DoubleArray,
				["uint[]"] = NtType.DoubleArray,
				["string[]"] = NtType.StringArray,
				["bool[]"] = NtType.BooleanArray,
				["boolean[]"] = NtType.BooleanArray,
			}.GetValueOrDefault(typename.ToLower(), NtType.Unassigned);
		}

		public static Value? ParseNTValue(NtType typename, string strrep)
		{
			switch (typename)
			{
				case NtType.Double:
					double v;
					if (double.TryParse(strrep, out v))
						return Value.MakeValue(v);
					else
						return null;
				case NtType.Boolean:
					bool b;
					if (bool.TryParse(strrep, out b))
						return Value.MakeValue(b);
					else
						return null;
				case NtType.String:
					return Value.MakeValue(strrep);
				case NtType.DoubleArray:
					double[]? arrd = JsonSerializer.Deserialize<double[]>(strrep);
					if (arrd is not null)
						return Value.MakeValue(arrd);
					else
						return null;
				case NtType.StringArray:
					string[]? arrs = JsonSerializer.Deserialize<string[]>(strrep);
					if (arrs is not null)
						return Value.MakeValue(arrs);
					else
						return null;
				case NtType.BooleanArray:
					bool[]? arrb = JsonSerializer.Deserialize<bool[]>(strrep);
					if (arrb is not null)
						return Value.MakeValue(arrb);
					else
						return null;
			}
			return null;
		}

		public static Value? ParseNTValueJSON(JsonValue? v)
		{
			if (v is null) return null;
			if (v.TryGetValue<double>(out double d))
			{
				return Value.MakeDouble(d);
			}
			else if (v.TryGetValue<int>(out int i))
			{
				return Value.MakeDouble(i);
			}
			else if (v.TryGetValue<string>(out string? s) && s is not null)
			{
				return Value.MakeString(s);
			}
			else if (v.TryGetValue<bool>(out bool b))
			{
				return Value.MakeBoolean(b);
			}
			else if (v.TryGetValue<double[]>(out double[]? ds) && ds is not null)
			{
				return Value.MakeDoubleArray(ds);
			}
			else if (v.TryGetValue<int[]>(out int[]? ins) && ins is not null)
			{
				return Value.MakeDoubleArray(ins.Select(x => (double)x).ToArray());
			}
			else if (v.TryGetValue<bool[]>(out bool[]? bs) && bs is not null)
			{
				return Value.MakeBooleanArray(bs);
			}
			else if (v.TryGetValue<string[]>(out string[]? ss) && ss is not null)
			{
				return Value.MakeStringArray(ss);
			}

			return null;
		}

		public class NTValueConverter : JsonConverter<Value>
		{
			public override Value? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				return ParseNTValueJSON(JsonObject.Parse(ref reader, null)?.AsValue());
			}

			public override void Write(Utf8JsonWriter writer, Value value, JsonSerializerOptions options)
			{
				writer.WriteStringValue(value.IsString() ? $"\"{value}\"" : value.ToString());
			}
		}

		public static bool CompareNTValue(Value a, Value b)
		{
			if (a.Type == b.Type)
			{
				object ao = a.GetObjectValue();
				object bo = b.GetObjectValue();
				if (a.Type == NtType.BooleanArray)
				{
					bool[] A = a.GetBooleanArray();
					bool[] B = b.GetBooleanArray();
					return Enumerable.SequenceEqual(A, B);
				}
				else if (a.Type == NtType.DoubleArray)
				{
					double[] A = a.GetDoubleArray();
					double[] B = b.GetDoubleArray();
					return Enumerable.SequenceEqual(A, B);
				}
				else if (a.Type == NtType.BooleanArray)
				{
					string[] A = a.GetStringArray();
					string[] B = b.GetStringArray();
					return Enumerable.SequenceEqual(A, B);
				}
				else
				{
					return Equals(ao, bo);
				}
			}
			else
			{
				return false;
			}
		}

		public class NTBooleanButton : Tile
		{
			NTValueWrapper remote = NTValueWrapper.Empty();

			[Setting] public Color TrueColor { get; set; } = Color.Transparent;

			[Setting] public Color FalseColor { get; set; } = Color.Transparent;

			[Setting] public bool Toggle { get; set; } = false;
			bool state = false;


			public override Task Init(CancellationToken cancellationToken)
			{
				InitNT();

				remote = NTValueWrapper.GetWrapper(Settings?["NTPath"]?.GetValue<string>() ?? nameof(NTBooleanButton));
				remote.ChangeListener += ChangeListener;

				if (Settings?.ContainsKey("Initial") ?? false)
					remote.value = Value.MakeBoolean(bool.Parse(Settings["Initial"]?.GetValue<string>() ?? ""));

				ChangeListener(this, EventArgs.Empty);

				return base.Init(cancellationToken);
			}

			private void ChangeListener(object? sender, EventArgs e)
			{
				state = remote is not null && remote.value is not null && remote.value.IsBoolean() && remote.value.GetBoolean();
				Update();
			}

			private void Update()
			{
				BackgroundColor = state ? TrueColor : FalseColor;
			}

			public override Task OnTilePressDown(CancellationToken cancellationToken)
			{
				if (Toggle) state = !state;
				else
				{
					state = true;
				}
				remote.value = Value.MakeBoolean(state);
				return base.OnTilePressDown(cancellationToken);
			}

			public override Task OnTilePressUp(CancellationToken cancellationToken)
			{
				if (!Toggle)
				{
					state = false;
					remote.value = Value.MakeBoolean(state);
				}
				return base.OnTilePressUp(cancellationToken);
			}

			public override Task DeInit()
			{
				return base.DeInit();
			}
		}


		public class NTBooleanIndicator : Tile
		{
			NTValueWrapper remote = NTValueWrapper.Empty();
			[Setting] public Color TrueColor { set; get; } = Color.Green;
			[Setting] public Color FalseColor { get; set; } = Color.Red;

			[Setting] public Image? TrueImage { get; set; } = null;
			[Setting] public Image? FalseImage { get; set; } = null;

			[Setting]
			public JsonObject? TestSetting { get; set; } = null;

			public override Task Init(CancellationToken cancellationToken)
			{
				InitNT();

				remote = NTValueWrapper.GetWrapper(Settings?["NTPath"]?.GetValue<string>() ?? nameof(NTBooleanButton));
				remote.ChangeListener += ChangeListener;

				SetIconState(remote is not null && remote.value is not null && remote.value.IsBoolean() && remote.value.GetBoolean());
				return base.Init(cancellationToken);
			}

			private void SetIconState(bool state)
			{
				BackgroundColor = state ? TrueColor : FalseColor;

				Text = state ? (Settings?["TrueText"]?.GetValue<string>() ?? "") : (Settings?["FalseText"]?.GetValue<string>() ?? "");

				Image? isel = state ? TrueImage : FalseImage;
				if (isel is not null) Image = isel;
			}

			private void ChangeListener(object? sender, EventArgs e)
			{
				SetIconState(remote.value.GetBoolean());
			}

			public override Task OnTilePressDown(CancellationToken cancellationToken)
			{
				return base.OnTilePressDown(cancellationToken);
			}

			public override Task OnTilePressUp(CancellationToken cancellationToken)
			{
				return base.OnTilePressUp(cancellationToken);
			}

			public override Task DeInit()
			{
				return base.DeInit();
			}
		}

		public class NTValueSetter : Tile
		{
			private NTValueWrapper remote = NTValueWrapper.Empty();
			[Setting] public string NTPath { get; set; } = nameof(NTValueSetter);

			[Setting] public Color SelectedColor { get; set; } = Color.Green;
			[Setting] public Color UnselectedColor { get; set; } = Color.Transparent;

			// NtType type;
			[Setting][JsonConverter(typeof(NTValueConverter))] public Value Value { get; set; } = new Value();
			[Setting][JsonConverter(typeof(NTValueConverter))] public Value? Initial { get; set; } = null;


			public override async Task Init(CancellationToken cancellationToken)
			{
				InitNT();

				remote = NTValueWrapper.GetWrapper(NTPath);
				remote.ChangeListener += ChangeListener;

				// Console.WriteLine(Settings?.ToJsonString());
				// Console.WriteLine(Settings?["Value"]?.GetValue<string>());

				// thisbuttonvalue = ParseValue(Settings?["Value"]?.GetValue<string>()) ?? Value.MakeString(Text);
				// thisinitvalue = ParseValue(Settings?["Initial"]?.GetValue<string>());

				if (Initial is not null)
					remote.value = Initial;

				// TODO: Find a better way to get an initial value!
				await Task.Delay(800);

				SetIconState();

				await base.Init(cancellationToken);
			}

			// TODO: Move to upper class, parse type AND value (tuple)
			// private Value? ParseValue(string? value)
			// {
			// 	if (value is null) return null;
			// 	string[] parts = value.Split(':');
			// 	if (parts.Length != 2) return null;
			// 	type = ParseNTType(parts[0]);
			// 	return ParseNTValue(type, parts[1]) ?? new Value();
			// }

			private void SetIconState()
			{
				bool state = CompareNTValue(remote.value, Value);
				BackgroundColor = state ? SelectedColor : UnselectedColor;
			}

			private void ChangeListener(object? sender, EventArgs e)
			{
				SetIconState();
			}

			public override Task OnTilePressDown(CancellationToken cancellationToken)
			{
				remote.value = Value;
				SetIconState();
				return base.OnTilePressDown(cancellationToken);
			}

			public override Task OnTilePressUp(CancellationToken cancellationToken)
			{
				return base.OnTilePressUp(cancellationToken);
			}

			public override Task DeInit()
			{
				return base.DeInit();
			}
		}

		public class NTNumberChanger : Tile
		{
			private NTValueWrapper remote = NTValueWrapper.Empty();
			[Setting] public string NTPath { get; set; } = nameof(NTNumberChanger);

			[Setting] public double Increment { get; set; }
			[Setting] public double Min { get; set; } = double.MinValue;
			[Setting] public double Max { get; set; } = double.MaxValue;
			[Setting] public double? Initial { get; set; }
			[Setting] public bool Holdable { get; set; } = false;
			[Setting] public int? HoldPulseMS { get; set; }
			[Setting] public int? HoldSenseMS { get; set; }

			private CancellationTokenSource _bgTaskCancel = new();

			[Setting] public Color? MaxColor { get; set; }
			[Setting] public Color? MinColor { get; set; }

			private Color DefaultColor;

			public override Task Init(CancellationToken cancellationToken)
			{
				InitNT();

				DefaultColor = BackgroundColor ?? Color.Transparent;

				remote = NTValueWrapper.GetWrapper(NTPath);
				remote.ChangeListener += ChangeListener;

				if (Initial is not null)
					remote.value = Value.MakeDouble(Initial.Value);

				return base.Init(cancellationToken);
			}

			private async Task HoldTask(CancellationToken cancellationToken)
			{
				Execute();
				await Task.Delay(HoldSenseMS ?? 500); // Initial wait for press!
				while (!cancellationToken.IsCancellationRequested)
				{
					Execute();
					await Task.Delay(HoldPulseMS ?? 100, cancellationToken);
				}
			}

			private void Execute()
			{
				remote.value = Value.MakeDouble(Math.Clamp(remote.value.GetDouble() + Increment, Min, Max));
				SetIconState();
			}

			private void SetIconState()
			{
				if (remote.value.GetDouble() <= Min && MinColor is not null)
				{
					BackgroundColor = MinColor;
				}
				else if (remote.value.GetDouble() >= Max && MaxColor is not null)
				{
					BackgroundColor = MaxColor;
				}
				else
				{
					BackgroundColor = DefaultColor;
				}
			}

			private void ChangeListener(object? sender, EventArgs e)
			{
				SetIconState();
			}

			public async override Task OnTilePressDown(CancellationToken cancellationToken)
			{
				if (Holdable)
				{
					_bgTaskCancel.Cancel();
					_bgTaskCancel = new();
					await Task.Run(async () =>
					await HoldTask(_bgTaskCancel.Token), _bgTaskCancel.Token);
				}
				else
				{
					Execute();
				}
				await base.OnTilePressDown(cancellationToken);
			}

			public override Task OnTilePressUp(CancellationToken cancellationToken)
			{
				_bgTaskCancel.Cancel();
				return base.OnTilePressUp(cancellationToken);
			}

			public override Task DeInit()
			{
				return base.DeInit();
			}
		}
	}
}