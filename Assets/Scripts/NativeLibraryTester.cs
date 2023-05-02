using System;
using System.Runtime.InteropServices;

using UnityEngine;

namespace Kew
{
    /// <summary>
    ///   <see cref="DllNotFoundException" />の原因のデバッグ用のコード。
    ///   ライブラリをロードし、そのエラーコードやエラーメッセージを読む。
    /// </summary>
    /// <remarks>
    ///   Linux, macOS, Windows, Android, iOSのみを考慮。
    ///   ただし、iOSは静的リンクしている想定なので、何もしない。
    /// </remarks>
    public class NativeLibraryTester
    {
        /// <summary>
        ///   ライブラリをロードする。
        /// </summary>
        /// <remarks>
        ///   Androidの場合は、通常 <see cref="LoadLibrary" /> を使うほうが良い。
        /// </remarks>
        /// <param name="path">
        ///   ライブラリのパス
        ///     UnityEditorの場合: プロジェクトのルートからの相対パスでOK
        ///     Standaloneの場合: `{Application.dataPath}/Plugins/*.{so,dylib,dll}`
        ///     Androidの場合: 絶対パス
        /// </param>
        public static void Load(string path)
        {
#if UNITY_IOS && !UNITY_EDITOR
    // iOS
    // 静的リンクを想定してスルー
#elif UNITY_ANDROID && !UNITY_EDITOR
    // Android
    // NOTE: Linux, macOSと同じくdlopenしても良いけど
    using (var system = new AndroidJavaClass("java.lang.System"))
    {
      system.CallStatic("load", path);
    }
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    // Windows
    // ただし、まともなエラーメッセージは出ない
    var handle = LoadLibraryW(path);

    if (handle != IntPtr.Zero)
    {
      // Success
      if (!FreeLibrary(handle))
      {
        Debug.LogError($"Failed to unload {path}: {Marshal.GetLastWin32Error()}");
      }
    }
    else
    {
      // Error
      // cf. https://docs.microsoft.com/en-us/windows/win32/debug/system-error-codes--0-499-
      var errorCode = Marshal.GetLastWin32Error();
      Debug.LogError($"Failed to load {path}: {errorCode}");

      if (errorCode == 126)
      {
        // ERROR_MOD_NOT_FOUND
        Debug.LogError(@"Check missing dependencies using [Dependencies](https://github.com/lucasg/Dependencies).
If all the required libraries exist, open the plugin inspector for dependent libraries and check `Load on startup`.
");
      }
    }
#else
            // Linux, macOS
            var handle = dlopen(path, 2);

            if (handle != IntPtr.Zero)
            {
                var result = dlclose(handle);

                if (result != 0)
                {
                    Debug.LogError($"Failed to unload {path}");
                }
            }
            else
            {
                Debug.LogError($"Failed to load {path}: {Marshal.GetLastWin32Error()}");
                var error = Marshal.PtrToStringAnsi(dlerror());
                // TODO: release memory

                if (error != null)
                {
                    Debug.LogError(error);
                }
            }
#endif
        }

        /// <summary>
        ///   ライブラリをロードする。
        /// </summary>
        /// <param name="name">
        ///   ライブラリ名 (e.g. libfoo.so -> foo)
        /// </param>
        public static void LoadLibrary(string name)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
    using (var system = new AndroidJavaClass("java.lang.System"))
    {
      system.CallStatic("loadLibrary", name);
    }
#else
            throw new NotSupportedException("Androidのみ対応");
#endif
        }

        [DllImport("dl", SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr dlopen(string name, int flags);

        [DllImport("dl", ExactSpelling = true)]
        private static extern IntPtr dlerror();

        [DllImport("dl", ExactSpelling = true)]
        private static extern int dlclose(IntPtr handle);

        [DllImport("kernel32", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibraryW(string path);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr handle);
    }
}