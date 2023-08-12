using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
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
using System.Threading;
using MaterialDesignThemes.Wpf;
using SuperLinkClassroom.Model;

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
        private TcpListener tcpListener;
        private ConcurrentBag<TcpClient> connectedClients = new ConcurrentBag<TcpClient>();
        private TcpClient tcpClient;
        private NetworkStream stream;
    
        public MainWindow()
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen; // Center the login window
            loginWindow.ShowDialog();
            InitializeComponent();
            StartServer();
            UpdateConnectedCount();
            btn_Disconnect.IsEnabled = false;
            btn_Connect.IsEnabled = true;
            btn_Disremote.IsEnabled = false;
            btn_Remote.IsEnabled = true;
        }

        private async void MainWindow_OnLoad(object sender, RoutedEventArgs e)
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowState = WindowState.Maximized;
            txtChatServerIPAddress.Text = GetLocalIPAddress();
            try
            {
                tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(GetLocalIPAddress(), 12345); // Replace with the actual IP address of the server
                stream = tcpClient.GetStream();
                ReceiveMessages();
            }
            catch (Exception ex)
            {
                // Handle connection errors
            }

            localip = GetLocalIPAddress() != null ? GetLocalIPAddress() : GetEthernetIPv4Address();
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

                studentPage.Children.Clear();

                double chipMargin = 10; // Spacing between the chips

                foreach (string ip in ips)
                {
                    string hostName = GetHostName(ip);

                    // Create a new Chip for each IP
                    MaterialDesignThemes.Wpf.Chip chip = new MaterialDesignThemes.Wpf.Chip();
                    chip.Style = (Style)Application.Current.Resources["MaterialDesignOutlineChip"];

                    // Create a WrapPanel to arrange the computer name and IP address vertically
                    WrapPanel wrapPanel = new WrapPanel();
                    wrapPanel.Orientation = Orientation.Vertical;
                    wrapPanel.HorizontalAlignment = HorizontalAlignment.Center;
                    wrapPanel.VerticalAlignment = VerticalAlignment.Center;

                    // Add the ComputerName (hostName) to the WrapPanel
                    TextBlock nameTextBlock = new TextBlock();
                    nameTextBlock.Text = hostName ?? "Unknown";
                    nameTextBlock.TextAlignment = TextAlignment.Center;
                    nameTextBlock.FontSize = 14;
                    nameTextBlock.FontWeight = FontWeights.Bold;
                    wrapPanel.Children.Add(nameTextBlock);

                    // Add the IP address to the WrapPanel
                    TextBlock ipTextBlock = new TextBlock();
                    ipTextBlock.Text = ip;
                    ipTextBlock.TextAlignment = TextAlignment.Center;
                    ipTextBlock.FontSize = 12;
                    ipTextBlock.Foreground = Brushes.Gray;
                    wrapPanel.Children.Add(ipTextBlock);

                    // Set the WrapPanel as the content of the Chip
                    chip.Content = wrapPanel;

                    // Calculate the size based on the length of the computer name and IP address
                    double nameWidth = MeasureTextWidth(hostName ?? "Unknown", 14, FontWeights.Bold);
                    double ipWidth = MeasureTextWidth(ip, 12, FontWeights.Normal);

                    double chipWidth = Math.Max(nameWidth, ipWidth) + 40; // Add some extra padding

                    // Set the size of the Chip
                    chip.Width = chipWidth;
                    chip.Height = 80; // Use "Auto" height to fit the content automatically
                    chip.Margin = new Thickness(chipMargin); // Add some margin around the chip

                    // Add the Chip to studentPage
                    studentPage.Children.Add(chip);
                }
 

                // Sau khi hoàn thành, gọi HideLoadingScreen()để ẩn màn hình loading
                HideLoadingScreen();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message + " Check you IP range.");
                HideLoadingScreen();
            }
        }

        private double MeasureTextWidth(string text, double fontSize, FontWeight fontWeight)
        {
            FormattedText formattedText = new FormattedText(
                text,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, fontWeight, FontStretches.Normal),
                fontSize,
                Brushes.Black,
                new NumberSubstitution(),
                1.0);

            return formattedText.Width;
        }

        private string GetLocalIPAddress()
        {
            NetworkInterface wirelessAdapter = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(adapter =>
                    adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 &&
                    adapter.OperationalStatus == OperationalStatus.Up);

            if (wirelessAdapter != null)
            {
                foreach (var address in wirelessAdapter.GetIPProperties().UnicastAddresses)
                {
                    if (address.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return address.Address.ToString();
                    }
                }
            }

            return null;
        }

        static string GetEthernetIPv4Address()
        {
            NetworkInterface ethernetAdapter = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(adapter =>
                    adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
                    adapter.OperationalStatus == OperationalStatus.Up);

            if (ethernetAdapter != null)
            {
                foreach (var address in ethernetAdapter.GetIPProperties().UnicastAddresses)
                {
                    if (address.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return address.Address.ToString();
                    }
                }
            }

            return null;
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


        private async void Btn_Remote_OnClick(object sender, RoutedEventArgs e)
        {
            ShowLoadingScreen();

            string remoteIp = txtIP.Text;

            if (string.IsNullOrEmpty(remoteIp))
            {
                MessageBox.Show("Please enter a valid remote IP address.");
                HideLoadingScreen(); // Hide the loading screen if there's an issue with the IP address.
                return;
            }

            RemoteDesktopWpf rdp = new RemoteDesktopWpf();
            rdp.SetInputMode(true); // Enable input handling for the remote desktop control.

            // Set up the initial size of the rdp control to match the size of RemoteMonitor
            rdp.Width = RemoteMonitor.ActualWidth;
            rdp.Height = RemoteMonitor.ActualHeight;


            try
            {
                await Task.Delay(1000);
                // Connect to the remote IP address.
                await Task.Run(() => { rdp.Dispatcher.Invoke(() => { rdp.Connect(remoteIp); }); });

                // Add the RemoteDesktopWpf control to the RemoteMonitor Canvas
                RemoteMonitor.Children.Clear(); // Clear existing content if any.
                RemoteMonitor.Children.Add(rdp);

                // Get the computer name
                string
                    computerName =
                        GetHostName(txtIP.Text); // Replace this with the method to retrieve the computer name

                // Update the computer name in the tab header
                txtComputerName.Text = !string.IsNullOrEmpty(computerName) ? " - " + computerName : "";

                // Update rdp size when RemoteMonitor size changes
                RemoteMonitor.SizeChanged += (s, args) =>
                {
                    rdp.Width = RemoteMonitor.ActualWidth;
                    rdp.Height = RemoteMonitor.ActualHeight;
                };
                btn_Remote.IsEnabled = false;
                btn_Disremote.IsEnabled = true;
                txtRemote.Text = "Connected";
                HideLoadingScreen(); // Hide the loading screen after the connection attempt.
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error when connecting to remote desktop");
            }

        }

        private void btn_Disremote_Click(object sender, RoutedEventArgs e)
        {
            // Check if the RemoteMonitor contains the rdp control
            if (RemoteMonitor.Children.Count > 0)
            {
                var rdp = RemoteMonitor.Children[0] as RemoteDesktopWpf;
                if (rdp != null)
                {
                    // Disconnect the rdp control
                    rdp.Disconnect();

                    // Remove the rdp control from the RemoteMonitor
                    RemoteMonitor.Children.Clear();

                    // Hide the computer name in the tab header
                    txtComputerName.Text = "";

                    btn_Remote.IsEnabled = true;
                    btn_Disremote.IsEnabled = false;
                    txtRemote.Text = "Remote";
                    MessageBox.Show("Connection have been disconnected");
                }
            }
        }


        private string GetHostName(string ipAddress)
        {
            try
            {
                IPHostEntry hostEntry = Dns.GetHostEntry(ipAddress);
                return hostEntry?.HostName;
            }
            catch
            {
                return null; // Trả về null nếu không thể lấy tên máy tính từ địa chỉ IP
            }
        }

        private async void StartServer()
        {
            tcpListener = new TcpListener(IPAddress.Any, 12345);
            tcpListener.Start();

            while (true)
            {
                TcpClient client = await tcpListener.AcceptTcpClientAsync();
                connectedClients.Add(client);

                Task.Run(() => HandleClient(client));
            }
        }

        private async void HandleClient(TcpClient client)
        {
            try
            {
                using (var stream = client.GetStream())
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead;

                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        BroadcastMessage(message);
                    }

                    connectedClients.TryTake(out client);
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions here
            }
        }

        private void BroadcastMessage(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message + Environment.NewLine);

            foreach (var client in connectedClients)
            {
                try
                {
                    client.GetStream().WriteAsync(data, 0, data.Length);
                }
                catch (Exception)
                {
                    // Handle exceptions when sending fails
                }
            }

            // Dispatcher.Invoke(() => { txtChat.AppendText(message + Environment.NewLine); });
        }
        

        // private async void ReceiveMessages()
        // {
        //     byte[] buffer = new byte[1024];
        //     int bytesRead;
        //
        //     while (true)
        //     {
        //         try
        //         {
        //             bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        //             string messageContent = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        //
        //             // Hiển thị tin nhắn nhận lại trên giao diện
        //             Dispatcher.Invoke(() =>
        //             {
        //                 string formattedMessage = $"{GetLocalIPAddress()} - {Environment.MachineName}: {messageContent}";
        //                 ChatMessage receivedMsg = new ChatMessage();
        //                 receivedMsg.SenderInfo = $"{GetLocalIPAddress()} - {Environment.MachineName}: ";
        //                 receivedMsg.MessageContent = $"{messageContent}";
        //                 messageListView.Items.Add(receivedMsg);
        //             });
        //         }
        //         catch (Exception)
        //         {
        //             // Handle exceptions or disconnections
        //             break;
        //         }
        //     }
        // }
        
        private async void ReceiveMessages()
        {
            byte[] buffer = new byte[1024];
            int bytesRead;

            while (true)
            {
                try
                {
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string messageContent = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                  

                    // Update the UI using Dispatcher
                    Dispatcher.Invoke(() =>
                    {
                        ChatMessage receivedMsg = new ChatMessage();
                        receivedMsg.SenderInfo = messageContent.IndexOf(":") != -1 ? messageContent.Substring(0, messageContent.IndexOf(":")) +":" : "Unknown" ;
                        receivedMsg.MessageContent = messageContent.Substring(messageContent.IndexOf(":")+1);
                        ScrollToBottom(FindVisualChild<ScrollViewer>(messageListView));
                        messageListView.Items.Add(receivedMsg);
                    });
                }
                catch (Exception)
                {
                    // Handle exceptions or disconnections
                    break;
                }
            }
        }
        
        
        private async Task SendMessage(string message)
        {
            try
            {
                string senderInfo = $"{Environment.MachineName} - {GetLocalIPAddress()}";
                string formattedMessage = $"{senderInfo}: {message}";

                byte[] data = Encoding.UTF8.GetBytes(formattedMessage);
                await stream.WriteAsync(data, 0, data.Length);

                // Scroll to the bottom after sending a message
                ScrollToBottom(FindVisualChild<ScrollViewer>(messageListView));
            }
            catch (Exception)
            {
                // Handle send errors
            }
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {

            if (tcpClient == null || !tcpClient.Connected)
            {
                MessageBox.Show("Please join the class first before sending a message.");
                return;
            }
            string messageContent = messageTextBox.Text;
            if (messageContent != "")
            {
                SendMessage(messageContent);
            }
            // Clear the message input field
            messageTextBox.Text = string.Empty;
            ScrollToBottom(FindVisualChild<ScrollViewer>(messageListView));

        }

        private void ScrollToBottom(ScrollViewer scrollViewer)
        {
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToEnd();
            }
        }
        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    return typedChild;
                }

                T result = FindVisualChild<T>(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }
    }
}