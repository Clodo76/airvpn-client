﻿// <eddie_source_header>
// This file is part of Eddie/AirVPN software.
// Copyright (C)2014-2016 AirVPN (support@airvpn.org) / https://airvpn.org
//
// Eddie is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Eddie is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Eddie. If not, see <http://www.gnu.org/licenses/>.
// </eddie_source_header>

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Xml;
using Eddie.Core;
using Microsoft.Win32;

namespace Eddie.Platform.Windows
{
	public static class Native
	{
		private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

		private const uint FILE_READ_EA = 0x0008;
		private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x2000000;

		[DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern uint GetFinalPathNameByHandle(IntPtr hFile, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpszFilePath, uint cchFilePath, uint dwFlags);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool CloseHandle(IntPtr hObject);

		[DllImport("Kernel32")]
		public static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandlerRoutine Handler, bool Add);
		public delegate bool ConsoleCtrlHandlerRoutine(CtrlTypes CtrlType);		
		// An enumerated type for the control messages sent to the handler routine.
		public enum CtrlTypes
		{
			CTRL_C_EVENT = 0,
			CTRL_BREAK_EVENT,
			CTRL_CLOSE_EVENT,
			CTRL_LOGOFF_EVENT = 5,
			CTRL_SHUTDOWN_EVENT
		}

		[DllImport("kernel32.dll")]
		internal static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);
		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool AttachConsole(uint dwProcessId);
		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		internal static extern bool FreeConsole();

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr CreateFile(
				[MarshalAs(UnmanagedType.LPTStr)] string filename,
				[MarshalAs(UnmanagedType.U4)] uint access,
				[MarshalAs(UnmanagedType.U4)] FileShare share,
				IntPtr securityAttributes, // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
				[MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
				[MarshalAs(UnmanagedType.U4)] uint flagsAndAttributes,
				IntPtr templateFile);

		public static string GetFinalPathName(string path)
		{
			var h = CreateFile(path,
				FILE_READ_EA,
				FileShare.ReadWrite | FileShare.Delete,
				IntPtr.Zero,
				FileMode.Open,
				FILE_FLAG_BACKUP_SEMANTICS,
				IntPtr.Zero);
			if (h == INVALID_HANDLE_VALUE)
				return "";

			try
			{
				var sb = new StringBuilder(1024);
				var res = GetFinalPathNameByHandle(h, sb, 1024, 0);
				if (res == 0)
					return "";

				return sb.ToString();
			}
			finally
			{
				CloseHandle(h);
			}
		}

		[DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool IsWow64Process(
			[In] IntPtr hProcess,
			[Out] out bool wow64Process
		);

		public static bool InternalCheckIsWow64()
		{
			if ((Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1) ||
				Environment.OSVersion.Version.Major >= 6)
			{
				using (System.Diagnostics.Process p = System.Diagnostics.Process.GetCurrentProcess())
				{
					bool retVal;
					try
					{
						if (!IsWow64Process(p.Handle, out retVal))
						{
							return false;
						}
					}
					catch (Exception)
					{
						return false;
					}
					return retVal;
				}
			}
			else
			{
				return false;
			}
		}

		[DllImport("Lib.Platform.Windows.Native.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void LibPocketFirewallInit(string name);

		[DllImport("Lib.Platform.Windows.Native.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool LibPocketFirewallStart(string xml);

		[DllImport("Lib.Platform.Windows.Native.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool LibPocketFirewallStop();

		[DllImport("Lib.Platform.Windows.Native.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern UInt64 LibPocketFirewallAddRule(string xml);

		[DllImport("Lib.Platform.Windows.Native.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool LibPocketFirewallRemoveRule(UInt64 id);

		[DllImport("Lib.Platform.Windows.Native.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool LibPocketFirewallRemoveRuleDirect(UInt64 id);

		[DllImport("Lib.Platform.Windows.Native.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr LibPocketFirewallGetLastError();

		[DllImport("Lib.Platform.Windows.Native.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern UInt32 LibPocketFirewallGetLastErrorCode();

		public static string LibPocketFirewallGetLastError2()
		{
			IntPtr result = LibPocketFirewallGetLastError();
			string s = Marshal.PtrToStringAnsi(result);
			UInt32 code = LibPocketFirewallGetLastErrorCode();
			if (code != 0)
				s += " (0x" + code.ToString("x") + ")";
			return s;
		}

		[DllImport("Lib.Platform.Windows.Native.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int eddie_test_native();
		[DllImport("Lib.Platform.Windows.Native.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int GetInterfaceMetric(int idx, string layer);
		[DllImport("Lib.Platform.Windows.Native.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int SetInterfaceMetric(int idx, string layer, int value);
	}
}
