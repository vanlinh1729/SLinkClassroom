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
        private string localip;
        private int connectedCount = 0; // Số kết nối VNC đang được mở

        public MainWindow()
        {
            InitializeComponent();
            UpdateConnectedCount();
            btn_Disconnect.IsEnabled = false;
            btn_Connect.IsEnabled = true;
        }

        private async void MainWindow_OnLoad(object sender, RoutedEventArgs e)
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowState = WindowState.Maximized;
            localip = GetLocalIPAddress();
            Activate();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            ShowLoadingScreen();

            if (ips.Count == 0)
            {
                MessageBox.Show("0 connection found, check your IP range in Configuration.");
            }
            else
            {
                for (int i = 0; i < ips.Count; i++)
                {
                    string ip = ips[i];
                    IPHostEntry hostEntry = await Dns.GetHostEntryAsync(ip);
                    string platform = hostEntry.HostName;

                    GroupBox groupBox = new GroupBox()
                    {
                        Header = $"{platform} ({ip})",
                        Height = 250,
                        Width = 360,
                        Margin = new Thickness(0, 30, 20, 0),
                        Style = (Style)Application.Current.Resources["MaterialDesignCardGroupBox"]
                    };

                    // GroupBox Header
                    DataTemplate headerTemplate = new DataTemplate();
                    FrameworkElementFactory stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
                    stackPanelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

                    FrameworkElementFactory packIconFactory = new FrameworkElementFactory(typeof(PackIcon));
                    packIconFactory.SetValue(PackIcon.KindProperty, PackIconKind.Monitor);
                    packIconFactory.SetValue(PackIcon.WidthProperty, 24.0);
                    packIconFactory.SetValue(PackIcon.HeightProperty, 24.0);
                    packIconFactory.SetValue(PackIcon.VerticalAlignmentProperty, VerticalAlignment.Center);

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

                    // RemoteDesktopWpf Control
                    RemoteDesktopWpf rdp = new RemoteDesktopWpf();
                    rdp.Name = "rdp" + i;
                    rdp.SetInputMode(true);

                    // Set Content
                    StackPanel contentPanel = new StackPanel();
                    contentPanel.Children.Add(rdp);

                    groupBox.Content = contentPanel;

                    // Add to the layout
                    int row = i / 3;
                    int col = i % 3;
                    double left = col * (groupBox.Width + 20);
                    double top = row * (groupBox.Height + 20);

                    Canvas.SetLeft(groupBox, left);
                    Canvas.SetTop(groupBox, top);
                    connectionPage.Children.Add(groupBox);


                    try
                    {
                        await Task.Run(() => { rdp.Dispatcher.Invoke(() => { rdp.Connect(ip); }); });
                        rdp.Dispatcher.Invoke(() =>
                        {
                            rdp.SetInputMode(true);
                            txtConnect.Text = "Connected";
                            btn_Connect.IsEnabled = false;
                            btn_Disconnect.IsEnabled = true;
                        });
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

            HideLoadingScreen();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                connectionPage.Children.Clear();
                connectedCount = 0;
                foreach (GroupBox groupBox in connectionPage.Children.OfType<GroupBox>())
                {
                    RemoteDesktopWpf rdp = groupBox.Content as RemoteDesktopWpf;
                    if (rdp != null)
                    {
                        rdp.Disconnect();
                    }
                }

                MessageBox.Show("All connections have been disconnected");
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

        private void PingCompleted(PingReply reply, Exception o, bool cancelled, object userToken)
        {
            if (reply != null && reply.Status == IPStatus.Success && reply.Address.ToString() != localip)
            {
                if (IsOpenPort(reply.Address.ToString()))
                {
                    Application.Current.Dispatcher.Invoke(() => { ips.Add(reply.Address.ToString()); });
                }
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

        private async void Btn_Search_OnClick(object sender, RoutedEventArgs e)
        {
            ips.Clear();
            studentPage.Children.Clear();
            string ipAddress = txtIPMask.Text;
            ShowLoadingScreen();
            // Gọi hàm tải dữ liệu hoặc thực hiện các công việc khác ở đây
            try
            {
                List<Task> pingTasks = new List<Task>();

                for (int i = 1; i <= 255; i++)
                {
                    string currentIP = $"{ipAddress}{i}";

                    Task pingTask = Task.Run(async () =>
                    {
                        using (Ping ping = new Ping())
                        {
                            PingReply reply = await ping.SendPingAsync(currentIP, 100);
                            PingCompleted(reply, null, false, currentIP);
                        }
                    });

                    pingTasks.Add(pingTask);
                }

                await Task.WhenAll(pingTasks);
                foreach (string ip in ips)
                {
                    // Create a new Chip for each IP
                    MaterialDesignThemes.Wpf.Chip chip = new MaterialDesignThemes.Wpf.Chip();
                    chip.Content = ip;
                    chip.Style = (Style)Application.Current.Resources["MaterialDesignOutlineChip"];
                    // Add the Chip to studentPage
                    studentPage.Children.Add(chip);
                }

                // Sau khi hoàn thành, gọi HideLoadingScreen() để ẩn màn hình loading
                HideLoadingScreen();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message + " Check you IP range.");
                HideLoadingScreen();
            }
        }
        private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        
                        localip = ip.ToString();
                    }
                }

                return localip;
        }

        private void ShowLoadingScreen()
        {
            LoadingGrid.Visibility = Visibility.Visible;
            // Thực hiện các xử lý bổ sung nếu cần
        }

        private void HideLoadingScreen()
        {
            LoadingGrid.Visibility = Visibility.Collapsed;
            // Thực hiện các xử lý bổ sung nếu cần
        }

        private void Btn_Show_OnClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("This feature is in develop progress");
        }

        private void Btn_UnShow_OnClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("This feature is in develop progress");
        }
    }
}