using System;
using System.Collections.Generic;
using System.Data;
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
using System.Windows.Shapes;
using MySql.Data.MySqlClient;
using MaterialDesignColors;
using MaterialDesignThemes;
using System.Windows.Media.Animation;
using System.Threading;
using Tulpep.NotificationWindow;
using Microsoft.Win32;
using f = System.Windows.Forms;
using System.Net.Sockets;
using Abp.Application.Navigation;
using System.IO;



namespace Skooby
{
    /// <summary>
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private delegate void ChatEvent(string content, string clr);
        private ChatEvent _addMessage;
        private Socket _serverSocket;
        private Thread listenThread;
        private string _host = "127.0.0.1";
        private int _port = 2222;
        public f.ContextMenuStrip userMenu = new f.ContextMenuStrip();

        string id;
        MySqlConnection conn = new MySqlConnection(DBConnection.Connect());

        List<MyNotes> myNotes = new List<MyNotes>();
        public class MyItem
        {
            public string id { get; set; }
            public string name { get; set; }
            public string desk { get; set; }
            public string date { get; set; }
            public string time { get; set; }
            public string importance { get; set; }
        }

        private class MyNotes
        {
            public string name;
            public string date;
            public string time;
            public string importance;
            public string desk;
        }

        public static bool IsCalendarOpen = false;
        Callendar callendar = new Callendar();

        public Window1()
        {
            InitializeComponent();

            _addMessage = new ChatEvent(AddMessage);
            userMenu = new f.ContextMenuStrip();
            f.ToolStripMenuItem PrivateMessageItem = new f.ToolStripMenuItem();


            PrivateMessageItem.Text = "Личное сообщение";
            PrivateMessageItem.Click += delegate
            {
                if (userList.SelectedItems.Count > 0)
                {
                    messageData.Text = $"\"{userList.SelectedItem} ";
                }
            };
            userMenu.Items.Add(PrivateMessageItem);
           
           MenuItem SendFileItem = new MenuItem()
            {
                Header = "Отправить файл"
            };
            SendFileItem.Click += delegate
            {
                if (userList.SelectedItems.Count == 0)
                {
                    return;
                }
                OpenFileDialog ofp = new OpenFileDialog();
                ofp.ShowDialog();
                if (!File.Exists(ofp.FileName))
                {
                    MessageBox.Show($"Файл {ofp.FileName} не найден!");
                    return;
                }
                FileInfo fi = new FileInfo(ofp.FileName);
                byte[] buffer = File.ReadAllBytes(ofp.FileName);
                Send($"#sendfileto|{userList.SelectedItem}|{buffer.Length}|{fi.Name}");//g
                Send(buffer);


            };

            //  userMenu.Items.Add(SendFileItem);
            
            //userList.ContextMenu.Items.Add(SendFileItem);
        }

        private void AddMessage(string Content, string Color = "Black")
        {
            
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(_addMessage, Content, Color);
                return;
            }
           // chatBox.CaretPosition = chatBox.Document.ContentEnd;
            
            chatBox.SelectionBrush = new SolidColorBrush(getColor(Color));
            chatBox.AppendText(Content + Environment.NewLine);
        }

        private Color getColor(string text)
        {
            //govno
            
            return Colors.Black;

        }
        public void Send(byte[] buffer)
        {
            try
            {
                _serverSocket.Send(buffer);
            }
            catch { }
        }
        public void Send(string Buffer)
        {
            try
            {
                _serverSocket.Send(Encoding.Unicode.GetBytes(Buffer));
            }
            catch { }
        }
        public void handleCommand(string cmd)
        {

            string[] commands = cmd.Split('#');
            int countCommands = commands.Length;
            for (int i = 0; i < countCommands; i++)
            {
                try
                {
                    string currentCommand = commands[i];
                    if (string.IsNullOrEmpty(currentCommand))
                        continue;
                    if (currentCommand.Contains("setnamesuccess"))
                    {


                        //Из-за того что программа пыталась получить доступ к контролам из другого потока вылетал эксепщен и поля не разблокировались

                        Dispatcher.Invoke((f.MethodInvoker)delegate
                        {
                            AddMessage($"Добро пожаловать, {nicknameData.Text}");
                            nameData.Content = nicknameData.Text;
                            chatBox.IsEnabled = true;
                            messageData.IsEnabled = true;
                            userList.IsEnabled = true;
                            nicknameData.IsEnabled = false;
                            enterChat.IsEnabled = false;
                        });
                        continue;
                    }
                    if (currentCommand.Contains("setnamefailed"))
                    {
                        AddMessage("Неверный ник!");
                        continue;
                    }
                    if (currentCommand.Contains("msg"))
                    {
                        string[] Arguments = currentCommand.Split('|');
                        AddMessage(Arguments[1], Arguments[2]);
                        continue;
                    }

                    if (currentCommand.Contains("userlist"))
                    {
                        string[] Users = currentCommand.Split('|')[1].Split(',');
                        int countUsers = Users.Length;
                        userList.Dispatcher.Invoke((f.MethodInvoker)delegate { userList.Items.Clear(); });
                        for (int j = 0; j < countUsers; j++)
                        {
                            userList.Dispatcher.Invoke((f.MethodInvoker)delegate { userList.Items.Add(Users[j]); });
                        }
                        continue;

                    }
                    if (currentCommand.Contains("gfile"))
                    {
                        string[] Arguments = currentCommand.Split('|');
                        string fileName = Arguments[1];
                        string FromName = Arguments[2];
                        string FileSize = Arguments[3];
                        string idFile = Arguments[4];
                        f.DialogResult Result = (f.DialogResult)MessageBox.Show($"Вы хотите принять файл {fileName} размером {FileSize} от {FromName}", "Файл", MessageBoxButton.YesNo) ;
                        if (Result == f.DialogResult.Yes)
                        {
                            Thread.Sleep(1000);
                            Send("#yy|" + idFile);
                            byte[] fileBuffer = new byte[int.Parse(FileSize)];
                            _serverSocket.Receive(fileBuffer);
                            File.WriteAllBytes(fileName, fileBuffer);
                            MessageBox.Show($"Файл {fileName} принят.");
                        }
                        else
                            Send("nn");
                        continue;
                    }

                }
                catch (Exception exp) { Console.WriteLine("Error with handleCommand: " + exp.Message); }

            }


        }
        public void listner()
        {
            try
            {
                while (_serverSocket.Connected)
                {
                    byte[] buffer = new byte[2048];
                    int bytesReceive = _serverSocket.Receive(buffer);
                    handleCommand(Encoding.Unicode.GetString(buffer, 0, bytesReceive));
                }
            }
            catch
            {
                MessageBox.Show("Связь с сервером прервана");
                
            }
        }

        private void ConnChat()
        {
            System.Net.IPAddress temp = System.Net.IPAddress.Parse(_host);
            _serverSocket = new Socket(temp.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.Connect(new System.Net.IPEndPoint(temp, _port));
            if (_serverSocket.Connected)
            {
                enterChat.IsEnabled = true;
                AddMessage("Связь с сервером установлена.");
                listenThread = new Thread(listner);
                listenThread.IsBackground = true;
                listenThread.Start();

            }
            else
                AddMessage("Связь с сервером не установлена.");
        }






        bool IsOpenVievNotes,IsOpenVievProfil = false;

        private  void ListNotes()
        {
            
            myNotes.Clear();
            try
            {
                conn.Open();
                string sqlExpression = String.Format("SELECT id FROM users WHERE mail = '{0}'",TextBlockName.Text.Trim());
             
                MySqlCommand command = new MySqlCommand(sqlExpression, conn);
                string id = command.ExecuteScalar().ToString();
                sqlExpression = String.Format("SELECT * FROM notes WHERE id_user = '{0}' AND status = '1'",id);
         
                command.CommandText = sqlExpression;
                MySqlDataReader reader = command.ExecuteReader();
               
           
                while (reader.Read()) myNotes.Add(new MyNotes { name = Convert.ToString(reader["name"]), date = Convert.ToString(reader["date"]), time = Convert.ToString(reader["time"]), desk = Convert.ToString(reader["description"]) , importance = Convert.ToString(reader["importance"])});
              
            }
            catch(Exception ex)
            {
              //  MessageBox.Show(ex.Message);
            }
            finally
            {
                conn.Close();
                
            }
        
        }
        public void TimerStart()
        {
            try
            {

                ListNotes();
                PopupNotifier popup = new PopupNotifier();
                popup.Image = Properties.Resources.popup;
                popup.ImageSize = new System.Drawing.Size(100, 100);
                popup.HeaderColor = System.Drawing.Color.FromArgb(100, 1, 1, 33);
                TextBlockTime.Text = MainWindow.GetNetworkTime().ToLongTimeString();
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = new TimeSpan(0, 0, 10);
                timer.IsEnabled = true;
                timer.Tick += (o, t) =>
                {
                    Console.WriteLine("tuta");
                    for (int i = 0; i < myNotes.Count; i++)
                    {
                        Console.WriteLine("Tut");
                        if (DateTime.Parse(myNotes[i].date).ToShortDateString() == MainWindow.GetNetworkTime().ToShortDateString() && DateTime.Parse(myNotes[i].time).Minute == MainWindow.GetNetworkTime().Minute)
                        {
                            if (myNotes[i].importance == "Очень важно")
                            {
                                popup.TitleText = myNotes[i].name;
                                popup.ContentText = myNotes[i].desk;
                                popup.BodyColor = System.Drawing.Color.Red;
                                popup.Popup();

                            }
                            else if (myNotes[i].importance == "Важно")
                            {
                                popup.TitleText = myNotes[i].name;
                                popup.ContentText = myNotes[i].desk;
                                popup.BodyColor = System.Drawing.Color.Yellow;
                                popup.Popup();
                            }
                            else
                            {
                                popup.TitleText = myNotes[i].name;
                                popup.ContentText = myNotes[i].desk;
                                popup.BodyColor = System.Drawing.Color.Green;
                                popup.Popup();
                            }
                        }
                    }
                };
                timer.Start();
                var timer1 = new System.Windows.Threading.DispatcherTimer();
                
                timer1.Interval = new TimeSpan(0, 0, 1);
                timer1.IsEnabled = true;
                timer1.Tick += (o, t) => {
                    Console.WriteLine(myNotes.Count.ToString());
                    

                };
                timer1.Start();

               

              
                // long before = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                TextBlockDate.Text = MainWindow.GetNetworkTime().ToShortDateString();
                //long after = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                // MessageBox.Show((after - before).ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
       
        private  void window1_Loaded(object sender, RoutedEventArgs e)
        {
            TimerStart();
            Callendar callendar = new Callendar();
            /*   Thread thread = new Thread(new ThreadStart(ConnChat));
               thread.Start();*/
            ConnChat();// перенести
         
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Your Notes");
            if (key.GetValue("Language").ToString() == "English")
            {
                TextBlockAddNotess.Text = "Add note";
                TextBlockViewNotes.Text = "View notes";
                btnAddNotes.Content = "Add note";
                btnCurrentNotes.Text = "Current notes";
                btnPendingNotes.Text = "Pending notes";
                btnCompletedNotes.Text = "Completed notes";
                TextBlockProfil.Text = "Profile";
                PersonalData.Text = "Personal information";
                blockSecurity.Text = "Security";
                TextBlockMain.Text = "Main";
                textBlockGraphic.Text = "Graphic";
            }
           

        }

        private void TextBlockProfil_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsOpenVievNotes){
                Storyboard ss = (Storyboard)TryFindResource("NotesClose");
                ss.Begin();
                IsOpenVievNotes = false;

            }
            if (IsOpenVievProfil)
            {
                Storyboard ss = (Storyboard)TryFindResource("ProfilClose");
                ss.Begin();
                IsOpenVievProfil = false;
            }
            else
            {
                Storyboard ss = (Storyboard)TryFindResource("ProfilOpen");
                ss.Begin();
                IsOpenVievProfil = true;
            }
        }

        private void Grid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
           
        }

        private void xqwe_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            DateTime? selectedDate = xqwe.SelectedDate;

           
        }

        private void btnProfilSave_Click(object sender, RoutedEventArgs e)
        {
            btnProfilSave.IsEnabled = false;
            if (ProfilTextBoxPatronymic.Text.Trim() != "" && ProfilTexBoxName.Text.Trim() != "" && ProfilTextBoxSurname.Text.Trim() != "")
            {
                if (ProfilTextDateBrith.Text.Trim() == "")
                {
                    PathTextDateBrith.Stroke = new SolidColorBrush(Colors.Red);
                }
                else
                {
                    PathTextDateBrith.Stroke = new SolidColorBrush(Colors.Red);
                    try
                    {
                        conn.Open();

                        string sqlExpression = String.Format("UPDATE users SET Name = '{0}', Surname = '{1}', Patronymic = '{2}', DateOfBrith = '{3}' WHERE mail = '{4}'", ProfilTexBoxName.Text.Trim(), ProfilTextBoxSurname.Text.Trim(), ProfilTextBoxPatronymic.Text.Trim(), DateTime.Parse(ProfilTextDateBrith.Text.Trim()).ToString(), TextBlockName.Text.Trim());
                        MySqlCommand command = new MySqlCommand(sqlExpression, conn);
                        command.ExecuteNonQuery();

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    finally
                    {
                        conn.Close();
                        btnProfilSave.IsEnabled = true;
                    }
                }
            }
        }

        private void ProfilTexBoxName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (ProfilTexBoxName.Text.Trim() == "") PathTextBoxName.Stroke = new SolidColorBrush(Colors.Red);
            else PathTextBoxName.Stroke = new SolidColorBrush(Colors.White);
        }

        private void ProfilTextBoxSurname_LostFocus(object sender, RoutedEventArgs e)
        {
            if (ProfilTextBoxSurname.Text.Trim() == "") PathTextBoxSurname.Stroke = new SolidColorBrush(Colors.Red);
            else PathTextBoxSurname.Stroke = new SolidColorBrush(Colors.White);
        }

        private void ProfilTextBoxPatronymic_LostFocus(object sender, RoutedEventArgs e)
        {
            if (ProfilTextBoxPatronymic.Text.Trim() == "") PathTextBoxPatronymic.Stroke = new SolidColorBrush(Colors.Red);
            else PathTextBoxPatronymic.Stroke = new SolidColorBrush(Colors.White);
        }

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Addnotes.Visibility = Visibility.Visible;
            GridAllNotes.Visibility = Visibility.Hidden;
            CurrentNotes.Visibility = Visibility.Hidden;
            ExpectedNotes.Visibility = Visibility.Hidden;
            CompletesNotes.Visibility = Visibility.Hidden;
        }

        private void btnAddNotes_Click(object sender, RoutedEventArgs e)
        {
            btnAddNotes.IsEnabled = false;
            try
            {
                conn.Open();
                string sqlExpression = String.Format("SELECT id FROM users WHERE mail = '{0}'", TextBlockName.Text.Trim());
                MySqlCommand command = new MySqlCommand(sqlExpression, conn);
                string id = command.ExecuteScalar().ToString();
                string hren = AddNotesLvl.Text.ToString();
                MessageBox.Show(id);            
                sqlExpression = String.Format("INSERT INTO notes (id_user,name,description,date,time,importance,status) VALUES ('{0}','{1}','{2}','{3}','{4}','{5}', '{6}')",id,AddNotesName.Text.Trim(),AddNotesDesk.Text.Trim(),DateTime.Parse(AddNotesDate.Text).ToShortDateString(),DateTime.Parse(AddNotesDate.Text).ToShortTimeString(),AddNotesLvl.Text,"3");
                command.CommandText = sqlExpression;
                command.ExecuteNonQuery();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
                
            }
            finally
            {
                conn.Close();
                btnAddNotes.IsEnabled = true;
            }
        }

        private void TextBlock_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            
            GridAllNotes.Visibility = Visibility.Visible;
            CurrentNotes.Visibility = Visibility.Hidden;
            ExpectedNotes.Visibility = Visibility.Hidden;
            CompletesNotes.Visibility = Visibility.Hidden;
            Addnotes.Visibility = Visibility.Hidden;
            try
            {
                conn.Open();
                string sqlExpression = String.Format("SELECT id FROM users WHERE mail = '{0}'", TextBlockName.Text.Trim());
                MySqlCommand command = new MySqlCommand(sqlExpression,conn);
                string id = command.ExecuteScalar().ToString();
                sqlExpression = String.Format("SELECT * FROM notes WHERE id_user = '{0}'", id);
                command.CommandText = sqlExpression;
                MySqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    list.Items.Add(new MyItem { name = Convert.ToString(reader["name"]), desk = Convert.ToString(reader["description"]), date = Convert.ToString(reader["date"]), time = Convert.ToString(reader["time"]), importance = Convert.ToString(reader["importance"]) });
                }
              
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        private void TextBlock_MouseDown_2(object sender, MouseButtonEventArgs e)
        {
            try
            {

                conn.Open();
                GridAllNotes.Visibility = Visibility.Hidden;
                CurrentNotes.Visibility = Visibility.Visible;
                ExpectedNotes.Visibility = Visibility.Hidden;
                Addnotes.Visibility = Visibility.Hidden;
                CompletesNotes.Visibility = Visibility.Hidden;
                string sqlExpression = String.Format("SELECT id FROM users WHERE mail = '{0}'", TextBlockName.Text.Trim());
                MySqlCommand command = new MySqlCommand(sqlExpression, conn);
                string id = command.ExecuteScalar().ToString();
                sqlExpression = String.Format("SELECT * FROM notes WHERE id_user = '{0}' AND status < 3", id);
                command.CommandText = sqlExpression;
                MySqlDataReader reader = command.ExecuteReader();
                List<string> list = new List<string>();
                while (reader.Read())
                {
                    if (DateTime.Parse(Convert.ToString(reader["date"])) < DateTime.Parse(MainWindow.GetNetworkTime().ToShortDateString()))
                    {
                        list.Add(reader["id_note"].ToString());   
                    }
                    else if (DateTime.Parse(Convert.ToString(reader["date"]))==DateTime.Parse(MainWindow.GetNetworkTime().ToShortDateString()) && DateTime.Parse(Convert.ToString(reader["time"]))<=DateTime.Parse(MainWindow.GetNetworkTime().ToShortTimeString()))
                    {
                        list.Add(reader["id_note"].ToString());
                    }
                }
                reader.Close();
                for (int i = 0; i < list.Count(); i++)
                {
                    sqlExpression = String.Format("UPDATE notes SET status = '2' WHERE id_note = '{0}'", list[i]);
                    command.CommandText = sqlExpression;
                    command.ExecuteNonQuery();
                }
                sqlExpression = String.Format("SELECT * FROM notes WHERE id_user = '{0}' AND status = '1'", "13");
                command.CommandText = sqlExpression;
                reader = command.ExecuteReader();            
                while (reader.Read())
                {
                    NotesS.Items.Add(new MyItem { name = Convert.ToString(reader["name"]), desk = Convert.ToString(reader["description"]), date = Convert.ToString(reader["date"]), time = Convert.ToString(reader["time"]), importance = Convert.ToString(reader["importance"]) });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        private void TextBlock_MouseDown_3(object sender, MouseButtonEventArgs e)
        {
          
            GridAllNotes.Visibility = Visibility.Hidden;
            CurrentNotes.Visibility = Visibility.Hidden;
            ExpectedNotes.Visibility = Visibility.Visible;
            Addnotes.Visibility = Visibility.Hidden;
            CompletesNotes.Visibility = Visibility.Hidden;
            try
            {
                conn.Open();
                string sqlExpression = String.Format("SELECT id FROM users WHERE mail = '{0}'", TextBlockName.Text.Trim());
                MySqlCommand command = new MySqlCommand(sqlExpression, conn);
                string id = command.ExecuteScalar().ToString();
                sqlExpression = String.Format("SELECT * FROM notes WHERE id_user = '{0}' AND status = '2'", "13");
                command.CommandText = sqlExpression;
                MySqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    NotessS.Items.Add(new MyItem { id = Convert.ToString(reader["id_note"]),name = Convert.ToString(reader["name"]), desk = Convert.ToString(reader["description"]), date = Convert.ToString(reader["date"]), time = Convert.ToString(reader["time"]), importance = Convert.ToString(reader["importance"]) });
                    comboBoxNotes.Items.Add(reader["id_note"]);
                }

            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        private void TextBlock_MouseDown_4(object sender, MouseButtonEventArgs e)
        {
            
            GridAllNotes.Visibility = Visibility.Hidden;
            CurrentNotes.Visibility = Visibility.Hidden;
            ExpectedNotes.Visibility = Visibility.Hidden;
            Addnotes.Visibility = Visibility.Hidden;
            CompletesNotes.Visibility = Visibility.Visible;
            try
            {
                conn.Open();
                string sqlExpression = String.Format("SELECT id FROM users WHERE mail = '{0}'", TextBlockName.Text.Trim());
                MySqlCommand command = new MySqlCommand(sqlExpression, conn);
                string id = command.ExecuteScalar().ToString();
                sqlExpression = String.Format("SELECT * FROM notes WHERE id_user = '{0}' AND status = '3'", id);
                command.CommandText = sqlExpression;
                MySqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    NotesSs.Items.Add(new MyItem { id = Convert.ToString(reader["id_note"]), name = Convert.ToString(reader["name"]), desk = Convert.ToString(reader["description"]), date = Convert.ToString(reader["date"]), time = Convert.ToString(reader["time"]), importance = Convert.ToString(reader["importance"]) });
                    comboBoxNotes.Items.Add(reader["id_note"]);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
           /* f.OpenFileDialog odf = new f.OpenFileDialog();
            odf.Filter = "Image Files(*.BMP;*.JPG;*.GIF;*.PNG)|*.BMP;*.JPG;*.GIF;*.PNG|All files (*.*)|*.*";
           if (odf.ShowDialog()==f.DialogResult.OK)
            {
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(odf.FileName, UriKind.Relative);
                

            }*/
          
        }

        private void TextBlock_MouseDown_5(object sender, MouseButtonEventArgs e)
        {
            
            try
            {
                if (!IsCalendarOpen)
                {
                    Callendar callendar = new Callendar();
                    conn.Open();
                    IsCalendarOpen = true;
                    string sqlExpression = String.Format("SELECT id FROM users WHERE mail = '{0}'", TextBlockName.Text.Trim());
                    MySqlCommand command = new MySqlCommand(sqlExpression, conn);
                    string id = command.ExecuteScalar().ToString();
                    callendar.id = id;
                    callendar.Show();
                }
            }
            catch
            {

            }
            finally
            {
                conn.Close();
            }
        }

        private void enterChat_Click(object sender, RoutedEventArgs e)
        {
            string nickName = nicknameData.Text;
            
            if (string.IsNullOrEmpty(nickName))
                return;
            Send($"#setname|{nickName}");
        }

        private void window1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
           
                if (_serverSocket.Connected)
                    Send("#endsession");
            
        }

        private void messageData_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string msgData = messageData.Text;
                if (string.IsNullOrEmpty(msgData))
                    return;
                if (msgData[0] == '"')
                {
                    string temp = msgData.Split(' ')[0];
                    string content = msgData.Substring(temp.Length + 1);
                    temp = temp.Replace("\"", string.Empty);
                    Send($"#private|{temp}|{content}");
                }
                else
                    Send($"#message|{msgData}");
                messageData.Text = string.Empty;
            }
        }

        private void SendImgChat_Click(object sender, RoutedEventArgs e)
        {
            if (userList.SelectedItems.Count == 0)
            {
                return;
            }
            OpenFileDialog ofp = new OpenFileDialog();
            ofp.ShowDialog();
            if (!File.Exists(ofp.FileName))
            {
                MessageBox.Show($"Файл {ofp.FileName} не найден!");
                return;
            }
            FileInfo fi = new FileInfo(ofp.FileName);
            byte[] buffer = File.ReadAllBytes(ofp.FileName);
            Send($"#sendfileto|{userList.SelectedItem}|{buffer.Length}|{fi.Name}");//g
            Send(buffer);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (userList.SelectedItems.Count > 0)
            {
                messageData.Text = $"\"{userList.SelectedItem} ";
            }
        }

        private void TextBlockViewNotes_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsOpenVievProfil)
            {
                Storyboard ss = (Storyboard)TryFindResource("ProfilClose");
                ss.Begin();
                IsOpenVievProfil = false;
            }
            if (IsOpenVievNotes)
            {
                Storyboard ss = (Storyboard)TryFindResource("NotesClose");
                ss.Begin();
                IsOpenVievNotes = false;
            }
            else
            {
                Storyboard ss = (Storyboard)TryFindResource("NotesOpen");
                ss.Begin();
                IsOpenVievNotes = true;
            }
        }

       
    }
}
