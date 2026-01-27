using Assets.Behaviour.UI;
using Assets.Source.Player;
using MelonLoader;
using Widgitpelago.Archipelago;

[assembly: MelonInfo(typeof(Widgitpelago.Core), "Widgitpelago", "0.1.0", "SW_CreeperKing", null)]
[assembly: MelonGame("Leaping Turtle", "WidgetInc")]

namespace Widgitpelago
{
	public class Core : MelonMod
	{
		public static MelonLogger.Instance Log;
		
		public override void OnInitializeMelon()
		{
			Log = LoggerInstance;
			
			Log.Msg("Running Shenanigans");
			
			ApShenanigans.RunShenanigans();
			
			Log.Msg("Shenanigans ran");
			
			var classesToPatch = MelonAssembly.Assembly.GetTypes()
			                             .Where(t => t.GetCustomAttributes(typeof(PatchAllAttribute), false).Any())
			                             .ToArray();

			Log.Msg($"Loading [{classesToPatch.Length}] Class patches");

			foreach (var patch in classesToPatch)
			{
				HarmonyInstance.PatchAll(patch);

				Log.Msg($"Loaded: [{patch.Name}]");
			}
			
			LoggerInstance.Msg("Initialized.");
		}
	}
}

[AttributeUsage(AttributeTargets.Class)]
public class PatchAllAttribute : Attribute;