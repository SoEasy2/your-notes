using System;
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
using System.Windows.Shapes;
using MySql.Data.MySqlClient;

namespace Skooby
{
    /// <summary>
    /// Логика взаимодействия для Callendar.xaml
    /// </summary>
    public partial class Callendar : Window
    {
        public string id;
        public Callendar()
        {
            InitializeComponent();
    
        }

    
    

        private void calendar1_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            NotesNow.Text = "";
            MySqlConnection conn = new MySqlConnection(DBConnection.Connect());
            DateTime? selectedDate = calendar1.SelectedDate;
            try
            {
                conn.Open();
               string sqlExpression = String.Format("SELECT name, date, time FROM notes WHERE id_user = '{0}' AND date = '{1}'",id,selectedDate.Value.Date.ToShortDateString());
                MySqlCommand command = new MySqlCommand( sqlExpression, conn);
                MySqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    NotesNow.Text += Convert.ToString(reader["name"]) + " " + Convert.ToString(reader["date"]) + " " + Convert.ToString(reader["time"]) + Environment.NewLine; 
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Window1.IsCalendarOpen = false;
            e.Cancel = false;
        }
    }
}
