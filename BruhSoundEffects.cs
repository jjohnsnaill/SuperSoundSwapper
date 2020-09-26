using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using MonoMod.Cil;
using MP3Sharp;
using NVorbis;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Audio;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;
using static Mono.Cecil.Cil.OpCodes;

namespace BruhSoundEffects
{
	public class BruhSoundEffects : Mod
	{
		private static List<SoundEffect> sounds;

		public override void Load()
		{
			sounds = new List<SoundEffect>();

			List<string> paths = ModContent.GetInstance<Config>().paths;
			for (int i = 0; i < paths.Count; i++)
			{
				if (File.Exists(paths[i]))
				{
					using (MemoryStream memoryStream = new MemoryStream(File.ReadAllBytes(paths[i])))
					{
						switch (Path.GetExtension(paths[i]).ToLower())
						{
							case ".wav":
								if (!memoryStream.CanSeek)
								{
									sounds.Add(SoundEffect.FromStream(new MemoryStream(memoryStream.ReadBytes(memoryStream.Length))));
								}
								else
								{
									sounds.Add(SoundEffect.FromStream(memoryStream));
								}
								break;
							case ".mp3":
								using (var mp3 = new MP3Stream(memoryStream))
								using (var stream = new MemoryStream())
								{
									mp3.CopyTo(stream);
									sounds.Add(new SoundEffect(stream.ToArray(), mp3.Frequency, (AudioChannels)mp3.ChannelCount));
								}
								break;
							case ".ogg":
								using (var reader = new VorbisReader(memoryStream, true))
								{
									var buffer = new byte[reader.TotalSamples * 2 * reader.Channels];
									var floatBuf = new float[buffer.Length / 2];
									reader.ReadSamples(floatBuf, 0, floatBuf.Length);
									MusicStreamingOGG.Convert(floatBuf, buffer);
									sounds.Add(new SoundEffect(buffer, reader.SampleRate, (AudioChannels)reader.Channels));
								}
								break;
							default:
								sounds.Add(ModContent.GetSound("BruhSoundEffects/BruhSoundEffect"));
								break;
						}
					}
				}
				else
				{
					sounds.Add(ModContent.GetSound("BruhSoundEffects/BruhSoundEffect"));
				}
			}
			if (sounds.Count < 1)
			{
				sounds.Add(ModContent.GetSound("BruhSoundEffects/BruhSoundEffect"));
			}

			IL.Terraria.Main.PlaySound_int_int_int_int_float_float += il =>
			{
				var c = new ILCursor(il);

				if (!c.TryGotoNext(i => i.MatchCall(typeof(Main), "PlaySoundInstance")))
				{
					return;
				}
				c.Index--;

				c.Emit(Ldloca, 10);
				c.Emit(Mono.Cecil.Cil.OpCodes.Call, typeof(BruhSoundEffects).GetMethod("Swap"));
			};
		}

		public static void Swap(ref SoundEffectInstance original)
		{
			Config config = ModContent.GetInstance<Config>();

			if (config.chance <= 0 || (config.chance < 1 && Main.rand.NextFloat() >= config.chance))
				return;

			SoundEffectInstance instance;
			if (sounds.Count > 1)
			{
				instance = sounds[Main.rand.Next(sounds.Count)].CreateInstance();
			}
			else
			{
				instance = sounds[0].CreateInstance();
			}

			if (config.retainPitch)
				instance.Pitch = original.Pitch;
			if (config.retainVolume)
				instance.Volume = original.Volume;
			if (config.retainPan)
				instance.Pan = original.Pan;

			if (config.minPitch != 0 || config.maxPitch != 0)
			{
				float pitch = instance.Pitch;
				if (config.maxPitch > config.minPitch)
				{
					pitch += Main.rand.NextFloat(config.minPitch, config.maxPitch);
				}
				else
				{
					pitch += config.minPitch;
				}
				instance.Pitch = MathHelper.Clamp(pitch, -1f, 1f);
			}
			if (config.minVolume != 1)
			{
				float volume = instance.Volume;
				if (config.maxVolume > config.minVolume)
				{
					volume *= Main.rand.NextFloat(config.minVolume, config.maxVolume);
				}
				else
				{
					volume *= config.minVolume;
				}
				instance.Volume = MathHelper.Clamp(volume, 0f, 1f);
			}
			if (config.minPan != 0 || config.maxPan != 0)
			{
				float pan = instance.Pan;
				if (config.maxPan > config.minPan)
				{
					pan += Main.rand.NextFloat(config.minPan, config.maxPan);
				}
				else
				{
					pan += config.minPan;
				}
				instance.Pan = MathHelper.Clamp(pan, -1f, 1f);
			}

			original = instance;
		}
	}

	public class Config : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ClientSide;

		[Label("Sound Paths")]
		[Tooltip("Bruh Sound Effect #2 is loaded for every invalid path or sound")]
		public List<string> paths { get; set; } = new List<string>() { Main.SavePath + "\\Sound.wav" };

		[DefaultValue(1f)]
		[Range(0f, 1f)]
		[Label("Swap Chance")]
		public float chance;

		[DefaultValue(true)]
		[Label("Retain Pitch")]
		public bool retainPitch;

		[DefaultValue(true)]
		[Label("Retain Volume")]
		public bool retainVolume;

		[DefaultValue(true)]
		[Label("Retain Pan")]
		public bool retainPan;

		[Range(-1f, 1f)]
		[Label("Minimum Pitch")]
		public float minPitch;

		[Range(-1f, 1f)]
		[Label("Maximum Pitch")]
		public float maxPitch;

		[Range(0f, 1f)]
		[Label("Minimum Volume")]
		public float minVolume;

		[Range(0f, 1f)]
		[Label("Maximum Volume")]
		public float maxVolume;

		[Range(-1f, 1f)]
		[Label("Minimum Pan")]
		public float minPan;

		[Range(-1f, 1f)]
		[Label("Maximum Pan")]
		public float maxPan;
	}
}