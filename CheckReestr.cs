using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Skooby
{
    class CheckReestr
    {
        public static void checkReestr()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Your Notes", true);
            try
            {
                foreach (string name in key.GetValueNames())
                {
             
                    if (name != "Language" && DateTime.Parse(key.GetValue(name).ToString()).AddMinutes(5) < MainWindow.GetNetworkTime())
                    {
                        
                        key.DeleteValue(name);
                    }
            }
            }
            catch
            {

            }
        }
    }
}
