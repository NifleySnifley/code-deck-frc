using CodeDeck.PluginAbstractions;
using System;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using NetworkTables;
using System.Collections.Generic;
using NetworkTables.Tables;
using System.Linq;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;


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
			public NTValueWrapper(string path)
			{
				this.path = path;
				NtCore.AddEntryListener(path, (uid, key, value, flags) =>
			{
				ChangeListener?.Invoke(this, EventArgs.Empty);
			}, NotifyFlags.NotifyNew | NotifyFlags.NotifyUpdate | NotifyFlags.NotifyLocal);
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

				remote = new NTValueWrapper(Settings?["NTPath"] ?? nameof(NTBooleanButton));
				remote.ChangeListener += ChangeListener;

				if (Settings?.ContainsKey("Initial") ?? false)
					remote.value = Value.MakeBoolean(bool.Parse(Settings["Initial"]));

				ChangeListener(this, EventArgs.Empty);

				Update();

				return base.Init(cancellationToken);
			}

			private void ChangeListener(object? sender, EventArgs e)
			{
				state = remote is not null && remote.value is not null && remote.value.IsBoolean() && remote.value.GetBoolean();
				Update();
			}

			private void Update()
			{
				BackgroundColor = (remote is not null && remote.value is not null && remote.value.IsBoolean() && remote.value.GetBoolean()) ? truecolor : falsecolor;
			}

			public override Task OnTilePressDown(CancellationToken cancellationToken)
			{
				if (toggle) state = !state;
				else
				{
					state = true;
				}
				remote.value = Value.MakeBoolean(state);
				Update();
				return base.OnTilePressDown(cancellationToken);
			}

			public override Task OnTilePressUp(CancellationToken cancellationToken)
			{
				if (!toggle)
				{
					state = false;
					remote.value = Value.MakeBoolean(state);
					Update();
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


				remote = new NTValueWrapper(Settings?["NTPath"] ?? nameof(NTBooleanButton));
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

		// class NTRadioButton : Tile
		// {

		// }
	}
}