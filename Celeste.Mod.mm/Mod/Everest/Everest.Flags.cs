using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Celeste.Mod {
    public static partial class Everest {
        public static class Flags {
            /// <summary>
            /// Is the game running on XNA - always false on .NET Core Everest.
            /// </summary>
            [Obsolete("`IsXNA` is always false on Everest Core")]
            public static bool IsXNA => false;

            /// <summary>
            /// Is the game running on FNA - always true on .NET Core Everest.
            /// </summary>
            [Obsolete("`IsFNA` is always true on Everest Core")]
            public static bool IsFNA => true;

            /// <summary>
            /// Is the vanilla install running on XNA?
            /// </summary>
            public static bool VanillaIsXNA { get; private set; }

            /// <summary>
            /// Is the vanilla install running on FNA?
            /// </summary>
            public static bool VanillaIsFNA { get; private set; }

            /// <summary>
            /// Is Everest running without a window?
            /// </summary>
            public static bool IsHeadless { get; internal set; }

            /// <summary>
            /// Whether the environment (platform, ...) can launch external processes.  false on libnx,
            /// which has no process creation API: the splash screen, the self-updater, game
            /// restart/relaunch and "open in an external program" helpers are unavailable there and
            /// each of them is skipped with a log instead of failing inside System.Diagnostics.Process.
            /// </summary>
            internal static bool SupportsExternalProcesses =>
                MonoMod.Utils.PlatformDetection.OS != MonoMod.Utils.OSKind.Libnx;

            /// <summary>
            /// Whether the Discord Game SDK can be loaded.  It is a native library (and needs a live
            /// Discord client plus network) that the libnx payload does not ship, so the target skips
            /// the attempt instead of provoking a native-library miss and a detailed exception dump
            /// during boot.  Desktop platforms keep it; the web port has no Discord support at all.
            /// </summary>
            internal static bool SupportsDiscordSDK =>
                MonoMod.Utils.PlatformDetection.OS != MonoMod.Utils.OSKind.Libnx;

            /// <summary>
            /// Whether FMOD Studio Live Update can be requested.  It needs the Live Update runtime
            /// (fmodL / fmodstudioL) plus a TCP connection to an FMOD Studio editor; the libnx payload
            /// talks to FMOD through the fauxmod bridge, which links the release runtime only.  The
            /// menu option is therefore hidden there and a stale setting can never set
            /// INITFLAGS.LIVEUPDATE.
            /// </summary>
            internal static bool SupportsFMODLiveUpdate =>
                MonoMod.Utils.PlatformDetection.OS != MonoMod.Utils.OSKind.Libnx;

            /// <summary>
            /// Whether FNA3D's exclusive fullscreen switch can do anything.  It is a D3D11 (Windows)
            /// renderer knob; on libnx the game renders through the OpenGL ES backend, so the option
            /// would only set an environment variable that nothing reads.
            /// </summary>
            internal static bool SupportsExclusiveFullscreen =>
                MonoMod.Utils.PlatformDetection.OS != MonoMod.Utils.OSKind.Libnx;

            /// <summary>
            /// Whether <see cref="FileSystemWatcher"/> can be constructed.  The libnx runtime ships no
            /// inotify implementation: constructing a watcher throws IOException("Not supported"), so
            /// watcher-backed features (mod hot-reload, auto-loading new mod containers, code reload)
            /// are disabled there instead of throwing once per watched folder.
            /// </summary>
            internal static bool SupportsFileSystemWatchers =>
                MonoMod.Utils.PlatformDetection.OS != MonoMod.Utils.OSKind.Libnx;

            private static readonly HashSet<string> _loggedFileWatcherUnsupported = new HashSet<string>();

            /// <summary>
            /// Report, once per feature tag, that file watching is unavailable on this platform.  Used
            /// instead of letting the unsupported FileSystemWatcher construction throw per folder.
            /// </summary>
            internal static void LogFileSystemWatchersUnavailable(string tag, string feature) {
                lock (_loggedFileWatcherUnsupported) {
                    if (!_loggedFileWatcherUnsupported.Add(tag))
                        return;
                }

                Logger.Log(LogLevel.Warn, tag,
                    $"{feature} is unavailable on {MonoMod.Utils.PlatformDetection.OS}: the runtime cannot watch files, so changes are not detected");
            }

            /// <summary>
            /// Is the game running using Mono - always false on .NET Core Everest.
            /// </summary>
            [Obsolete("`IsMono` is always false on Everest Core")]
            public static bool IsMono => false;

            /// <summary>
            /// Should the game avoid creating render targets if possible?
            /// </summary>
            [Obsolete("`AvoidRenderTargets` is always false on Everest Core")]
            public static bool AvoidRenderTargets => false;

            /// <summary>
            /// Does the environment (platform, ...) prefer lazy loading?
            /// </summary>
            [Obsolete("`PreferLazyLoading` is always false on Everest Core")]
            public static bool PreferLazyLoading => false;

            /// <summary>
            /// Does the environment (renderer, framework ,...) prefer threaded GL?
            /// </summary>
            [Obsolete("`PreferThreadedGL` is always false on Everest Core")]
            public static bool PreferThreadedGL => false;

            /// <summary>
            /// Does the environment (platform, ...) support loading runtime mods?
            /// </summary>
            [Obsolete("`SupportRuntimeMods` is always true on Everest Core")]
            public static bool SupportRuntimeMods => true;

            /// <summary>
            /// Does the environment (platform, ...) support updating Everest?
            /// </summary>
            [Obsolete("`SupportUpdatingEverest` is always true on Everest Core")]
            public static bool SupportUpdatingEverest => true;

            internal static void Initialize() {
                // Determine vanilla install type
                string vanillaExe = Path.Combine(PathGame, "orig", "Celeste.exe");
                if (File.Exists(vanillaExe)) {
                    using FileStream stream = File.OpenRead(vanillaExe);
                    // PrefetchEntireImage makes PEReader read the image into memory instead of
                    // memory-mapping the FileStream: file-backed mappings do not exist where the OS has
                    // no file mmap (libnx).  Documented reader behaviour, identical on every platform.
                    using PEReader peReader = new PEReader(stream, PEStreamOptions.PrefetchEntireImage);
                    MetadataReader metaReader = peReader.GetMetadataReader();

                    VanillaIsFNA = metaReader.AssemblyReferences.Any(handle => metaReader.GetString(metaReader.GetAssemblyReference(handle).Name) == "FNA");
                    VanillaIsXNA = !VanillaIsFNA;
                }
            }
        }
    }
}
