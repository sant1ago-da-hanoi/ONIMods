﻿/*
 * Copyright 2022 Peter Han
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software
 * and associated documentation files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or
 * substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
 * BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using HarmonyLib;
using KMod;
using PeterHan.PLib.AVC;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using System;
using System.Collections.Generic;
using System.Threading;

namespace PeterHan.FastTrack {
	/// <summary>
	/// Patches which will be applied via annotations for Fast Track.
	/// </summary>
	public sealed class FastTrackMod : KMod.UserMod2 {
		// Global, Game, World
		// Game#LateUpdate is ~50ms/1000ms
		// Game#Update is ~300ms
		// Global#Update is ~100ms
		// World#LateUpdate is ~100ms
		// Global#LateUpdate is ~30ms
		// Pathfinding#UpdateNavGrids is <20ms
		// StatusItemRenderer#RenderEveryTick could use some work but is only ~10ms
		//  (need to excise GetComponent calls which is a massive transpiler)
		// ElectricalUtilityNetwork#Update is ~10ms
		// KBatchedAnimUpdater#LateUpdate is ~50ms
		// AnimEventManager#Update is 20ms but not much can be done
		// KBatchedAnimUpdater#UpdateRegisteredAnims is 40ms
		// ConduitFlow.Sim200ms is <10ms
		// ChoreConsumer.FindNextChore is <10ms
		// None of the RenderImage methods are more than 1ms
#if DEBUG
		[PLibMethod(RunAt.AfterModsLoad)]
		internal static void Profile(Harmony harmony) {
			harmony.Profile(typeof(KAnimBatchManager), nameof(KAnimBatchManager.UpdateDirty));
			harmony.Profile(typeof(KBatchedAnimUpdater), nameof(KBatchedAnimUpdater.UpdateRegisteredAnims));
			harmony.Profile(typeof(VisualPatches.KBatchedAnimController_UpdateAnim_Patch), "UpdateActive");
		}
#endif

		/// <summary>
		/// The maximum time that any of the blocking joins will wait, in real time ms.
		/// </summary>
		public const int MAX_TIMEOUT = 5000;

		/// <summary>
		/// Set to true when the game gets off its feet, and false while it is still loading.
		/// </summary>
		internal static bool GameRunning { get; private set; }

		/// <summary>
		/// The handle that is signaled when worldgen loading completes.
		/// </summary>
		private static readonly EventWaitHandle onWorldGenLoad = new AutoResetEvent(false);

		/// <summary>
		/// Initializes several patches after Db is initialized.
		/// </summary>
		[PLibMethod(RunAt.AfterDbInit)]
		internal static void AfterDbInit() {
			var options = FastTrackOptions.Instance;
			if (options.ThreatOvercrowding)
				CritterPatches.OvercrowdingMonitor_UpdateState_Patch.InitTagBits();
			if (options.SensorOpts) {
				SensorPatches.SensorPatches.Init();
				SensorPatches.SensorPatches.MingleCellSensor_Update.Init();
			}
			if (options.AnimOpts)
				VisualPatches.KAnimLoopOptimizer.CreateInstance();
		}

		/// <summary>
		/// Fixes the drag order spam bug, if Stock Bug Fix did not get to it first.
		/// </summary>
		internal static void FixTimeLapseDrag() {
			PlayerController.Instance?.CancelDragging();
		}

		/// <summary>
		/// Loads worldgen data in the background while other parts of the game load.
		/// </summary>
		private static void LoadWorldGenInBackground() {
			try {
				ProcGenGame.WorldGen.LoadSettings();
			} catch (Exception e) {
				PUtil.LogError(e);
			}
			onWorldGenLoad.Set();
		}

		/// <summary>
		/// Cleans up the mod caches after the game ends.
		/// </summary>
		[PLibMethod(RunAt.OnEndGame)]
		internal static void OnEndGame() {
			var options = FastTrackOptions.Instance;
			ConduitPatches.ConduitFlowVisualizerRenderer.Cleanup();
			if (options.CachePaths)
				PathPatches.PathCacher.Cleanup();
			// FastCellChangeMonitor did not help, because pretty much all updates were to
			// things that actually had a listener
			if (options.UnstackLights)
				VisualPatches.LightBufferManager.Cleanup();
			if (options.ReduceTileUpdates)
				VisualPatches.PropertyTextureUpdater.DestroyInstance();
			if (options.ConduitOpts)
				ConduitPatches.BackgroundConduitUpdater.DestroyInstance();
			if (options.ParallelInventory)
				UIPatches.BackgroundInventoryUpdater.DestroyInstance();
			if (options.MeshRendererOptions != FastTrackOptions.MeshRendererSettings.None) {
				VisualPatches.TerrainMeshRenderer.DestroyInstance();
				ConduitPatches.ConduitFlowMeshPatches.CleanupAll();
				VisualPatches.GroundRendererDataPatches.CleanupAll();
			}
			if (options.MeshRendererOptions == FastTrackOptions.MeshRendererSettings.All)
				VisualPatches.TileMeshRenderer.DestroyInstance();
			if (options.FastReachability)
				SensorPatches.FastGroupProber.Cleanup();
			if (options.PickupOpts)
				GamePatches.SolidTransferArmUpdater.DestroyInstance();
			if (options.AsyncPathProbe)
				PathPatches.PathProbeJobManager.DestroyInstance();
			GamePatches.AchievementPatches.DestroyInstance();
			PathPatches.AsyncBrainGroupUpdater.DestroyInstance();
			AsyncJobManager.DestroyInstance();
			GameRunning = false;
		}

		/// <summary>
		/// Starts up some asynchronous tasks after everything is fully loaded.
		/// </summary>
		[PLibMethod(RunAt.AfterLayerableLoad)]
		private static void OnLayerablesLoaded() {
			if (FastTrackOptions.Instance.OptimizeDialogs) {
				var thread = new Thread(LoadWorldGenInBackground) {
					Name = "Load Worldgen Async", IsBackground = true,
					Priority = ThreadPriority.BelowNormal
				};
				Util.ApplyInvariantCultureToThread(thread);
				thread.Start();
			}
		}

		/// <summary>
		/// Waits for worldgen loading to complete for up to 3 seconds.
		/// </summary>
		[PLibMethod(RunAt.InMainMenu)]
		internal static void OnMainMenu() {
			if (FastTrackOptions.Instance.OptimizeDialogs && !onWorldGenLoad.WaitOne(3000))
				PUtil.LogWarning("Worldgen was not loaded within the timeout!");
		}

		/// <summary>
		/// Initializes the nav grids on game start, since Pathfinding.AddNavGrid gets inlined.
		/// </summary>
		[PLibMethod(RunAt.OnStartGame)]
		internal static void OnStartGame() {
			var inst = Game.Instance;
			var options = FastTrackOptions.Instance;
			if (options.CachePaths)
				PathPatches.PathCacher.Init();
			// Slices updates to Duplicant sensors
			if (options.AsyncPathProbe)
				PathPatches.PathProbeJobManager.CreateInstance();
			if (inst != null) {
				var go = inst.gameObject;
				go.AddOrGet<AsyncJobManager>();
				if (options.ReduceSoundUpdates && !options.DisableSound)
					go.AddOrGet<SoundUpdater>();
				if (options.ParallelInventory)
					UIPatches.BackgroundInventoryUpdater.CreateInstance();
				if (options.MiscOpts)
					go.AddOrGet<GamePatches.AchievementPatches>();
				if (options.RadiationOpts)
					go.AddOrGet<GamePatches.SlicedRadiationGridUpdater>();
				// Requires the AJM to work
				if (options.PickupOpts)
					GamePatches.SolidTransferArmUpdater.CreateInstance();
				if (options.ConduitOpts)
					ConduitPatches.BackgroundConduitUpdater.CreateInstance();
				// If debugging is on, start logging
				if (options.Metrics)
					go.AddOrGet<Metrics.DebugMetrics>();
				inst.StartCoroutine(WaitForCleanLoad());
			}
			ConduitPatches.ConduitFlowVisualizerRenderer.Init();
			if (options.UnstackLights)
				VisualPatches.LightBufferManager.Init();
			VisualPatches.FullScreenDialogPatches.Init();
		}

		public override void OnAllModsLoaded(Harmony harmony, IReadOnlyList<Mod> mods) {
			base.OnAllModsLoaded(harmony, mods);
			// Manual patch in the rewritten FetchManager.UpdatePickups only if Efficient
			// Supply is not enabled
			if (FastTrackOptions.Instance.FastUpdatePickups) {
				if (PPatchTools.GetTypeSafe("PeterHan.EfficientFetch.EfficientFetchManager") ==
						null) {
					harmony.Patch(typeof(FetchManager.FetchablesByPrefabId),
						nameof(FetchManager.FetchablesByPrefabId.UpdatePickups),
						prefix: new HarmonyMethod(typeof(GamePatches.FetchManagerFastUpdate),
						nameof(GamePatches.FetchManagerFastUpdate.BeforeUpdatePickups)));
#if DEBUG
					PUtil.LogDebug("Patched FetchManager for fast pickup updates");
#endif
				} else
					PUtil.LogWarning("Disabling fast pickup updates: Efficient Supply active");
			}
			if (!PRegistry.GetData<bool>("Bugs.AutosaveDragFix"))
				// Fix the annoying autosave bug
				harmony.Patch(typeof(Timelapser), "SaveScreenshot", postfix: new HarmonyMethod(
					typeof(FastTrackMod), nameof(FastTrackMod.FixTimeLapseDrag)));
		}

		public override void OnLoad(Harmony harmony) {
			base.OnLoad(harmony);
			var options = FastTrackOptions.Instance;
			onWorldGenLoad.Reset();
			PUtil.InitLibrary();
			new POptions().RegisterOptions(this, typeof(FastTrackOptions));
			new PPatchManager(harmony).RegisterPatchClass(typeof(FastTrackMod));
			new PVersionCheck().Register(this, new SteamVersionChecker());
			// In case this goes in stock bug fix later
			if (options.UnstackLights)
				PRegistry.PutData("Bugs.StackedLights", true);
			PRegistry.PutData("Bugs.AnimFree", true);
			// This patch is Windows only apparently
			var target = typeof(Global).GetMethodSafe(nameof(Global.TestDataLocations), false);
			if (options.MiscOpts && target != null && typeof(Global).GetFieldSafe(
					nameof(Global.saveFolderTestResult), true) != null) {
				harmony.Patch(target, prefix: new HarmonyMethod(typeof(FastTrackMod),
					nameof(RemoveTestDataLocations)));
#if DEBUG
				PUtil.LogDebug("Patched Global.TestDataLocations");
#endif
			} else
				PUtil.LogDebug("Skipping TestDataLocations patch");
			// Another potentially Windows only patch
			target = typeof(Game).Assembly.GetType(nameof(InitializeCheck), false)?.
				GetMethodSafe(nameof(InitializeCheck.CheckForSavePathIssue), false);
			if (options.MiscOpts && target != null) {
				harmony.Patch(target, prefix: new HarmonyMethod(typeof(FastTrackMod),
					nameof(SkipInitCheck)));
#if DEBUG
				PUtil.LogDebug("Patched InitializeCheck.Awake");
#endif
			} else
				PUtil.LogDebug("Skipping InitializeCheck patch");
			GameRunning = false;
		}

		/// <summary>
		/// Disables a time-consuming check on whether the save folders could successfully be
		/// written. Only used in the metrics reports anyways.
		/// </summary>
		internal static bool RemoveTestDataLocations(ref string ___saveFolderTestResult) {
			___saveFolderTestResult = "both";
			return false;
		}

		/// <summary>
		/// Skip a time-consuming file write test on load.
		/// </summary>
		internal static bool SkipInitCheck() {
			return false;
		}

		/// <summary>
		/// Waits a few frames as a coroutine, then allows things that require game stability
		/// to run.
		/// </summary>
		private static System.Collections.IEnumerator WaitForCleanLoad() {
			for (int i = 0; i < 3; i++)
				yield return null;
			GameRunning = true;
			yield break;
		}
	}
}