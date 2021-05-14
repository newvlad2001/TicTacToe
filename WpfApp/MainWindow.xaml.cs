using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Helper;

namespace ClientWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Thread _handlerProc;
        private Socket _clientSocket;
        private bool _isClosing = false;
        private string _sign;
        private bool _isYourTurn;
        private bool _isStarted;

        private string Sign
        {
            set
            {
                _sign = value;
                NameLabel.Content = "You are playing: " + value;
            }

            get => _sign;
        }

        private bool IsYourTurn
        {
            set
            {
                _isYourTurn = value;
                TurnLabel.Content = value ? "Your turn" : "Opponent turn";
            }

            get => _isYourTurn;
        }

        public MainWindow()
        {
            InitializeComponent();
            IsYourTurn = false;
        }

        private void ConnectButtonOnClick(object sender, RoutedEventArgs e)
        {
            if (!IsFieldsFilled())
            {
                MessageBox.Show("Please, fill all input fields", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (IsFieldsFilledCorrectly())
            {
                CreateClientSocket();
            }
        }

        private bool IsFieldsFilledCorrectly()
        {
            string value = IPTextBox.Text;
            bool isValid = IPAddress.TryParse(value, out var address);
            if (!isValid)
            {
                MessageBox.Show("Error with IP", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                IPTextBox.Text = "";
                return false;
            }

            value = PortTextBox.Text;
            isValid = int.TryParse(value, out _);
            if (!isValid)
            {
                MessageBox.Show("Error with port", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                PortTextBox.Text = "";
                return false;
            }

            return true;
        }

        private bool IsFieldsFilled()
        {
            if (IPTextBox.Text == "" || PortTextBox.Text == "")
            {
                return false;
            }

            return true;
        }

        private void PortTextBoxOnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.Text = new string
                (
                    textBox.Text
                        .Where
                        (ch =>
                            ch == '0' || ch == '1' || ch == '2' || ch == '3' ||
                            ch == '4' || ch == '5' || ch == '6' || ch == '7' ||
                            ch == '8' || ch == '9'
                        )
                        .ToArray()
                );
                textBox.SelectionStart = e.Changes.First().Offset + 1;
                textBox.SelectionLength = 0;
            }
        }

        private void CreateClientSocket()
        {
            IPAddress ipAddress = IPAddress.Parse(IPTextBox.Text);
            int port = int.Parse(PortTextBox.Text);

            IPEndPoint ipPoint = new IPEndPoint(ipAddress, port);
            _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                _clientSocket.Connect(ipPoint);
            }
            catch (SocketException ex)
            {
                MessageBox.Show($"It is impossible to connect to the server. {ex.Message}", "Connection error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            catch (ObjectDisposedException ex)
            {
                MessageBox.Show($"It is impossible to connect to the server. {ex.Message}", "Connection error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SetStatusLabel(true);
            _handlerProc = new Thread(ReceivingFromServer);
            _handlerProc.Start();
        }

        private void ReceivingFromServer(object obj)
        {
            string data;
            while ((data = Helper.TransferHelper.Receive(_clientSocket)) != null)
            {
                MsgProcessor(data);
            }

            if (!_isClosing)
            {
                MessageBox.Show("Server was disconnected.", "Server error", MessageBoxButton.OK, MessageBoxImage.Error);
                Dispatcher.Invoke(() =>
                    {
                        ConnectButton.IsEnabled = true;
                        SetGameEnabled(false);
                        SetStatusLabel(false);
                    }
                );
            }
        }

        private void SetGameEnabled(bool isConnected)
        {
            PlayGrid.IsEnabled = isConnected;
            RestartButton.IsEnabled = isConnected;
        }

        private void SetStatusLabel(bool isConnected)
        {
            StatusLabel.Content = isConnected ? "Connected" : "Disconnected";
            StatusLabel.Foreground = isConnected ? Brushes.Green : Brushes.Red;
            ConnectButton.IsEnabled = !isConnected;
        }

        private void GameButtonOnClick(object sender, RoutedEventArgs e)
        {
            if (IsYourTurn)
            {
                Button button = (Button) sender;
                button.IsEnabled = false;
                Helper.TransferHelper.Send("button/" + button.Name + '/' + Sign, _clientSocket);
                IsYourTurn = false;
            }
            else
            {
                MessageBox.Show("Wait for your turn", "Is not your turn", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void MsgProcessor(string data)
        {
            string[] splittedData = data.Split('/');
            switch (splittedData[0])
            {
                case "button":
                    Dispatcher.Invoke(() =>
                        {
                            if (PlayGrid.FindName(splittedData[1]) is Button pressedButton)
                            {
                                pressedButton.Content = splittedData[2];
                                pressedButton.IsEnabled = false;
                            }
                        }
                    );
                    break;
                case "playerSign":
                    Dispatcher.Invoke(() => { Sign = splittedData[1]; });
                    break;
                case "turn":
                    Dispatcher.Invoke(() => { IsYourTurn = bool.Parse(splittedData[1]); });
                    break;
                case "start":
                    Dispatcher.Invoke(() =>
                    {
                        SetGameEnabled(true);
                        if (_isStarted) Restart();
                        _isStarted = true;
                    });
                    break;
                case "win":
                    Dispatcher.Invoke(() =>
                    {
                        SetGameEnabled(false);
                        MessageBox.Show(splittedData[1], "Game ended", MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        TurnLabel.Content = splittedData[1];
                        RestartButton.IsEnabled = true;
                    });
                    break;
                case "draw":
                    Dispatcher.Invoke(() =>
                    {
                        SetGameEnabled(false);
                        MessageBox.Show("DRAW", "Game ended", MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        TurnLabel.Content = "DRAW";
                        RestartButton.IsEnabled = true;
                    });
                    break;
                case "disconnected":
                    Dispatcher.Invoke(() =>
                    {
                        SetGameEnabled(false);
                        MessageBox.Show("Player disconnected", "Game paused", MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    });
                    break;
                case "restart":
                    Dispatcher.Invoke(() => { Restart(); });
                    break;
                default:
                    Console.WriteLine(data);
                    break;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _isClosing = true;
            _clientSocket.Shutdown(SocketShutdown.Send);
            _handlerProc.Join();
        }

        private void Restart()
        {
            ArrayList list = new ArrayList(PlayGrid.Children);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is Button button)
                {
                    button.IsEnabled = true;
                    button.Content = "";
                }
            }
        }

        private void RestartButton_OnClick(object sender, RoutedEventArgs e)
        {
            TransferHelper.Send("restart", _clientSocket);
        }
    }
}