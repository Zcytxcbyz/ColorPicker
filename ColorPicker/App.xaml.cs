using System;
using System.Windows;

namespace ColorPicker
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                MainWindow window = new MainWindow();
                window.Show();
            }
            catch (Exception err)
            {
                MessageBoxResult result=
                MessageBox.Show(err.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                if (result == MessageBoxResult.OK)
                {
                    Environment.Exit(0);
                }
            }
        }
    }
}
