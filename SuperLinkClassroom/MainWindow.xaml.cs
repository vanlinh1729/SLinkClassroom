using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VncSharpWpf;
using System.Net.NetworkInformation;
using System.Net;
using System.Net.Sockets;
using MaterialDesignThemes.Wpf;

namespace SuperLinkClassroom
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private RemoteDesktopWpf localDesktop; // LocalDesktopWpf control to display your own screen
        private List<string> ips = new List<string>(); 
        private int connectedCount = 0; // Số kết nối VNC đang được mở

        public MainWindow()
        {
            InitializeComponent();
            UpdateConnectedCount();
            btn_Disconnect.IsEnabled = false;
            btn_Connect.IsEnabled = true;
           
        }

        private void MainWindow_OnLoad(object sender, RoutedEventArgs e)
        {
            string ipAddress = "192.168.55.";
            for (int i = 1; i <= 255; i++)
            {
                string currentIP = $"{ipAddress}{i}";
                Ping ping = new Ping();
                ping.PingCompleted += PingCompleted;
                ping.SendAsync(currentIP, 100, currentIP);
            }

            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowState = WindowState.Maximized;
            Activate();
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            

            for (int i = 0; i < ips.Count; i++)
            {
                IPHostEntry hostEntry = Dns.GetHostEntry(ips[i]);
                string platform = hostEntry.HostName;

                GroupBox groupBox = new GroupBox()
                {
                    Header = platform + "(" + ips[i] + ")",
                    Height = 250,
                    Width = 360,
                    Margin = new Thickness(0, 30, 20, 0),
                    Style = (Style)Application.Current.Resources["MaterialDesignCardGroupBox"]
                };

                ColorZoneAssist.SetBackground(groupBox, new SolidColorBrush(Colors.DimGray));
                ColorZoneAssist.SetForeground(groupBox, new SolidColorBrush(Colors.White));
                ColorZoneAssist.SetMode(groupBox, ColorZoneMode.Custom);

                DataTemplate headerTemplate = new DataTemplate();
                FrameworkElementFactory stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
                stackPanelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

                FrameworkElementFactory packIconFactory = new FrameworkElementFactory(typeof(PackIcon));
                packIconFactory.SetValue(PackIcon.WidthProperty, 32.0);
                packIconFactory.SetValue(PackIcon.HeightProperty, 32.0);
                packIconFactory.SetValue(PackIcon.VerticalAlignmentProperty, VerticalAlignment.Center);
                packIconFactory.SetValue(PackIcon.KindProperty, PackIconKind.Monitor);

                FrameworkElementFactory textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                textBlockFactory.SetValue(TextBlock.MarginProperty, new Thickness(8, 0, 0, 0));
                textBlockFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
                textBlockFactory.SetValue(TextBlock.StyleProperty,
                    Application.Current.Resources["MaterialDesignSubtitle1TextBlock"]);
                textBlockFactory.SetBinding(TextBlock.TextProperty, new Binding());

                stackPanelFactory.AppendChild(packIconFactory);
                stackPanelFactory.AppendChild(textBlockFactory);

                headerTemplate.VisualTree = stackPanelFactory;
                groupBox.HeaderTemplate = headerTemplate;

                RemoteDesktopWpf rdp = new RemoteDesktopWpf();
                rdp.Name = "rdp" + i;

                groupBox.Content = rdp;
                int row = i / 3;
                int col = i % 3;
                double left = col * (groupBox.Width + 20);
                double top = row * (groupBox.Height + 20);

                Canvas.SetLeft(groupBox, left);
                Canvas.SetTop(groupBox, top);
                connectionPage.Children.Add(groupBox);

                try
                {
                    rdp.Connect(ips[i]);
                    rdp.SetInputMode(true);
                    txtConnect.Text = "Connected";
                    btn_Connect.IsEnabled = false;
                    btn_Disconnect.IsEnabled = true;
                    connectedCount++; // Tăng số kết nối lên
                    // Cập nhật số kết nối hiển thị trên giao diện
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, " error when connect");
                }
            }
            UpdateConnectedCount();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                connectionPage.Children.Clear();
                connectedCount=0; 
                foreach (GroupBox groupBox in connectionPage.Children.OfType<GroupBox>())
                {
                    RemoteDesktopWpf rdp = groupBox.Content as RemoteDesktopWpf;
                    if (rdp != null)
                    {
                        rdp.Disconnect();
                        
                    }
                }
            
                MessageBox.Show("All connections disconnected");
                txtConnect.Text = "Connect";
                btn_Connect.IsEnabled = true;
                btn_Disconnect.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            UpdateConnectedCount(); // Cập nhật số kết nối hiển thị trên giao diện

        }

        static void PingCompleted(object sender, PingCompletedEventArgs e)
        {
            try
            {
                if (e.Reply != null && e.Reply.Status == IPStatus.Success)
                {
                    if (IsOpenPort(e.Reply.Address.ToString()))
                    {
                        MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
                        if (mainWindow != null)
                        {
                            mainWindow.ips.Add(e.Reply.Address.ToString());
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }

        static bool IsOpenPort(string ip)
        {
            TcpClient client = new TcpClient();
            try
            {
                client.Connect(ip, 5900);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
        
        private void UpdateConnectedCount()
        {
            txtConnectedCount.Text = $"{connectedCount}";
        }

        private void Btn_Save_OnClick(object sender, RoutedEventArgs e)
        {
            if (txtSave.Text == "Save")
            {
                txt_IPMask.IsEnabled = false;
                txtSave.Text = "Edit";
                iconSaveEdit.Kind = PackIconKind.Edit;
            }
            else
            {
                txt_IPMask.IsEnabled = true;
                txtSave.Text = "Save";
                iconSaveEdit.Kind = PackIconKind.ContentSave;
            }
        }

        private void Btn_Search_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string ipAddress = txtIPMask.Text;
                for (int i = 1; i <= 255; i++)
                {
                    string currentIP = $"{ipAddress}{i}";
                    Ping ping = new Ping();
                    ping.PingCompleted += PingCompleted;
                    ping.SendAsync(currentIP, 100, currentIP);
                }

                btn_Search.IsEnabled = false;
            }
            catch (Exception exc)
            {
                if (ips.Count == 0)
                    MessageBox.Show("LAN have no Client!. Check your client or your LAN connection.");
                btn_Search.IsEnabled = true;
            }

        }
    }
}


// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Net;
// using System.Net.NetworkInformation;
// using System.Net.Sockets;
// using System.Threading.Tasks;
// using System.Windows;
// using System.Windows.Controls;
// using System.Windows.Data;
// using System.Windows.Documents;
// using System.Windows.Input;
// using System.Windows.Media;
// using System.Windows.Media.Imaging;
// using MaterialDesignThemes.Wpf;
// using VncSharpWpf;
//
// namespace SuperLinkClassroom
// {
//     public partial class MainWindow : Window
//     {
//         private List<string> ips = new List<string>();
//         private int connectedCount = 0;
//
//         public MainWindow()
//         {
//             InitializeComponent();
//             UpdateConnectedCount();
//             btn_Disconnect.IsEnabled = false;
//             btn_Connect.IsEnabled = true;
//         }
//
//         private async void MainWindow_OnLoad(object sender, RoutedEventArgs e)
//         {
//             WindowStartupLocation = WindowStartupLocation.CenterScreen;
//             WindowState = WindowState.Maximized;
//             Activate();
//
//             // await SearchForIPAddressesAsync();
//         }
//
//         private async void Button_Click(object sender, RoutedEventArgs e)
//         {
//             foreach (string ipAddress in ips)
//             {
//                 GroupBox groupBox = CreateGroupBox(ipAddress);
//                 RemoteDesktopWpf rdp = new RemoteDesktopWpf();
//                 rdp.Name = "rdp" + (connectionPage.Children.Count + 1);
//                 groupBox.Content = rdp;
//
//                 try
//                 {
//                     rdp.Connect(ipAddress);
//                     rdp.SetInputMode(true);
//                     connectedCount++;
//                 }
//                 catch (Exception exception)
//                 {
//                     MessageBox.Show(exception.Message, "Error when connecting");
//                 }
//
//                 connectionPage.Children.Add(groupBox);
//             }
//
//             btn_Connect.IsEnabled = false;
//             btn_Disconnect.IsEnabled = true;
//             UpdateConnectedCount();
//         }
//
//         private async void Button_Click_1(object sender, RoutedEventArgs e)
//         {
//             try
//             {
//                 foreach (GroupBox groupBox in connectionPage.Children.OfType<GroupBox>())
//                 {
//                     RemoteDesktopWpf rdp = groupBox.Content as RemoteDesktopWpf;
//                     if (rdp != null)
//                     {
//                         rdp.Disconnect();
//                     }
//                 }
//
//                 MessageBox.Show("All connections disconnected");
//                 btn_Connect.IsEnabled = true;
//                 btn_Disconnect.IsEnabled = false;
//                 ips.Clear();
//                 connectedCount = 0;
//             }
//             catch (Exception ex)
//             {
//                 MessageBox.Show(ex.Message);
//             }
//
//             connectionPage.Children.Clear();
//             UpdateConnectedCount();
//         }
//
//         private void UpdateConnectedCount()
//         {
//             txtConnectedCount.Text = connectedCount.ToString();
//         }
//
//         private async Task SearchForIPAddressesAsync()
//         {
//             string ipAddress = txtIPMask.Text;
//
//             await Task.Run(() =>
//             {
//                 Parallel.For(1, 255, (i) =>
//                 {
//                     string currentIP = $"{ipAddress}{i}";
//                     Ping ping = new Ping();
//                     ping.PingCompleted += PingCompleted;
//                     ping.SendAsync(currentIP, 100, currentIP);
//                 });
//             });
//
//             btn_Search.IsEnabled = false;
//         }
//
//         private void PingCompleted(object sender, PingCompletedEventArgs e)
//         {
//             try
//             {
//                 if (e.Reply != null && e.Reply.Status == IPStatus.Success)
//                 {
//                     if (IsOpenPort(e.Reply.Address.ToString()))
//                     {
//                         ips.Add(e.Reply.Address.ToString());
//                     }
//                 }
//             }
//             catch (Exception exception)
//             {
//                 Console.WriteLine(exception);
//             }
//         }
//
//         private bool IsOpenPort(string ip)
//         {
//             using (TcpClient client = new TcpClient())
//             {
//                 try
//                 {
//                     client.Connect(ip, 5900);
//                     return true;
//                 }
//                 catch (Exception)
//                 {
//                     return false;
//                 }
//             }
//         }
//
//         private GroupBox CreateGroupBox(string ipAddress)
//         {
//             IPHostEntry hostEntry = Dns.GetHostEntry(ipAddress);
//             string platform = hostEntry.HostName;
//
//             GroupBox groupBox = new GroupBox()
//             {
//                 Header = $"{platform} ({ipAddress})",
//                 Height = 250,
//                 Width = 360,
//                 Margin = new Thickness(0, 30, 20, 0),
//                 Style = (Style)Application.Current.Resources["MaterialDesignCardGroupBox"]
//             };
//
//             ColorZoneAssist.SetBackground(groupBox, new SolidColorBrush(Colors.DimGray));
//             ColorZoneAssist.SetForeground(groupBox, new SolidColorBrush(Colors.White));
//             ColorZoneAssist.SetMode(groupBox, ColorZoneMode.Custom);
//
//             DataTemplate headerTemplate = new DataTemplate();
//             FrameworkElementFactory stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
//             stackPanelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
//
//             FrameworkElementFactory packIconFactory = new FrameworkElementFactory(typeof(PackIcon));
//             packIconFactory.SetValue(PackIcon.WidthProperty, 32.0);
//             packIconFactory.SetValue(PackIcon.HeightProperty, 32.0);
//             packIconFactory.SetValue(PackIcon.VerticalAlignmentProperty, VerticalAlignment.Center);
//             packIconFactory.SetValue(PackIcon.KindProperty, PackIconKind.Monitor);
//
//             FrameworkElementFactory textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
//             textBlockFactory.SetValue(TextBlock.MarginProperty, new Thickness(8, 0, 0, 0));
//             textBlockFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
//             textBlockFactory.SetValue(TextBlock.StyleProperty,
//                 Application.Current.Resources["MaterialDesignSubtitle1TextBlock"]);
//             textBlockFactory.SetBinding(TextBlock.TextProperty, new Binding());
//
//             stackPanelFactory.AppendChild(packIconFactory);
//             stackPanelFactory.AppendChild(textBlockFactory);
//
//             headerTemplate.VisualTree = stackPanelFactory;
//             groupBox.HeaderTemplate = headerTemplate;
//
//             return groupBox;
//         }
//
//         private async void Btn_Save_OnClick(object sender, RoutedEventArgs e)
//         {
//             if (txtSave.Text == "Save")
//             {
//                 txt_IPMask.IsEnabled = false;
//                 txtSave.Text = "Edit";
//                 iconSaveEdit.Kind = PackIconKind.Edit;
//             }
//             else
//             {
//                 txt_IPMask.IsEnabled = true;
//                 txtSave.Text = "Save";
//                 iconSaveEdit.Kind = PackIconKind.ContentSave;
//             }
//
//             await SearchForIPAddressesAsync();
//         }
//
//         private async void Btn_Search_OnClick(object sender, RoutedEventArgs e)
//         {
//             await SearchForIPAddressesAsync();
//         }
//     }
// }