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
				int team = int.Parse(Settings?.GetValueOrDefault("Team", "2530") ?? "2530");
				NetworkTable.SetTeam(team);
				NetworkTable.SetNetworkIdentity("FRCDeck");
				if (Settings?.ContainsKey("IP") ?? false)
					NetworkTable.SetIPAddress(Settings["IP"]);
				NetworkTable.Initialize();
				ntinit = true;
			}
		}

		public class NTValueWrapper
		{
			string path;
			static Dictionary<string, NTValueWrapper> remotes = new();

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

			public event EventHandler? ChangeListener;

			public Value value
			{
				get
				{
					return NtCore.GetEntryValue(path);
				}
				set
				{
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
			NTValueWrapper remote;
			Color truecolor = Color.Transparent;
			Color falsecolor = Color.Transparent;

			bool toggle = false;
			bool state = false;


			public NTBooleanButton()
			{
			}

			public override Task Init(CancellationToken cancellationToken)
			{
				InitNT();

				if (Settings?.ContainsKey("TrueColor") ?? false)
					Color.TryParse(Settings["TrueColor"], out truecolor);
				if (Settings?.ContainsKey("FalseColor") ?? false)
					Color.TryParse(Settings["FalseColor"], out falsecolor);

				toggle = (Settings?.GetValueOrDefault("Toggle", "") ?? "").ToLower() == "true";

				remote = NTValueWrapper.GetWrapper(Settings?["NTPath"] ?? nameof(NTBooleanButton));
				remote.ChangeListener += ChangeListener;

				if (Settings?.ContainsKey("Initial") ?? false)
					remote.value = Value.MakeBoolean(bool.Parse(Settings["Initial"]));

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
				BackgroundColor = state ? truecolor : falsecolor;
			}

			public override Task OnTilePressDown(CancellationToken cancellationToken)
			{
				if (toggle) state = !state;
				else
				{
					state = true;
				}
				remote.value = Value.MakeBoolean(state);
				return base.OnTilePressDown(cancellationToken);
			}

			public override Task OnTilePressUp(CancellationToken cancellationToken)
			{
				if (!toggle)
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
			NTValueWrapper remote;
			Color truecolor = Color.Green;
			Color falsecolor = Color.Red;

			Image trueimage = new Image<Rgba32>(10, 10);
			Image? falseimage = new Image<Rgba32>(10, 10);


			public NTBooleanIndicator()
			{
			}


			public override Task Init(CancellationToken cancellationToken)
			{
				InitNT();

				if (Settings?.ContainsKey("TrueColor") ?? false)
					Color.TryParse(Settings["TrueColor"], out truecolor);
				if (Settings?.ContainsKey("FalseColor") ?? false)
					Color.TryParse(Settings["FalseColor"], out falsecolor);

				if (Settings?.ContainsKey("TrueIcon") ?? false && File.Exists(Settings["TrueIcon"]))
					trueimage = Image.Load(Settings["TrueIcon"]);
				if (Settings?.ContainsKey("FalseIcon") ?? false && File.Exists(Settings["FalseIcon"]))
					falseimage = Image.Load(Settings["FalseIcon"]);


				remote = NTValueWrapper.GetWrapper(Settings?["NTPath"] ?? nameof(NTBooleanButton));
				remote.ChangeListener += ChangeListener;

				SetIconState(remote is not null && remote.value is not null && remote.value.IsBoolean() && remote.value.GetBoolean());
				return base.Init(cancellationToken);
			}

			private void SetIconState(bool state)
			{
				BackgroundColor = state ? truecolor : falsecolor;

				Text = (state ? Settings?.GetValueOrDefault("TrueText", "") : Settings?.GetValueOrDefault("FalseText", "")) ?? "";

				Image = state ? trueimage : falseimage;
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
			private NTValueWrapper remote;
			[Setting] public string NTPath { get; set; } = nameof(NTValueSetter);

			Color selectedBg = Color.Green;
			Color normalBg = Color.Transparent;

			NtType type;
			Value thisbuttonvalue = new Value();

			public NTValueSetter()
			{
			}


			public override async Task Init(CancellationToken cancellationToken)
			{
				InitNT();

				if (Settings?.ContainsKey("TrueColor") ?? false)
					Color.TryParse(Settings["TrueColor"], out selectedBg);
				if (Settings?.ContainsKey("FalseColor") ?? false)
					Color.TryParse(Settings["FalseColor"], out normalBg);

				remote = NTValueWrapper.GetWrapper(NTPath);
				remote.ChangeListener += ChangeListener;

				ParseValue(Settings?["Value"]);

				// TODO: Find a better way to get an initial value!
				await Task.Delay(800);

				SetIconState();

				await base.Init(cancellationToken);
			}

			private void ParseValue(string? value)
			{
				if (value is null) return;
				string[] parts = value.Split(':');
				if (parts.Length != 2) return;
				type = ParseNTType(parts[0]);
				thisbuttonvalue = ParseNTValue(type, parts[1]) ?? new Value();
			}

			private void SetIconState()
			{
				bool state = CompareNTValue(remote.value, thisbuttonvalue);
				BackgroundColor = state ? selectedBg : normalBg;
			}

			private void ChangeListener(object? sender, EventArgs e)
			{
				SetIconState();
			}

			public override Task OnTilePressDown(CancellationToken cancellationToken)
			{
				remote.value = thisbuttonvalue;
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
			private NTValueWrapper remote;
			[Setting] public string NTPath { get; set; } = nameof(NTNumberChanger);

			[Setting] public double? Increment { get; set; }
			[Setting] public double? Min { get; set; }
			[Setting] public double? Max { get; set; }
			[Setting] public double? Initial { get; set; }

			private Color? _maxColor;
			private Color? _minColor;
			private Color _defaultBg;

			[Setting]
			public string? MaxColor
			{
				set
				{
					_maxColor = Color.Parse(value);
				}
			}
			[Setting]
			public string? MinColor
			{
				set
				{
					_minColor = Color.Parse(value);
				}
			}

			public NTNumberChanger()
			{
			}


			public override Task Init(CancellationToken cancellationToken)
			{
				InitNT();

				Min = Min ?? 0;
				Max = Max ?? 255;
				Increment = Increment ?? 1;
				_defaultBg = BackgroundColor ?? Color.Transparent;

				remote = NTValueWrapper.GetWrapper(NTPath);
				remote.ChangeListener += ChangeListener;

				if (Initial is not null)
					remote.value = Value.MakeDouble(Initial.Value);

				return base.Init(cancellationToken);
			}

			private void SetIconState()
			{
				if (remote.value.GetDouble() <= Min && _minColor is not null)
				{
					BackgroundColor = _minColor;
				}
				else if (remote.value.GetDouble() >= Max && _maxColor is not null)
				{
					BackgroundColor = _maxColor;
				}
				else
				{
					BackgroundColor = _defaultBg;
				}
			}

			private void ChangeListener(object? sender, EventArgs e)
			{
				SetIconState();
			}

			public override Task OnTilePressDown(CancellationToken cancellationToken)
			{
				remote.value = Value.MakeDouble(Math.Clamp(remote.value.GetDouble() + Increment.Value, Min.Value, Max.Value));
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
	}
}