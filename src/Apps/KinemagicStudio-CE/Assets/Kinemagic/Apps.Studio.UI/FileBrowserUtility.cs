using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Kinemagic.Apps.Studio.UI
{
    public static class FileBrowserUtility
    {
        // Windows
        [DllImport("shell32.dll", SetLastError = true)]
        private static extern int ShellExecute(IntPtr hwnd, string lpOperation, string lpFile, string lpParameters, string lpDirectory, int nShowCmd);
        private const int SW_SHOW = 5;

        // macOS
        [DllImport("libc")]
        private static extern int system(string command);

        public static bool IsWindows => Application.platform == RuntimePlatform.WindowsPlayer || 
                                        Application.platform == RuntimePlatform.WindowsEditor;
        public static bool IsMacOS => Application.platform == RuntimePlatform.OSXPlayer || 
                                       Application.platform == RuntimePlatform.OSXEditor;

        /// <summary>
        /// Opens the system file manager and reveals the specified path.
        /// Works on Windows (Explorer), macOS (Finder).
        /// </summary>
        public static void OpenDirectory(string directoryPath)
        {
            if (IsWindows)
            {
                OpenInWindows(directoryPath);
            }
            else if (IsMacOS)
            {
                OpenInMacOS(directoryPath);
            }
            else
            {
                Debug.LogError($"Unsupported platform for opening file browser");
            }
        }

        private static void OpenInWindows(string directoryPath)
        {
            var winPath = directoryPath.Replace("/", "\\");
            if (!Directory.Exists(winPath))
            {
                Debug.LogError($"Directory does not exist: {winPath}");
                return;
            }

            try
            {
                // NOTE: IL2CPP doesn’t support the System.Diagnostics.Process API methods.
                var result = ShellExecute(IntPtr.Zero, "open", winPath, null, null, SW_SHOW);
                if (result <= 32)
                {
                    Debug.LogError($"Failed to open directory in Windows Explorer. Error code: {result}");
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static void OpenInMacOS(string directoryPath)
        {
            var macPath = directoryPath.Replace("\\", "/");
            if (!Directory.Exists(macPath))
            {
                Debug.LogError($"Directory does not exist: {macPath}");
                return;
            }

            try
            {
                // NOTE: IL2CPP doesn’t support the System.Diagnostics.Process API methods.
                var quotedPath = macPath.Contains(" ") ? $"\"{macPath}\"" : macPath;
                var result = system($"open {quotedPath}");
                if (result != 0)
                {
                    Debug.LogError($"Failed to open directory in Finder. Error code: {result}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}