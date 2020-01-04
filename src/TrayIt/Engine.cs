using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

internal class Engine
{
	/// <summary>
	/// Dictionary containing all trayed processes, and thier NotifyIcon.
	/// </summary>
	public Dictionary<Process, System.Windows.Forms.NotifyIcon> TrayedApplications { get; } = new Dictionary<Process, System.Windows.Forms.NotifyIcon>();

	private readonly List<Process> _hiddenWindows = new List<Process>();

	#region APIs

	/* For grabbing icons. */
	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll")]
	private static extern IntPtr GetClassLong(int hwnd, int nIndex);

	/// <summary>
	/// Show and hide windows.
	/// </summary>
	/// <param name="hWnd">Handle of the window to show/hide.</param>
	/// <param name="nCmdShow">Window state.</param>
	/// <returns></returns>
	[DllImport("user32.dll")]
	private static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommands nCmdShow);

	private enum ShowWindowCommands : int
	{
		Hide = 0,
		Normal = 1,
		ShowMinimized = 2,
		Maximize = 3,
		ShowMaximized = 3,
		ShowNoActivate = 4,
		Show = 5,
		Minimize = 6,
		ShowMinNoActive = 7,
		ShowNa = 8,
		Restore = 9,
		ShowDefault = 10,
		ForceMinimize = 11
	}

	/* Auto traying. */
	/// <summary>
	/// Gets the handle of the foregound window.
	/// </summary>
	/// <returns>The handle of the foreground window.</returns>
	[DllImport("user32.dll")]
	private static extern IntPtr GetForegroundWindow();

	/// <summary>
	/// Sets the foreground window.
	/// </summary>
	/// <param name="hWnd">Handle of the window to set as foreground.</param>
	/// <returns></returns>
	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SetForegroundWindow(IntPtr hWnd);

	#endregion

	/// <summary>
	/// Creates a tray icon, and minimizes the window of the specified process.
	/// </summary>
	/// <param name="p">The process of the window to tray.</param>
	/// <returns>Return false if it fails to tray the process.</returns>
	public bool TrayApplication(Process p)
	{
		try
		{
			if (IsTrayable(p))
			{
				var icon = CreateTrayIcon(p);

				if (icon != null)
				{
					TrayedApplications.Add(p, icon);
					HideWindow(p);
					return true;
				}
				else
					return false;
			}
			else
				return false;
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Removes the tray icon, and restores the window state of the specified process.
	/// </summary>
	/// <param name="p">The process of the window to un-tray.</param>
	public void UntrayApplication(Process p)
	{
		try
		{
			using (var icon = TrayedApplications[p])
			{
				icon.Visible = false;
				icon.Dispose();
			}

			ShowWindow(p);
			TrayedApplications.Remove(p);
		}
		catch
		{
		}
	}

	/// <summary>
	/// Shows the window of the specified process.
	/// </summary>
	/// <param name="p">Process of the window to show.</param>
	/// <returns>Return false if it fails to show the window.</returns>
	public bool ShowWindow(Process p)
	{
		try
		{
			if (p.MainWindowHandle != IntPtr.Zero)
			{
				ShowWindow(p.MainWindowHandle, ShowWindowCommands.Restore);
				SetForegroundWindow(p.MainWindowHandle);
				if (_hiddenWindows.Contains(p)) _hiddenWindows.Remove(p);
				return true;
			}

			return false;
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Hides the window of the specified process.
	/// </summary>
	/// <param name="p">Process of the window to hide.</param>
	/// <returns>Return false if it fails to hide the window.</returns>
	public bool HideWindow(Process p)
	{
		try
		{
			if (p.MainWindowHandle != IntPtr.Zero)
			{
				ShowWindow(p.MainWindowHandle, ShowWindowCommands.Hide);
				if (!_hiddenWindows.Contains(p)) _hiddenWindows.Add(p);
				return true;
			}

			return false;
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// A function that analyzes the process and return True if you can tray the application.
	/// </summary>
	/// <param name="p">Process to checl.</param>
	/// <returns>Application is trayable.</returns>
	public bool IsTrayable(Process p)
	{
		try
		{
			if (p.MainWindowHandle != IntPtr.Zero && p.MainWindowHandle != null)
			{
				try
				{
					var state = p.HasExited;
				}
				catch (System.ComponentModel.Win32Exception)
				{
					return false;
				}

				return true;
			}
			else
				return false;
		}
		catch
		{
			return false;
		}
	}

	#region Tray Icon

	/// <summary>
	/// Creates and shows a new tray icon for the specified process.
	/// </summary>
	/// <param name="p">Process to create tray icon for.</param>
	/// <returns>Returns the created NotifyIcon, or null if an error occurred.</returns>
	private System.Windows.Forms.NotifyIcon CreateTrayIcon(Process p)
	{
		try
		{
			// Create Menu
			var menu = new System.Windows.Forms.ContextMenu();
			menu.MenuItems.Add("Show").Click += menuItemShow_Click;
			menu.MenuItems.Add("Hide").Click += menuItemHide_Click;
			menu.MenuItems.Add("Remove Tray").Click += menuItemRemoveTray_Click;
			menu.Tag = p;

			// Create Icon
			var icon = new System.Windows.Forms.NotifyIcon();
			icon.MouseDoubleClick += icon_MouseDoubleClick;
			if (p.MainWindowTitle.Length >= 64)
				icon.Text = p.MainWindowTitle.Substring(0, 63);
			else
				icon.Text = p.MainWindowTitle;
			icon.Tag = p;
			var ico = GetIcon(p);
			if (ico == null) return null;
			icon.Icon = ico;
			icon.ContextMenu = menu;
			icon.BalloonTipText = "I have been trayed!";
			icon.Visible = true;
			icon.ShowBalloonTip(3000);

			return icon;
		}
		catch (Exception ex)
		{
			System.Windows.MessageBox.Show("Failed to create tray icon! Exception below:\n\n" + ex.ToString());
			return null;
		}
	}

	/// <summary>
	/// Gets a System.Windows.Forms icon from a process.
	/// </summary>
	/// <param name="p">Process to get icon from.</param>
	/// <returns>Icon extracted from the main window handle of the process.</returns>
	public Icon GetIcon(Process p)
	{
		try
		{
			// No main window handle. Trying to grab icon is pointless. Exit now.
			if ((p.MainWindowHandle == null) | (p.MainWindowHandle == IntPtr.Zero))
				throw new Exception();

			var handle = SendMessage(p.MainWindowHandle, 0x7f, (IntPtr) 0, IntPtr.Zero);
			if (handle != IntPtr.Zero)
				return Icon.FromHandle(handle);

			handle = SendMessage(p.MainWindowHandle, 0x7f, (IntPtr) 1, IntPtr.Zero);
			if (handle != IntPtr.Zero)
				return Icon.FromHandle(handle);

			handle = GetClassLong((int) p.MainWindowHandle, -34);
			if (handle != IntPtr.Zero)
				return Icon.FromHandle(handle);

			handle = GetClassLong((int) p.MainWindowHandle, -14);
			if (handle != IntPtr.Zero)
				return Icon.FromHandle(handle);

			throw new Exception();
		}
		catch
		{
			System.Windows.MessageBox.Show("Failed to get icon.");
			return null;
		}
	}

	private void icon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
	{
		var p = (Process) ((System.Windows.Forms.NotifyIcon) sender).Tag;
		if (_hiddenWindows.Contains(p))
			ShowWindow(p);
		else
			HideWindow(p);
	}

	private void menuItemShow_Click(object sender, EventArgs e)
	{
		ShowWindow(ProcessFromMenuItem(sender));
	}

	private void menuItemHide_Click(object sender, EventArgs e)
	{
		HideWindow(ProcessFromMenuItem(sender));
	}

	private void menuItemRemoveTray_Click(object sender, EventArgs e)
	{
		UntrayApplication(ProcessFromMenuItem(sender));
	}

	private Process ProcessFromMenuItem(object menu) => (Process) ((System.Windows.Forms.MenuItem) menu).Parent.Tag;

	#endregion

	#region Auto Tray

	private System.Threading.Thread _autoTrayThread;

	public void StartAutoTray()
	{
		if (_autoTrayThread != null) StopAutoTray();
		_autoTrayThread = new System.Threading.Thread(AutoTrayProcess);
		_autoTrayThread.Priority = System.Threading.ThreadPriority.Lowest;
		_autoTrayThread.Start();
	}

	public void StopAutoTray()
	{
		if (_autoTrayThread != null)
		{
			try
			{
				_autoTrayThread.Abort();
			}
			catch
			{
			}

			_autoTrayThread = null;
		}
	}

	private void AutoTrayProcess()
	{
		while (true)
		{
			System.Threading.Thread.Sleep(500);
			try
			{
				var foregroundWindow = GetForegroundWindow();
				foreach (var p in TrayedApplications.Keys)
				{
					if (p.HasExited)
					{
						UntrayApplication(p);
						break;
					}

					if (!_hiddenWindows.Contains(p) && p.MainWindowHandle != foregroundWindow)
						HideWindow(p);
				}
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show("Failed to check trayed applications window state/validity! Exception below:\n\n" + ex.ToString());
				StopAutoTray();
			}
		}
	}

	#endregion
}