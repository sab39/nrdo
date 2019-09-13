﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NR.Win7
{
    public static class Win7Functions
    {
        public static void SetTaskBarProgress(this Form window, int current, int total, ProgressBarState errorState)
        {
            if (!Windows7Taskbar.Windows7OrGreater) return;

            Windows7Taskbar.SetProgressValue(window.Handle, (ulong)current, (ulong)total);

            ThumbnailProgressState thmState;
            switch (errorState)
            {
                case ProgressBarState.Normal:
                    thmState = ThumbnailProgressState.Normal;
                    break;
                case ProgressBarState.Error:
                    thmState = ThumbnailProgressState.Error;
                    break;
                case ProgressBarState.Pause:
                    thmState = ThumbnailProgressState.Paused;
                    break;
                default:
                    throw new ApplicationException("Unknown progress bar state");
            }

            Windows7Taskbar.SetProgressState(window.Handle, thmState);
        }

        public static void ClearTaskBarProgress(this Form window)
        {
            if (!Windows7Taskbar.Windows7OrGreater) return;

            Windows7Taskbar.SetProgressState(window.Handle, ThumbnailProgressState.NoProgress);
        }

        public static void SetErrorState(this ProgressBar progressBar, ProgressBarState errorState)
        {
            // This could hypothetically work on Vista but it doesn't buy us much.
            if (!Windows7Taskbar.Windows7OrGreater) return;

            // set the progress bar state (Normal, Error, Paused)
            Windows7Taskbar.SendMessage(progressBar.Handle, 0x410, (int)errorState, 0);
        }
    }

    /// <summary>
    /// The progress bar state for Windows Vista & 7
    /// </summary>
    public enum ProgressBarState
    {
        /// <summary>
        /// Indicates normal progress
        /// </summary>
        Normal = 1,

        /// <summary>
        /// Indicates an error in the progress
        /// </summary>
        Error = 2,

        /// <summary>
        /// Indicates paused progress
        /// </summary>
        Pause = 3
    }

    #region Windows7TaskBar class - the implementation of the taskbar code

    /// <summary>
    /// The primary coordinator of the Windows 7 taskbar-related activities.
    /// </summary>
    internal static class Windows7Taskbar
    {
        private static ITaskbarList3 _taskbarList;
        internal static ITaskbarList3 TaskbarList
        {
            get
            {
                if (_taskbarList == null)
                {
                    lock (typeof(Windows7Taskbar))
                    {
                        if (_taskbarList == null)
                        {
                            _taskbarList = (ITaskbarList3)new CTaskbarList();
                            _taskbarList.HrInit();
                        }
                    }
                }
                return _taskbarList;
            }
        }

        static readonly OperatingSystem osInfo = Environment.OSVersion;

        internal static bool Windows7OrGreater
        {
            get
            {
                return (osInfo.Version.Major == 6 && osInfo.Version.Minor >= 1)
                    || (osInfo.Version.Major > 6);
            }
        }

        /// <summary>
        /// Sets the progress state of the specified window's
        /// taskbar button.
        /// </summary>
        /// <param name="hwnd">The window handle.</param>
        /// <param name="state">The progress state.</param>
        public static void SetProgressState(IntPtr hwnd, ThumbnailProgressState state)
        {
            if (Windows7OrGreater)
                TaskbarList.SetProgressState(hwnd, state);
        }
        /// <summary>
        /// Sets the progress value of the specified window's
        /// taskbar button.
        /// </summary>
        /// <param name="hwnd">The window handle.</param>
        /// <param name="current">The current value.</param>
        /// <param name="maximum">The maximum value.</param>
        public static void SetProgressValue(IntPtr hwnd, ulong current, ulong maximum)
        {
            if (Windows7OrGreater)
                TaskbarList.SetProgressValue(hwnd, current, maximum);
        }


        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);
    }

    #endregion

    #region COM interop types

    /// <summary>
    /// Represents the thumbnail progress bar state.
    /// </summary>
    internal enum ThumbnailProgressState
    {
        /// <summary>
        /// No progress is displayed.
        /// </summary>
        NoProgress = 0,
        /// <summary>
        /// The progress is indeterminate (marquee).
        /// </summary>
        Indeterminate = 0x1,
        /// <summary>
        /// Normal progress is displayed.
        /// </summary>
        Normal = 0x2,
        /// <summary>
        /// An error occurred (red).
        /// </summary>
        Error = 0x4,
        /// <summary>
        /// The operation is paused (yellow).
        /// </summary>
        Paused = 0x8
    }

    //Based on Rob Jarett's wrappers for the desktop integration PDC demos.
    [ComImportAttribute()]
    [GuidAttribute("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ITaskbarList3
    {
        // ITaskbarList
        [PreserveSig]
        void HrInit();
        [PreserveSig]
        void AddTab(IntPtr hwnd);
        [PreserveSig]
        void DeleteTab(IntPtr hwnd);
        [PreserveSig]
        void ActivateTab(IntPtr hwnd);
        [PreserveSig]
        void SetActiveAlt(IntPtr hwnd);

        // ITaskbarList2
        [PreserveSig]
        void MarkFullscreenWindow(
            IntPtr hwnd,
            [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);

        // ITaskbarList3
        void SetProgressValue(IntPtr hwnd, UInt64 ullCompleted, UInt64 ullTotal);
        void SetProgressState(IntPtr hwnd, ThumbnailProgressState tbpFlags);

        // yadda, yadda - there's more to the interface, but we don't need it.
    }

    [GuidAttribute("56FDF344-FD6D-11d0-958A-006097C9A090")]
    [ClassInterfaceAttribute(ClassInterfaceType.None)]
    [ComImportAttribute()]
    internal class CTaskbarList { }

    #endregion
}
