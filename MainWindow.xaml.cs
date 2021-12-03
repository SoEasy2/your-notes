using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Recognizers.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Speech;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Speech.Recognition;
using MySql.Data.MySqlClient;
using System.Net.Mail;
using Microsoft.Win32;
using System.Net.Sockets;

namespace Skooby
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
 
        public static bool isEng, isOpen, isEmail, isPass, isConfEmail, isConfPass = false;
        MySqlConnection conn = new MySqlConnection(DBConnection.Connect());
        
        public MainWindow()
        {
            InitializeComponent();
        }


        public static DateTime GetNetworkTime()
        {
            const string ntpServer = "time.windows.com";
            var ntpData = new byte[48];
            ntpData[0] = 0x1B;

            var addresses = System.Net.Dns.GetHostEntry(ntpServer).AddressList;
            var ipEndPoint = new System.Net.IPEndPoint(addresses[0], 123);

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Connect(ipEndPoint);
                socket.Send(ntpData);
                socket.Receive(ntpData);
                socket.Close();
                
            }

            var intPart = (ulong)ntpData[40] << 24 | (ulong)ntpData[41] << 16 | (ulong)ntpData[42] << 8 | ntpData[43];
            var fractPart = (ulong)ntpData[44] << 24 | (ulong)ntpData[45] << 16 | (ulong)ntpData[46] << 8 | ntpData[47];

            var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
            var networkDateTime = (new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddMilliseconds((long)milliseconds);

            return networkDateTime.ToLocalTime();
        }

    
        
        bool IsReccAcc (string email, string id)
        {
            string sqlExpression;
            MySqlCommand command = new MySqlCommand();
            command.Connection = conn;
                if (Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Your Notes", email, null) == null)
                {
                 
                    sqlExpression = String.Format("SELECT dateRec FROM `reccoverypassword` WHERE id = '{0}'", id);
                    command.CommandText = sqlExpression;
                    MySqlDataReader reader = command.ExecuteReader();
                    reader.Read();
                    Registry.CurrentUser.CreateSubKey(@"Software\Your Notes").SetValue(email, DateTime.Parse(String.Format("{0}",reader["dateRec"])));
                    reader.Close();
                
            

                }
                try
                {
                    sqlExpression = String.Format("SELECT dateRec FROM `reccoverypassword` WHERE id = '{0}'", id);
                    command.CommandText = sqlExpression;
                    var registryTime = DateTime.Parse(Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Your Notes", email, null).ToString());
                    MessageBox.Show(registryTime.ToString());
                    MySqlDataReader reader = command.ExecuteReader();
                    MessageBox.Show("tut22");
                    reader.Read();
                    var dbTime = DateTime.Parse(String.Format("{0}",reader["dateRec"]));
                    reader.Close();
                    MessageBox.Show("tut23");
                    if (dbTime == registryTime && dbTime.AddMinutes(5)<GetNetworkTime() ){
            
                        return true;
                    }
                    else
                    {
                        MessageBox.Show("Vranie");
                    conn.Close();
                        return false;
                    }
                }
                
                    catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                conn.Close();
                    return false;
                }

          
        }

        bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        bool IsValidPass(string Pass)
        {
            var hasNumber = new Regex(@"[0-9]+");
            
            var hasMinimum8Chars = new Regex(@".{6,}");

           
       var isValidated = hasNumber.IsMatch(Pass) && hasMinimum8Chars.IsMatch(Pass);
            if (isValidated) return true;
            else return false;
            
        }

        private void txt_email_LostFocus(object sender, RoutedEventArgs e)
        {
            if(IsValidEmail(txt_email.Text.Trim()))
            {
                img_emailcheck.Visibility = Visibility.Visible;
                email_path.Stroke= Brushes.Green;
            }
        }

        

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            conn.Open();
            string sqlExpression = String.Format("SELECT count(*) FROM users WHERE mail = '{0}'", regEmail.Text.Trim());

            MySqlCommand command = new MySqlCommand(sqlExpression,conn);
            MessageBox.Show(command.ExecuteScalar().ToString());
            if (isEmail && isConfEmail && isPass && isConfPass && Convert.ToInt32(command.ExecuteScalar())==0)
            {
                
                try
                {
                    
                    sqlExpression = String.Format("INSERT INTO users (mail, password, date) VALUES ('{0}','{1}','{2}')", regEmail.Text.Trim(), regPass.Text.Trim(), DateTime.Now.ToString());

                    command = new MySqlCommand(sqlExpression, conn);
                    command.ExecuteNonQuery();
                    Storyboard s = (Storyboard)TryFindResource("strelkaEnd");
                    s.Begin();
                    Storyboard ss = (Storyboard)TryFindResource("regTy");
                    ss.Begin();
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
            else
            {
                Storyboard anim = (Storyboard)TryFindResource("imgEmail");
                regImgEmail.Source = new BitmapImage(new Uri(@"D:\C#\Skooby-main\Skooby\icons8-cancel-50.png"));
                anim.Begin();
                conn.Close();
            }
           
        }

        private void regEmail_LostFocus(object sender, RoutedEventArgs e)
        {
            Storyboard anim = (Storyboard)TryFindResource("imgEmail");
            if (IsValidEmail(regEmail.Text))
            {
                regImgEmail.Source = new BitmapImage(new Uri(@"D:\C#\Skooby-main\Skooby\icons8-checked-64.png"));
                anim.Begin();
                isEmail = true;
            }
            else { regImgEmail.Source = new BitmapImage(new Uri(@"D:\C#\Skooby-main\Skooby\icons8-cancel-50.png"));
                anim.Begin();
                isEmail = false;
            }

        }

        private void regPass_LostFocus(object sender, RoutedEventArgs e)
        {
            Storyboard anim = (Storyboard)TryFindResource("imgPass");
            if (IsValidPass(regPass.Text))
            {
                regImgPass.Source = new BitmapImage(new Uri(@"D:\C#\Skooby-main\Skooby\icons8-checked-64.png"));
                anim.Begin();
                isPass = true;
            }
            else
            {
                regImgPass.Source = new BitmapImage(new Uri(@"D:\C#\Skooby-main\Skooby\icons8-cancel-50.png"));
                anim.Begin();
                isPass = false;
            }
        }

        private void regConfPass_LostFocus(object sender, RoutedEventArgs e)
        {
            Storyboard anim = (Storyboard)TryFindResource("imgConfPass");
            if (regConfPass.Text == regPass.Text)
            {
                regImgConfPass.Source = new BitmapImage(new Uri(@"D:\C#\Skooby-main\Skooby\icons8-checked-64.png"));
                anim.Begin();
                isConfPass = true;
            }
            else
            {
                regImgConfPass.Source = new BitmapImage(new Uri(@"D:\C#\Skooby-main\Skooby\icons8-cancel-50.png"));
                anim.Begin();
                isConfPass = false;
            }
        }

        private void regConfEmail_LostFocus(object sender, RoutedEventArgs e)
        {
            Storyboard anim = (Storyboard)TryFindResource("imgConfEmail");
            if (regConfEmail.Text == regEmail.Text)
            {
                regImgConfEmail.Source = new BitmapImage(new Uri(@"D:\C#\Skooby-main\Skooby\icons8-checked-64.png"));
                anim.Begin();
                isConfEmail = true;
            }
            else
            {
                regImgConfEmail.Source = new BitmapImage(new Uri(@"D:\C#\Skooby-main\Skooby\icons8-cancel-50.png"));
                anim.Begin();
                isConfEmail = false;
            }
        }

        private void btnRegistration__Click(object sender, RoutedEventArgs e)
        {
            try
            {
                conn.Open();
                System.Data.DataTable table = new System.Data.DataTable();
                MySqlDataAdapter adapter = new MySqlDataAdapter();

                MySqlCommand command = new MySqlCommand("SELECT * FROM `users` WHERE `mail` = @uL AND `password` =@uP", conn);
                command.Parameters.Add("@uL", MySqlDbType.VarChar).Value = txt_email.Text;
                command.Parameters.Add("@uP", MySqlDbType.VarChar).Value = txt_pass.Password;
                adapter.SelectCommand = command;
                adapter.Fill(table);
                if (table.Rows.Count > 0)
                {
                    MessageBox.Show("true");
                    img_passcncel.Visibility = Visibility.Hidden;
                    img_passcheck.Visibility = Visibility.Visible;
                    pass_path.Stroke = Brushes.Green;
                    txt_pass.Foreground = Brushes.White;
                    Window1 window1 = new Window1();
                    window1.TextBlockName.Text = txt_email.Text.Trim();


                    window1.Show();
                    window1.TextBlockName.Text = txt_email.Text.Trim();
                    window1.TimerStart();
                    this.Hide();
                }
                else
                {
                    MessageBox.Show("False");
                    Storyboard s = (Storyboard)TryFindResource("Animate");
                    s.Begin();
                    img_passcncel.Visibility = Visibility.Visible;
                    img_passcheck.Visibility = Visibility.Hidden;
                    pass_path.Stroke = Brushes.Red;
                    txt_pass.Foreground = Brushes.Red;
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

        private void btnReg_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Storyboard s = (Storyboard)TryFindResource("Reg");
            s.Begin();
        }

     

        private void btnSendPass_Click(object sender, RoutedEventArgs e)
        {
            conn.Open();
            string sqlExpression = String.Format("SELECT COUNT(*) FROM users WHERE mail = '{0}'", textBoxRec.Text.Trim());
            MySqlCommand command = new MySqlCommand(sqlExpression, conn);
            if (Convert.ToInt32(command.ExecuteScalar())!=0) {
                sqlExpression = string.Format("SELECT id FROM users WHERE mail = '{0}'", textBoxRec.Text.Trim());
                command.CommandText = sqlExpression;
                string id = command.ExecuteScalar().ToString();
                sqlExpression = String.Format("SELECT COUNT(*) FROM reccoverypassword WHERE id = '{0}'", id);
                command.CommandText = sqlExpression;
                if (Convert.ToInt32(command.ExecuteScalar()) != 0)
                {
                    if(IsReccAcc(textBoxRec.Text.Trim(),id))
                    {
                        string pass = ReccPass.RandomPass(new Random().Next(6, 10));
                        if (ReccPass.SendEmail(textBoxRec.Text.Trim(), pass))
                        {
                            sqlExpression = String.Format("UPDATE reccoverypassword SET dateRec='{0}' WHERE id='{1}'", GetNetworkTime(), id);
                            command.CommandText = sqlExpression;
                            command.ExecuteNonQuery();
                            sqlExpression = String.Format("UPDATE users SET password = '{0}' WHERE id = {1}", pass, id);
                            command.ExecuteNonQuery();
                            Registry.CurrentUser.CreateSubKey(@"Software\Your Notes").SetValue(textBoxRec.Text.Trim(), GetNetworkTime().ToString());
                            MessageBox.Show("if");
                            conn.Close();
                        }
                    }
                }
                else
                {
                    try
                        {
                        string pass = ReccPass.RandomPass(new Random().Next(6, 10));
                        if (ReccPass.SendEmail(textBoxRec.Text.Trim(), pass))
                        {
                           
                            sqlExpression = String.Format("INSERT INTO reccoverypassword (id, dateRec) VALUES ('{0}','{1}') ",id,GetNetworkTime());
                            command.CommandText = sqlExpression;
                            command.ExecuteNonQuery();
                            sqlExpression = String.Format("UPDATE users SET password = '{0}' WHERE id = {1}", pass, id);
                            command.ExecuteNonQuery();
                            Registry.CurrentUser.CreateSubKey(@"Software\Your Notes").SetValue(textBoxRec.Text.Trim(), GetNetworkTime().ToString());
                            MessageBox.Show("else");
                            conn.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        conn.Close();
                    }
                   

                }
            }
            else
            {

            }

            conn.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Window1 window1 = new Window1();
            window1.Show();
            Registry.CurrentUser.CreateSubKey(@"Software\Your Notes").SetValue("xxx", "");
            CheckReestr.checkReestr();
            
            if (Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Your Notes", "Language",null) == null)
            {
                Registry.CurrentUser.CreateSubKey(@"Software\Your Notes").SetValue("Language", "Russian");
            }
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Your Notes");
            if (key.GetValue("Language").ToString()=="Russian"){
                lblLogEmail.Content = "Почта";
                lblLogPass.Content = "Пароль";
                lblRegEmail.Content = "Почта";
                lblRegConfEmail.Content = "Подтвердите почту";
                lblRegPass.Content = "Пароль";
                lblRegConfPass.Content = "Повторите пароль";
                btnLogin.Content = "Войти";
                labelReg.Text = "У вас еще нету аккаунта?";
                labelReg.Margin = new Thickness(20, 403, 0, 0);
                btnReg.Text = "Зарегистрироваться";
                btnReg.Margin = new Thickness(206, 403, 0, 0);
                btnRegistration.Content = "Зарегистрироваться";
                regTyText.Text = "Спасибо за регистрацию";
                RecPass.Text = "Забыли пароль?";
            }
            else if (key.GetValue("Language").ToString() == "English")
            {
                isEng = true;
                RecPass.Text = "Forgot your password?";
                labelReg.Text = "Don't have an account yet?";
                labelReg.Margin = new Thickness(50, 403, 0, 0);
                btnReg.Text = "Register now";
                btnReg.Margin = new Thickness(250, 403, 0, 0);
                btnLogin.Content = "Login";
                lblRegEmail.Content = "Email";
                lblRegConfPass.Content = "Repeat password";
                lblRegConfEmail.Content = "Confirm email";
                lblLogPass.Content = "Password";
                lblLogEmail.Content = "Email";
                btnRegistration.Content = "Registration";
                regTyText.Text = "Thank you for registering!";
                lblRegPass.Content = "Password";

            }
            

        }
        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            
            Storyboard s = (Storyboard)TryFindResource("ReccPassOpen");
            s.Begin();

        }

        private void btnOpenLang_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (isOpen)
            {
                Storyboard s = (Storyboard)TryFindResource("LangClose");
                s.Begin();
                isOpen = false;
            }
            else
            {
                Storyboard s = (Storyboard)TryFindResource("LangOpen");
                s.Begin();
                isOpen = true;
            }
        }

        private void Lang_rus_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Registry.CurrentUser.CreateSubKey(@"Software\Your Notes").SetValue("Language", "English");




        }

        private void lang_eng_MouseDown(object sender, MouseButtonEventArgs e)
        {

            Registry.CurrentUser.CreateSubKey(@"Software\Your Notes").SetValue("Language", "Russian");
        }
    }
}
