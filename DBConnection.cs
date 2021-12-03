using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Skooby
{
    class DBConnection
    {

    public static string Connect()
        {
            return "datasource=127.0.0.1;database=yournotes;port=25555;username=root;password=;SSL Mode=None";
        }
    }
}
