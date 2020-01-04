using System;
using System.Windows;

namespace TrayIt
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        MainWindow _window = new MainWindow();

        App()
        {
            _window.ApplicationIcon.Icon = new System.Drawing.Icon(GetResourceStream(new Uri("pack://application:,,/Icon.ico")).Stream);

            var menu = new System.Windows.Forms.ContextMenu();
            menu.MenuItems.Add("Show").Click += App_ShowClick;
            menu.MenuItems.Add("Exit").Click += App_ExitClick;
            _window.ApplicationIcon.ContextMenu = menu;

            _window.ApplicationIcon.MouseDoubleClick += applicationIcon_MouseDoubleClick;

            _window.ApplicationIcon.Visible = true;

            _window.Show();
        }

        void applicationIcon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (_window.Visibility == Visibility.Hidden)
            {
                _window.Show();
                _window.WindowState = WindowState.Normal;
                _window.Activate();
            }
            else
            {
                _window.Hide();
            }
        }

        void App_ShowClick(object sender, EventArgs e)
        {
            _window.Show();
            _window.WindowState = WindowState.Normal;
            _window.Activate();
        }

        void App_ExitClick(object sender, EventArgs e)
        {
            _window.ApplicationIcon.Visible = false;
            _window.ApplicationIcon.Dispose();
            _window.UnTrayAll();
            _window.Close();
            System.Threading.Thread.Sleep(500);
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
    }
}
