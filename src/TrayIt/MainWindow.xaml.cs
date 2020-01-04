using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;

namespace TrayIt
{
	public partial class MainWindow : Window
	{
		private readonly Engine _trayEngine = new Engine();
		public System.Windows.Forms.NotifyIcon ApplicationIcon = new System.Windows.Forms.NotifyIcon();

		#region UI

		public MainWindow()
		{
			InitializeComponent();

			RefreshProcesses();

			Closing += MainWindow_Closing;
			StateChanged += MainWindow_StateChanged;
		}

		private void MainWindow_StateChanged(object sender, EventArgs e)
		{
			if (WindowState == WindowState.Minimized) Hide();
		}

		private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (MessageBox.Show("Are you sure you want to quit TrayIt? This will un-tray your current applications.", "Quit TrayIt", MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes)
			{
				ApplicationIcon.Visible = false;
				UnTrayAll();
				Process.GetCurrentProcess().Kill();
			}
			else
			{
				e.Cancel = true;
				return;
			}
		}

		private void ButtonTrayControl_Click(object sender, RoutedEventArgs e)
		{
			if (ListBoxApplications.SelectedIndex > -1 && ListBoxApplications.SelectedIndex < ListBoxApplications.Items.Count)
			{
				_trayEngine.TrayApplication(ProcessFromItem(ListBoxApplications.SelectedItem));
				RefreshProcesses();
			}
		}

		private void ButtonUntrayControl_Click(object sender, RoutedEventArgs e)
		{
			if (ListBoxTrayedApplications.SelectedIndex > -1 && ListBoxTrayedApplications.SelectedIndex < ListBoxTrayedApplications.Items.Count)
			{
				_trayEngine.UntrayApplication(ProcessFromItem(ListBoxTrayedApplications.SelectedItem));
				RefreshProcesses();
			}
		}

		private void ButtonRestoreAll_Click(object sender, RoutedEventArgs e)
		{
			UnTrayAll();
		}

		private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
		{
			RefreshProcesses();
		}

		private void RefreshProcesses()
		{
			ListBoxApplications.Items.Clear();
			ListBoxTrayedApplications.Items.Clear();

			ListBoxItem item;
			foreach (var p in Process.GetProcesses())
			{
				_trayEngine.IsTrayable(p);
				if (_trayEngine.IsTrayable(p) && !_trayEngine.TrayedApplications.Keys.Contains(p))
				{
					item = new ListBoxItem();
					if (p.MainWindowTitle != "")
						item.Content = p.MainWindowTitle;
					else
						item.Content = p.ProcessName + ".exe";
					item.Tag = p;

					ListBoxApplications.Items.Add(item);
				}
			}

			foreach (var pt in _trayEngine.TrayedApplications.Keys)
			{
				item = new ListBoxItem();
				item.Content = pt.MainWindowTitle;
				item.Tag = pt;

				ListBoxTrayedApplications.Items.Add(item);
			}
		}

		private Process ProcessFromItem(object item)
		{
			try
			{
				return (Process) ((ListBoxItem) item).Tag;
			}
			catch
			{
				return null;
			}
		}

		private void CheckBoxTrayInactive_Checked(object sender, RoutedEventArgs e)
		{
			_trayEngine.StartAutoTray();
		}

		private void CheckBoxTrayInactive_Unchecked(object sender, RoutedEventArgs e)
		{
			_trayEngine.StopAutoTray();
		}

		#endregion

		public void UnTrayAll()
		{
			foreach (ListBoxItem i in ListBoxTrayedApplications.Items) _trayEngine.UntrayApplication(ProcessFromItem(i));
		}
	}
}