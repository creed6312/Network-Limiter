using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NetworkLimiter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow
    {
        #region Variables
        private MK _mikrotik;
        private List<Host> _hosts;
        private List<Queue> _queues;
        private DataTable _dataTableNetwork;
        private int _secondCounter;
        private readonly ObservableCollection<ChartPlot> _values = new ObservableCollection<ChartPlot>();

        public class ChartPlot
        {
            public int Key { get; set; }
            public double Value1 { get; set; }
            public double Value2 { get; set; }
        }
        #endregion

        #region Delegate Callbacks
        public bool Login()
        {
            var success = false;
            Dispatcher.Invoke(()=>
            {
                _mikrotik = new MK(txtIP.Text);
                if (!_mikrotik.Login(txtUsername.Text, txtPassword.Password))
                {
                    MessageBox.Show("Failed to Login.");
                    _mikrotik.Close();
                    txtPassword.IsEnabled = true;
                    txtUsername.IsEnabled = true;
                    txtIP.IsEnabled = true;
                    btnLogin.Content = "Login";
                    return;
                }
                success = true;
                txtPassword.IsEnabled = false;
                txtUsername.IsEnabled = false;
                txtIP.IsEnabled = false;
                btnLogin.Content = "Log Out";
            });
            return success;
        }

        public void UpdateDebugLog(string message)
        {
            Dispatcher.Invoke(()=>
            {
                if (chkDebug.IsChecked != null && (bool) chkDebug.IsChecked)
                    richTextBox.AppendText(message);
            });
        }

        public void UpdateQueues()
        {
            foreach (var t in _hosts)
                foreach (var t1 in _queues.Where(t1 => t.getIP().Equals(t1.getIP())))
                {
                    t1.setComment(t.getComment());
                    t1.setOnline(t.getOnline());
                }

            Dispatcher.Invoke(()=>
            {
                lstboxQueue.Items.Clear();
                foreach (var queue in _queues)
                {
                    lstboxQueue.Items.Add(queue.getIP() + " - " + queue.getComment());
                }

                DelayedExecutionService.DelayedExecute(() =>
                {
                    for (var i = 0; i < _queues.Count; i++)
                    {
                        var item = lstboxQueue.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                        if (item != null) item.Foreground = _queues[i].getOnline() ? Brushes.Green : Brushes.Red;
                    }
                    lstboxQueue.SelectedIndex = 0;
                }, 150);
            });
        }

        public void UpdateHosts()
        {
            Dispatcher.Invoke(()=>
            {
                lstboxIP.Items.Clear();
                var sortedList = _hosts.OrderBy(o => o.getIP()).ToList();
                _hosts = sortedList;
                foreach (var host in sortedList)
                    lstboxIP.Items.Add(host.getIP() + " - " + host.getComment());

                DelayedExecutionService.DelayedExecute(() =>
                {
                    for (var i = 0; i < sortedList.Count; i++)
                    {
                        var item = lstboxIP.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                        if (item != null) item.Foreground = _hosts[i].getOnline() ? Brushes.Green : Brushes.Red;
                    }
                    lstboxIP.SelectedIndex = 0;
                }, 150);
            });
        }
        #endregion

        #region Initialization
        public MainWindow()
        {
            InitializeComponent();

            InitializeGrid();
            // Async Task
            Initialize();

            MainChart.DataContext = _values;
        }

        private void InitializeGrid()
        {
            _dataTableNetwork = new DataTable("Network");
            _dataTableNetwork.Columns.Add(new DataColumn("Hostname", typeof(string)));
            _dataTableNetwork.Columns.Add(new DataColumn("IP", typeof(string)));
            _dataTableNetwork.Columns.Add(new DataColumn("Download", typeof(string)));
            _dataTableNetwork.Columns.Add(new DataColumn("Upload", typeof(string)));
            _dataTableNetwork.Columns.Add(new DataColumn("Download Limit", typeof(string)));
            _dataTableNetwork.Columns.Add(new DataColumn("Upload Limit", typeof(string)));
            _dataTableNetwork.Columns.Add(new DataColumn("Total Recv", typeof(string)));
            _dataTableNetwork.Columns.Add(new DataColumn("Total Sent", typeof(string)));
            dataGridInfo.ItemsSource = _dataTableNetwork.AsDataView();

            var columns = dataGridInfo.Columns;
            columns[0].Width = 110;
            columns[1].Width = 100;
            columns[2].Width = 90;
            columns[3].Width = 90;
            columns[4].Width = 110;
            columns[5].Width = 100;
            columns[6].Width = 90;
        }

        private string Conversion(double value)
        {
           return ConvertToKbps(value) + " kB/s";
        }

        private string BitsToBytes(double value)
        {
            return value / 8000 + " KB";
        }

        public double ConvertToKbps(double value)
        {
            // 1024 bytes in kilobyte Round 2 Decimals
            return Math.Round(value / 1024, 2);
        }

        public void Graphing(List<NetworkActivity> sortedList)
        {
            _secondCounter += 1;
            if (_secondCounter > 20)
                _values.RemoveAt(0);
            var chart = new ChartPlot {Key = _secondCounter};

            foreach (var sl in sortedList)
            {
                if (sl.Hostname.Equals("CreeD-PC"))
                {
                    chart1.Title = sl.Hostname;
                    chart.Value1 = sl.Download / 1024;
                }
                else if (sl.Hostname.Equals("Zay-PC"))
                {
                    chart2.Title = sl.Hostname;
                    chart.Value2 = sl.Download / 1024;
                }
            }
            _values.Add(chart);
            MainChart.DataContext = _values;
        }

        public List<NetworkActivity> SetHostLimits(List<NetworkActivity> sortedList)
        {
            foreach (var sl in sortedList)
            {
                try
                {
                    sl.Hostname = _hosts.First(item => item.getIP() == sl.IP).getComment();
                }
                catch (Exception)
                {
                    sl.Hostname = "Unknown";
                }
            }
            foreach (var q in _queues)
            {
                sortedList.First(item => item.IP == q.getIP()).DownloadLimit = q.getDown();
                sortedList.First(item => item.IP == q.getIP()).UploadLimit = q.getUp();
            }
            return sortedList;
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            richTextBox.Document.Blocks.Clear();
            _dataTableNetwork.Clear();
            var sortedList = NetworkActivity.getNeworkActivity().OrderByDescending(o => o.Download).ToList();
            sortedList = SetHostLimits(sortedList);
            foreach (var host in sortedList)
            {
                object[] values = { host.Hostname , host.IP, Conversion(host.Download), Conversion(host.Upload), BitsToBytes(host.DownloadLimit), BitsToBytes(host.UploadLimit), host.TotalRecv, host.TotalSend };
                _dataTableNetwork.Rows.Add(values);
            }
            Graphing(sortedList);

            DelayedExecutionService.DelayedExecute(() =>
            {
                if (dataGridInfo.Items.Count <= 0) return;
                for (var i = 0; i < dataGridInfo.Items.Count; i++)
                {
                    dataGridInfo.SelectedIndex = -1;
                    if (ConvertToKbps(sortedList[i].Download) > 20 || ConvertToKbps(sortedList[i].Upload) > 20)
                    {
                        var row2 = dataGridInfo.GetContainerFromItem(dataGridInfo.Items[i]) as Xceed.Wpf.DataGrid.DataRow;
                        if (row2 != null) row2.Foreground = Brushes.Red;
                    }
                    else if (ConvertToKbps(sortedList[i].Download) > 0 || ConvertToKbps(sortedList[i].Upload) > 0)
                    {
                        var row2 = dataGridInfo.GetContainerFromItem(dataGridInfo.Items[i]) as Xceed.Wpf.DataGrid.DataRow;
                        if (row2 != null) row2.Foreground = Brushes.Green;
                    }
                    else
                    {
                        var row2 = dataGridInfo.GetContainerFromItem(dataGridInfo.Items[i]) as Xceed.Wpf.DataGrid.DataRow;
                        if (row2 != null) row2.Foreground = Brushes.Gray;
                    }
                }
            }, 50);
        }

        public async void Initialize()
        {
            try
            {
                await Task.Run(() =>
                {
                    if (!Login()) return;
                    GetLeases();
                    GetQueues();
                });
            }
            catch (Exception ex)
            {
                UpdateDebugLog(ex.Message);
            }

            var dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 1);
            dispatcherTimer.Start();
        }
        #endregion

        #region Procedures / Functions

        public static class DelayedExecutionService
        {
            public static void DelayedExecute(Action action, int delay = 500)
            {
                var dispatcherTimer = new System.Windows.Threading.DispatcherTimer();

                EventHandler handler = null;
                handler = (sender, e) =>
                {
                    dispatcherTimer.Tick -= handler;
                    dispatcherTimer.Stop();
                    action();
                };

                dispatcherTimer.Tick += handler;
                dispatcherTimer.Interval = TimeSpan.FromMilliseconds(delay);
                dispatcherTimer.Start();
            }
        }

        public void LimitIp(string ip, string comment, int down, int up)
        {
            _mikrotik.Send("/queue/simple/add");
            _mikrotik.Send("=name=" + comment);
            _mikrotik.Send("=target=" + ip);
            _mikrotik.Send("=max-limit=" + (up * 8) + "k/" + (down * 8) + "k", true);
            foreach (var apiOutput in _mikrotik.Read())
            {
                UpdateDebugLog(apiOutput + "\n");
            }
        }

        public void GetLeases()
        {
            _hosts = new List<Host>();
            _mikrotik.Send("/ip/dhcp-server/lease/print", true);
            foreach (var apiOutput in _mikrotik.Read())
            {
                if (apiOutput.Contains("address"))
                {
                    var alive = false;

                    var stringSplit = apiOutput.Split('=');
                    var lstDhcpInfo = new List<KeyValuePair<string, string>>();
                    for (var i = 1; i < stringSplit.Length-1; i+=2)
                        lstDhcpInfo.Add(new KeyValuePair<string, string>(stringSplit[i], stringSplit[i+1]));

                    // Gets IP address from Key Value Pair
                    var ipAddress = (lstDhcpInfo.First(kvp => kvp.Key == "address").Value);
                    // Checks that there is a Comment
                    var comment = apiOutput.Contains("comment") ? (lstDhcpInfo.First(kvp => kvp.Key == "comment").Value) : "Unknown";

                    // Checks if the lease expires meaning the host is alive
                    if (apiOutput.Contains("expires-after"))
                        alive = true;

                    _hosts.Add(new Host(ipAddress, comment, alive));
                }
                UpdateDebugLog(apiOutput + "\n");
            }
            UpdateHosts();
        }

        private void GetQueues()
        {
            _queues = new List<Queue>();
            _mikrotik.Send("/queue/simple/print", true);
            foreach (var apiOutput in _mikrotik.Read())
            {
                if (apiOutput.Contains("id"))
                {
                    var stringSplit = apiOutput.Split('=');
                    var lstDhcpInfo = new List<KeyValuePair<string, string>>();
                    for (var i = 1; i < stringSplit.Length - 1; i += 2)
                        lstDhcpInfo.Add(new KeyValuePair<string, string>(stringSplit[i], stringSplit[i + 1]));

                    // Gets IP address from Key Value Pair
                    var id = (lstDhcpInfo.First(kvp => kvp.Key == ".id").Value);
                    // remove the /32 as address subnet not needed
                    var target = (lstDhcpInfo.First(kvp => kvp.Key == "target").Value).Replace("/32","");
                    // Split Up and Down speed
                    var upDown = lstDhcpInfo.First(kvp => kvp.Key == "max-limit").Value.Split('/');

                    _queues.Add(new Queue(id, target, int.Parse(upDown[0]), int.Parse(upDown[1])));
                }
                UpdateDebugLog(apiOutput + "\n");
            }
            UpdateQueues();
        }

        private void RemoveQueue(string id)
        {
            _mikrotik.Send("/queue/simple/remove");
            _mikrotik.Send("=.id=" + id, true);
            foreach (var h in _mikrotik.Read())
            {
                UpdateDebugLog(h + "\n");
            }
            GetQueues();
            if (lstboxQueue.Items.Count <= 0)
                btnRemove.IsEnabled = false;
            else lstboxQueue.SelectedIndex = 0;
        }
        #endregion

        #region Button Clicks
        private void btnSetSpeed_Click(object sender, RoutedEventArgs e)
        {
            richTextBox.Document.Blocks.Clear();
            string ip = _hosts[lstboxIP.SelectedIndex].getIP();

            for (int i = 0; i < _queues.Count; i++)
            {
                if (_queues[i].getIP().Contains(ip))
                {
                    _mikrotik.Send("/queue/simple/remove");
                    _mikrotik.Send("=.id=" + _queues[i].getID(), true);
                    foreach (string h in _mikrotik.Read())
                    {
                        UpdateDebugLog(h + "\n");
                    }
                    break;
                }
            }
            LimitIp(ip, _hosts[lstboxIP.SelectedIndex].getComment(), int.Parse(txtDown.Text), int.Parse(txtUp.Text));
            GetQueues();
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            RemoveQueue(_queues[lstboxQueue.SelectedIndex].getID());
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (!btnLogin.Content.Equals("Login"))
            {
                txtPassword.IsEnabled = true;
                txtUsername.IsEnabled = true;
                txtIP.IsEnabled = true;
                _queues.Clear();
                _hosts.Clear();
                UpdateHosts();
                UpdateQueues();
                btnLogin.Content = "Login";
            }
            else
            {
                Initialize();
            }
        }
        #endregion

        #region UI Events
        private void chkDebug_Click(object sender, RoutedEventArgs e)
        {
            if (chkDebug.IsChecked != null && (bool)chkDebug.IsChecked)
                richTextBox.Visibility = Visibility.Visible;
            else richTextBox.Visibility = Visibility.Hidden;
        }

        private void lstboxQueue_GotFocus(object sender, RoutedEventArgs e)
        {
            btnSetSpeed.IsEnabled = false;
            btnRemove.IsEnabled = true;
        }

        private void lstboxQueue_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnSetSpeed.IsEnabled = false;
            if (lstboxQueue.SelectedIndex <= -1) return;
            txtDown.Text = (_queues[lstboxQueue.SelectedIndex].getDown() / 8000).ToString();
            txtUp.Text = (_queues[lstboxQueue.SelectedIndex].getUp() / 8000).ToString();
            for (var i = 0; i < _hosts.Count; i++)
            {
                if (!_queues[lstboxQueue.SelectedIndex].getIP().Contains(_hosts[i].getIP())) continue;
                lstboxIP.SelectedIndex = i;
                btnSetSpeed.IsEnabled = true;
                break;
            }
        }

        private void lstboxIP_GotFocus(object sender, RoutedEventArgs e)
        {
            btnSetSpeed.IsEnabled = true;
            btnRemove.IsEnabled = false;
        }

        private void lstboxIP_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnRemove.IsEnabled = false;
            if (lstboxIP.SelectedIndex <= -1) return;
            for (var i = 0; i < _queues.Count; i++)
            {
                if (!_hosts[lstboxIP.SelectedIndex].getIP().Contains(_queues[i].getIP())) continue;
                lstboxQueue.SelectedIndex = i;
                btnRemove.IsEnabled = true;
                break;
            }
        }
        #endregion
    }
}
