using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace WpfApplication2
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

        public void CheckUpdate(String message)
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
                CheckUpdate(ex.Message);
            }
        }
        #endregion

        #region Procedures / Functions
        public string RandomNameGen()
        {
            String output = "";
            string a = "abcdefghijklmnopqrstuvwxyz1234567890";
            Random r = new Random();
            for (int i = 0; i < 10; i++)
                output += a[r.Next(35)];
            return output;
        }

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

        public void LimitIP(String IP, String Comment, int Down, int Up)
        {
            mikrotik.Send("/queue/simple/add");
            mikrotik.Send("=name=" + Comment);
            mikrotik.Send("=target=" + IP);
            mikrotik.Send("=max-limit=" + (Up * 8) + "k/" + (Down * 8) + "k", true);
            foreach (string h in mikrotik.Read())
            {
                CheckUpdate(h + "\n");
            }
        }

        public void GetLeases()
        {
            Hosts = new List<Host>();
            mikrotik.Send("/ip/dhcp-server/lease/print", true);
            foreach (string h in mikrotik.Read())
            {
                if (h.Contains("address"))
                {
                    string temp = h.Remove(0, h.IndexOf("address=") + 8);
                    if (h.Contains("comment") && h.Contains("expires-after"))
                        Hosts.Add(new Host(temp.Substring(0, temp.IndexOf("=")), h.Substring(h.IndexOf("comment") + 8), true));
                    else if (h.Contains("expires-after"))
                        Hosts.Add(new Host(temp.Substring(0, temp.IndexOf("=")), "Unknown", true));
                    else if (h.Contains("comment"))
                        Hosts.Add(new Host(temp.Substring(0, temp.IndexOf("=")), h.Substring(h.IndexOf("comment") + 8), false));
                    else
                        Hosts.Add(new Host(temp.Substring(0, temp.IndexOf("=")), "Unknown",false));

                }
                CheckUpdate(h + "\n");
            }
            UpdateHosts();
        }

        private void GetQueues()
        {
            Queues = new List<Queue>();
            mikrotik.Send("/queue/simple/print", true);
            foreach (string h in mikrotik.Read())
            {
                if (h.Contains("id"))
                {
                    string id = h.Remove(0, h.IndexOf(".id.") + 9);
                    id = id.Substring(0, id.IndexOf("="));

                    string target = h.Remove(0, h.IndexOf("target") + 7);
                    target = target.Substring(0, target.IndexOf("=") - 3);

                    string Down = h.Remove(0, h.IndexOf("max-limit") + 10);
                    Down = Down.Substring(0, Down.IndexOf("/"));

                    string Up = h.Remove(0, h.IndexOf("max-limit") + 10);
                    Up = Up.Remove(0, Up.IndexOf("/") + 1);
                    Up = Up.Substring(0, Up.IndexOf("="));
                    Queues.Add(new Queue(id, target, int.Parse(Up), int.Parse(Down)));
                }
                CheckUpdate(h + "\n");
            }
            UpdateQueues();
        }

        private void RemoveQueue(String ID)
        {
            mikrotik.Send("/queue/simple/remove");
            mikrotik.Send("=.id=" + ID, true);
            foreach (string h in mikrotik.Read())
            {
                CheckUpdate(h + "\n");
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
                        CheckUpdate(h + "\n");
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
