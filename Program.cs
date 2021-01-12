using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.ServiceProcess;
using System.Timers;

namespace MYSQL_Arkangel
{
    class Program
    {
        public static string SERVICENAME = "MYSQL80";
        public static string host = "smtp.gmail.com";

        ////////////////////////// EMAIL CREDENTIALS ////////////////////////////////
        public static string email = "";
        public static string password = "";

        /////////////////////////  RECEIVER(S) //////////////////////////////////////
        public static string ToEmail = "";
        public static string CC = "";

        private static Timer aTimer;
        private static bool sendEmail = false;
        private static bool isSendingEmail = false;

        ///////////////////////  LISTENER INTERVAL ///////////////////////////////////
        private static int interval = 1000;

        //////////////////////   LOG OUTPUT FILE ////////////////////////////////////
        private static string outputFile = "";

        private static StreamWriter sw = null;
        private static int previousStatus = (int)ServiceControllerStatus.Running;

        static void Main(string[] args)
        {
            try
            {
                //outputFile = args[1];
                using (sw = new StreamWriter(outputFile, append: true))
                {
                    sw.AutoFlush = true;
                    SetTimer();

                    Console.WriteLine("[ * ] Press the Enter key to exit the application...\n");
                    //sw.WriteLine("The application started at {YYYY-MM-DD:HH:mm:ss}", DateTime.Now);

                    Console.ReadLine();
                    aTimer.Stop();
                    aTimer.Dispose();

                    Console.WriteLine("[ * ] Terminating the application");
                }
            } 
            catch (Exception ex)
            {
                Console.WriteLine("[ * ] Error while running program due to: ");
                Console.WriteLine(ex.ToString());
            }
        }

        private static void SetTimer()
        {
            // Create a timer with a two second interval.
            aTimer = new Timer(interval);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }
        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
           
            ServiceController sc = new ServiceController(SERVICENAME);
            string status;
            switch (sc.Status)
            {
                case ServiceControllerStatus.Running:
                    status = "Running";
                    break;
                case ServiceControllerStatus.Stopped:
                    status = "Stopped";
                    break;
                case ServiceControllerStatus.Paused:
                    status = "Paused";
                    break;
                case ServiceControllerStatus.StopPending:
                    status = "Stopping";
                    break;
                case ServiceControllerStatus.StartPending:
                    status = "Starting";
                    break;
                default:
                    status = "Status Changing";
                    break;
            }

            sw.WriteLine($"[ {SERVICENAME} ] at {e.SignalTime} has status:  {status}");

            sendEmail = previousStatus != (int)sc.Status;

            if (sc.Status == ServiceControllerStatus.Stopped 
                || sc.Status == ServiceControllerStatus.StopPending)
            {
                try
                {
                    sw.WriteLine($"\n[ * ] Attempting to start {SERVICENAME}...");
                    sc.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ {SERVICENAME} ] at {e.SignalTime} failed to restart automatically. Exception: \n{ex}");
                }
            }
            
            if (sc.Status == ServiceControllerStatus.StopPending)
            {
                if (sendEmail && !isSendingEmail)
                    SendEmail(ToEmail, CC, "", "MySQL Down", "MySQL stopping...");
            }

            if (sc.Status == ServiceControllerStatus.Stopped)
            {
                if (sendEmail && !isSendingEmail)
                    SendEmail(ToEmail, CC, "", "MySQL Down", "MySQL stopped");
            }

            if (sc.Status == ServiceControllerStatus.StartPending)
            {
                if (sendEmail && !isSendingEmail)
                    SendEmail(ToEmail, CC, "", "MySQL Down", "MySQL restarting...");
            }

            if (sc.Status == ServiceControllerStatus.Running)
            {
                if (sendEmail && !isSendingEmail)
                    SendEmail(ToEmail, CC, "", "MySQL Up Again", "MySQL running again...");
            }

            previousStatus = (int)sc.Status;
        }

        public static void SendEmail(String ToEmail, string cc, string bcc, String Subj, string Message)
        {
            //Reading sender Email credential from web.config file  
            isSendingEmail = true;

            string HostAdd = host;
            string FromEmailid = email;
            string Pass = password;

            //creating the object of MailMessage  
            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(FromEmailid); //From Email Id  
            mailMessage.Subject = Subj; //Subject of Email  
            mailMessage.Body = Message; //body or message of Email  
            mailMessage.IsBodyHtml = false;

            string[] ToMuliId = ToEmail.Split(',');
            foreach (string ToEMailId in ToMuliId)
            {
                mailMessage.To.Add(new MailAddress(ToEMailId)); //adding multiple TO Email Id  
            }


            string[] CCId = cc.Split(',');

            foreach (string CCEmail in CCId)
            {
                if (CCEmail != string.Empty)
                    mailMessage.CC.Add(new MailAddress(CCEmail)); //Adding Multiple CC email Id  
            }

            string[] bccid = bcc.Split(',');

            foreach (string bccEmailId in bccid)
            {
                if (bccEmailId != string.Empty)
                    mailMessage.Bcc.Add(new MailAddress(bccEmailId)); //Adding Multiple BCC email Id  
            }
            SmtpClient smtp = new SmtpClient();  // creating object of smptpclient  
            smtp.Host = HostAdd;              //host of emailaddress for example smtp.gmail.com etc  

            //network and security related credentials  

            smtp.EnableSsl = true;
            NetworkCredential NetworkCred = new NetworkCredential();
            NetworkCred.UserName = mailMessage.From.Address;
            NetworkCred.Password = Pass;
            smtp.UseDefaultCredentials = true;
            smtp.Credentials = NetworkCred;
            smtp.Port = 587;

            try
            {
                smtp.Send(mailMessage); //sending Email
            }
            catch (Exception ex)
            {
                sw.WriteLine("[ * ] Exception raised while trying to send email: ");
                sw.WriteLine(ex.ToString());
            }
            finally
            {
                isSendingEmail = false;
            }

            
        }
    }
}
