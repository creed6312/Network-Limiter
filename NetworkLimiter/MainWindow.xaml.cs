using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace NetworkLimiter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        #region Variables
        private MK mikrotik;
        private List<Host> Hosts;
        private List<Queue> Queues;
        #endregion

        #region Delegate Callbacks
        public bool Login()
        {
            bool Success = false;
            Dispatcher.Invoke((Action)delegate ()
            {
                mikrotik = new MK(txtIP.Text);
                if (!mikrotik.Login(txtUsername.Text, txtPassword.Password))
                {
                    MessageBox.Show("Failed to Login.");
                    mikrotik.Close();
                    txtPassword.IsEnabled = true;
                    txtUsername.IsEnabled = true;
                    txtIP.IsEnabled = true;
                    btnLogin.Content = "Login";
                    return ;
                }
                Success = true;
                txtPassword.IsEnabled = false;
                txtUsername.IsEnabled = false;
                txtIP.IsEnabled = false;
                btnLogin.Content = "Log Out";
            });
            return Success;
        }

        public void UpdateDebugLog(string message)
        {
            Dispatcher.Invoke((Action)delegate ()
            {
                if ((bool) chkDebug.IsChecked)
                    richTextBox.AppendText(message);
            });
        }

        public void UpdateQueues()
        {
            for (int k = 0; k < Hosts.Count; k++)
                for (int m = 0; m < Queues.Count; m++)
                    if (Hosts[k].getIP().Equals(Queues[m].getIP()))
                    {
                        Queues[m].setComment(Hosts[k].getComment());
                        Queues[m].setOnline(Hosts[k].getOnline());
                    }

            Dispatcher.Invoke((Action)delegate ()
            {
                lstboxQueue.Items.Clear();
                for (int i = 0; i < Queues.Count; i++)
                {
                    lstboxQueue.Items.Add(Queues[i].getIP() + " - " + Queues[i].getComment());
                }

                DelayedExecutionService.DelayedExecute(() =>
                {
                    for (int i = 0; i < Queues.Count; i++)
                    {
                        ListBoxItem item = lstboxQueue.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                        if (Queues[i].getOnline())
                            item.Foreground = Brushes.Green;
                        else item.Foreground = Brushes.Red;
                    }
                    lstboxQueue.SelectedIndex = 0;
                }, 200);
            });
        }

        public void UpdateHosts()
        {
            Dispatcher.Invoke((Action)delegate ()
            {
                lstboxIP.Items.Clear();
                List<Host> SortedList = Hosts.OrderBy(o => o.getIP()).ToList();
                Hosts = SortedList;
                for (int i = 0; i < SortedList.Count; i++)
                    lstboxIP.Items.Add(SortedList[i].getIP() + " - " + SortedList[i].getComment());

                DelayedExecutionService.DelayedExecute(() =>
                {
                    for (int i = 0; i < SortedList.Count; i++)
                    {
                        ListBoxItem item = lstboxIP.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                        if (Hosts[i].getOnline())
                            item.Foreground = Brushes.Green;
                        else item.Foreground = Brushes.Red;
                    }
                    lstboxIP.SelectedIndex = 0;
                }, 200);
            });
        }
        #endregion

        #region Initialization
        public MainWindow()
        {
            InitializeComponent();

            // Async Task
            Initialize();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            richTextBox.Document.Blocks.Clear();
            dataGridInfo.ItemsSource = NetworkActivity.getNeworkActivity();
            var column = dataGridInfo.Columns;
            column[0].Width = 95;
            column[1].Width = 78;
            column[2].Width = 78;
            column[3].Width = 80;
            column[4].Width = 80;
        }

        public async void Initialize()
        {
            try
            {
                await Task.Run(() =>
                {
                    if (Login())
                    {
                        GetLeases();
                        GetQueues();
                    }
                });
            }
            catch (Exception ex)
            {
                UpdateDebugLog(ex.Message);
            }

            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
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

        public void LimitIP(string IP, string Comment, int Down, int Up)
        {
            mikrotik.Send("/queue/simple/add");
            mikrotik.Send("=name=" + Comment);
            mikrotik.Send("=target=" + IP);
            mikrotik.Send("=max-limit=" + (Up * 8) + "k/" + (Down * 8) + "k", true);
            foreach (string apiOutput in mikrotik.Read())
            {
                UpdateDebugLog(apiOutput + "\n");
            }
        }

        public void GetLeases()
        {
            Hosts = new List<Host>();
            mikrotik.Send("/ip/dhcp-server/lease/print", true);
            foreach (string apiOutput in mikrotik.Read())
            {
                if (apiOutput.Contains("address"))
                {
                    string comment = "";
                    bool alive = false;

                    string[] stringSplit = apiOutput.Split('=');
                    var lstDHCPInfo = new List<KeyValuePair<string, string>>();
                    for (int i = 1; i < stringSplit.Length-1; i+=2)
                        lstDHCPInfo.Add(new KeyValuePair<string, string>(stringSplit[i], stringSplit[i+1]));

                    // Gets IP address from Key Value Pair
                    string ipAddress = (lstDHCPInfo.First(kvp => kvp.Key == "address").Value);
                    // Checks that there is a Comment
                    if (apiOutput.Contains("comment"))
                        comment = (lstDHCPInfo.First(kvp => kvp.Key == "comment").Value);
                    else
                        comment = "Unknown";

                    // Checks if the lease expires meaning the host is alive
                    if (apiOutput.Contains("expires-after"))
                        alive = true;

                    Hosts.Add(new Host(ipAddress, comment, alive));
                }
                UpdateDebugLog(apiOutput + "\n");
            }
            UpdateHosts();
        }

        private void GetQueues()
        {
            Queues = new List<Queue>();
            mikrotik.Send("/queue/simple/print", true);
            foreach (string apiOutput in mikrotik.Read())
            {
                if (apiOutput.Contains("id"))
                {
                    string[] stringSplit = apiOutput.Split('=');
                    var lstDHCPInfo = new List<KeyValuePair<string, string>>();
                    for (int i = 1; i < stringSplit.Length - 1; i += 2)
                        lstDHCPInfo.Add(new KeyValuePair<string, string>(stringSplit[i], stringSplit[i + 1]));

                    // Gets IP address from Key Value Pair
                    string id = (lstDHCPInfo.First(kvp => kvp.Key == ".id").Value);
                    // remove the /32 as address subnet not needed
                    string target = (lstDHCPInfo.First(kvp => kvp.Key == "target").Value).Replace("/32","");
                    // Split Up and Down speed
                    string[] UpDown = lstDHCPInfo.First(kvp => kvp.Key == "max-limit").Value.Split('/');

                    Queues.Add(new Queue(id, target, int.Parse(UpDown[0]), int.Parse(UpDown[1])));
                }
                UpdateDebugLog(apiOutput + "\n");
            }
            UpdateQueues();
        }

        private void RemoveQueue(string ID)
        {
            mikrotik.Send("/queue/simple/remove");
            mikrotik.Send("=.id=" + ID, true);
            foreach (string h in mikrotik.Read())
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
            string ip = Hosts[lstboxIP.SelectedIndex].getIP();

            for (int i = 0; i < Queues.Count; i++)
            {
                if (Queues[i].getIP().Contains(ip))
                {
                    mikrotik.Send("/queue/simple/remove");
                    mikrotik.Send("=.id=" + Queues[i].getID(), true);
                    foreach (string h in mikrotik.Read())
                    {
                        UpdateDebugLog(h + "\n");
                    }
                    break;
                }
            }
            LimitIP(ip, Hosts[lstboxIP.SelectedIndex].getComment(), int.Parse(txtDown.Text), int.Parse(txtUp.Text));
            GetQueues();
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            RemoveQueue(Queues[lstboxQueue.SelectedIndex].getID());
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (!btnLogin.Content.Equals("Login"))
            {
                txtPassword.IsEnabled = true;
                txtUsername.IsEnabled = true;
                txtIP.IsEnabled = true;
                Queues.Clear();
                Hosts.Clear();
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
            if ((bool)chkDebug.IsChecked)
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
            if (lstboxQueue.SelectedIndex > -1)
            {
                txtDown.Text = (Queues[lstboxQueue.SelectedIndex].getDown() / 8000).ToString();
                txtUp.Text = (Queues[lstboxQueue.SelectedIndex].getUp() / 8000).ToString();
                for (int i = 0; i < Hosts.Count; i++)
                {
                    if (Queues[lstboxQueue.SelectedIndex].getIP().Contains(Hosts[i].getIP()))
                    {
                        lstboxIP.SelectedIndex = i;
                        btnSetSpeed.IsEnabled = true;
                        break;
                    }
                }
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
            if (lstboxIP.SelectedIndex > -1)
            {
                for (int i = 0; i < Queues.Count; i++)
                {
                    if (Hosts[lstboxIP.SelectedIndex].getIP().Contains(Queues[i].getIP()))
                    {
                        lstboxQueue.SelectedIndex = i;
                        btnRemove.IsEnabled = true;
                        break;
                    }
                }
            }
        }
        #endregion

    }
}
