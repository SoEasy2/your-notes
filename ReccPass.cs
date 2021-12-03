using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mail;


namespace Skooby
{
    class ReccPass
    {
       public static bool SendEmail(string email,string pass)
        {
            
            try

            {
                
                using (MailMessage mm = new MailMessage("roman.stasenok76@gmail.com", email))
                {
                    mm.Subject = "YOUR NOTES";
                    mm.Body = "Вы сменили пароль, ваш текущий пароль - "+pass;
                    mm.IsBodyHtml = false;
                    using (SmtpClient sc = new SmtpClient("smtp.gmail.com", 587))
                    {

                        sc.DeliveryMethod = SmtpDeliveryMethod.Network;

                        sc.EnableSsl = true;
                        sc.Credentials = new System.Net.NetworkCredential("roman.stasenok76@gmail.com", "roman15042003");
                        sc.Send(mm);
                        return true;
                    }
                }
    
            }
            catch (Exception ex)
            {
                return false;
                //AnyErrors
            }

        }
        static public string RandomPass (int size)
        {
            Random random = new Random();
            int rnd = 0;
            char[] password = new char[size];
            for (int i = 0; i < size; i++)
            {
                rnd = random.Next(1, 4);
                if (rnd == 1)
                {

                    password[i] = Convert.ToChar(random.Next(97, 122));
                }
                else if (rnd == 2)
                {
                    password[i] = Convert.ToChar(random.Next(65, 90));
                }
                else
                {
                    password[i] = Convert.ToChar(random.Next(48, 57));
                }
            }
            
            return new string(password);
        }

    }
}
