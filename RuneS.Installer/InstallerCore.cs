using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32;

namespace RuneS.Installer
{
    /// <summary>
    /// Handles all file system, registry, and shortcut operations for install/uninstall.
    /// </summary>
    public static class InstallerCore
    {
        public const string AppName    = "RuneS";
        public const string AppVersion = "1.0.0";
        public const string Publisher  = "RuneS Project";
        public const string ExeName    = "RuneS.exe";

        private const string RegUninstallRoot =
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        private const string RegKey = RegUninstallRoot + @"\RuneS";

        // ── Admin check ──────────────────────────────────────────────────────
        public static bool IsAdmin()
        {
            using (var id = WindowsIdentity.GetCurrent())
                return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
        }

        // ── Source payload ───────────────────────────────────────────────────
        /// <summary>
        /// Returns the folder that contains RuneS.exe to be installed.
        /// Looks for a "Payload" subfolder next to the installer exe,
        /// then falls back to the installer's own directory.
        /// </summary>
        public static string GetPayloadFolder()
        {
            var exeDir  = AppDomain.CurrentDomain.BaseDirectory;
            var payload = Path.Combine(exeDir, "Payload");
            return Directory.Exists(payload) ? payload : exeDir;
        }

        // ── Install ──────────────────────────────────────────────────────────
        public static void Install(string installDir,
                                   bool   createDesktopShortcut,
                                   bool   createStartMenuShortcut,
                                   IProgress<(int pct, string msg)> progress)
        {
            // 1. Create install directory
            progress.Report((5, "Creating installation folder..."));
            Directory.CreateDirectory(installDir);

            // 2. Copy payload files
            var payload = GetPayloadFolder();
            var files   = CollectFiles(payload);
            int total   = files.Count;
            int done    = 0;

            foreach (var (src, rel) in files)
            {
                var dst = Path.Combine(installDir, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(dst));
                File.Copy(src, dst, overwrite: true);
                done++;
                int pct = 5 + (int)(done / (double)total * 65);
                progress.Report((pct, "Copying: " + rel));
            }

            // 3. Shortcuts
            progress.Report((72, "Creating shortcuts..."));
            var exePath = Path.Combine(installDir, ExeName);

            if (createDesktopShortcut)
            {
                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                CreateShortcut(Path.Combine(desktop, AppName + ".lnk"), exePath, installDir);
            }

            if (createStartMenuShortcut)
            {
                var startMenu = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
                    AppName);
                Directory.CreateDirectory(startMenu);
                CreateShortcut(Path.Combine(startMenu, AppName + ".lnk"), exePath, installDir);
            }

            // 4. Register uninstaller
            progress.Report((88, "Registering uninstaller..."));
            RegisterUninstaller(installDir, exePath);

            // 5. Register as default browser (optional, user-initiated only)
            // Skipped – requires RegisterApplication manifest entries.

            progress.Report((100, "Installation complete."));
        }

        // ── Uninstall ────────────────────────────────────────────────────────
        public static void Uninstall(string installDir,
                                     IProgress<(int pct, string msg)> progress)
        {
            progress.Report((5, "Removing shortcuts..."));

            // Desktop shortcut
            var desktopLnk = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                AppName + ".lnk");
            if (File.Exists(desktopLnk)) File.Delete(desktopLnk);

            // Start Menu shortcut
            var startMenu = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
                AppName);
            if (Directory.Exists(startMenu))
                Directory.Delete(startMenu, recursive: true);

            progress.Report((30, "Removing files..."));

            if (Directory.Exists(installDir))
                Directory.Delete(installDir, recursive: true);

            progress.Report((80, "Removing registry entries..."));
            RemoveUninstaller();

            // Remove user data (optional – ask first in real scenario)
            var userData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RuneS");
            if (Directory.Exists(userData))
            {
                progress.Report((90, "Removing user data..."));
                Directory.Delete(userData, recursive: true);
            }

            progress.Report((100, "Uninstallation complete."));
        }

        // ── Registry ─────────────────────────────────────────────────────────
        private static void RegisterUninstaller(string installDir, string exePath)
        {
            using (var key = Registry.LocalMachine.CreateSubKey(RegKey))
            {
                if (key == null) return;
                key.SetValue("DisplayName",          AppName);
                key.SetValue("DisplayVersion",       AppVersion);
                key.SetValue("Publisher",            Publisher);
                key.SetValue("InstallLocation",      installDir);
                key.SetValue("DisplayIcon",          exePath + ",0");
                key.SetValue("UninstallString",
                    $"\"{exePath}\" --uninstall");
                key.SetValue("NoModify",  1, RegistryValueKind.DWord);
                key.SetValue("NoRepair",  1, RegistryValueKind.DWord);
                key.SetValue("EstimatedSize",
                    GetDirSizeKB(installDir), RegistryValueKind.DWord);
            }
        }

        private static void RemoveUninstaller()
        {
            try { Registry.LocalMachine.DeleteSubKeyTree(RegKey, throwOnMissingSubKey: false); }
            catch { }
        }

        public static string GetInstalledPath()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(RegKey))
                    return key?.GetValue("InstallLocation") as string;
            }
            catch { return null; }
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private static List<(string src, string rel)> CollectFiles(string root)
        {
            var list = new List<(string, string)>();
            if (!Directory.Exists(root)) return list;

            foreach (var f in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                var rel = f.Substring(root.Length).TrimStart('\\', '/');
                list.Add((f, rel));
            }
            return list;
        }

        private static int GetDirSizeKB(string dir)
        {
            if (!Directory.Exists(dir)) return 0;
            long bytes = 0;
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                try { bytes += new FileInfo(f).Length; } catch { }
            return (int)(bytes / 1024);
        }

        // ── Shell shortcut via COM ────────────────────────────────────────────
        [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLink { }

        [ComImport, Guid("000214F9-0000-0000-C000-000000000046"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellLink
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszFile,
                         int cch, IntPtr pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszName, int cch);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszDir, int cch);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszArgs, int cch);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszIconPath,
                                 int cch, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
            void Resolve(IntPtr hwnd, uint fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport, Guid("0000010B-0000-0000-C000-000000000046"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPersistFile
        {
            void GetClassID(out Guid pClassID);
            [PreserveSig] int IsDirty();
            void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
            void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName,
                      [MarshalAs(UnmanagedType.Bool)] bool fRemember);
            void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
            void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
        }

        private static void CreateShortcut(string lnkPath, string targetPath, string workDir)
        {
            var link = (IShellLink)new ShellLink();
            link.SetPath(targetPath);
            link.SetWorkingDirectory(workDir);
            link.SetDescription(AppName + " Browser");
            link.SetIconLocation(targetPath, 0);
            ((IPersistFile)link).Save(lnkPath, false);
        }
    }
}
