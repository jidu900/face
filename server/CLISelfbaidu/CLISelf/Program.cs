using CLISelf.Common;
//using CLISelf.ExtensionServer;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;

namespace CLISelf
{
    class Program
    {
        public static IDisposable m_webapi = null;
        private static NLog.Logger m_logger = LogManager.GetCurrentClassLogger();
        public static string m_webApiHost = System.Configuration.ConfigurationManager.AppSettings["webApiHost"].ToString();
        public static string m_ExtensionName = System.Configuration.ConfigurationManager.AppSettings["ExtensionName"].ToString();
        public static string facedb = System.Configuration.ConfigurationManager.AppSettings["face"].ToString();
       

        public static Daemon m_daemon = null;
        public static int m_state = 0;//0:init 1:run 2:start 3:workprocess

        public static ConcurrentDictionary<string, int> m_cameraDic = new ConcurrentDictionary<string, int>();
     
        
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Process currentProcess = Process.GetCurrentProcess();
            string path = Path.Combine(Environment.CurrentDirectory, currentProcess.ProcessName + m_ExtensionName);
         
            #region CLI

#if !DEBUG
            CameraSever();//内部持久化服务
            WebHost();//供外部调用
            Console.ReadLine();
#else
            string input = "";
            if (args.Length > 0)
            {
                input = args[0];
            }
            else
            {
                Console.WriteLine("Please enter instructions!");
                input = Console.ReadLine();
            }
            do
            {
                if (input == "stop")
                {
                    try
                    {
                        if (m_state != 2)
                        {
                            m_daemon.Stop();
                            m_logger.Debug("daemon server stop!");
                            Console.WriteLine("daemon server stop!");


                            m_state = 2;
                        }
                    }
                    catch { }
                    finally { }
                }
                else if (input == "start")
                {
                    //守护线程
                    if (m_state != 1)
                    {
                        if (m_daemon == null)
                        {
                            m_daemon = new Daemon(path, "workprocess");
                        }
                        if (!m_daemon.IsRun)
                        {
                            m_daemon.Start();
                            m_state = 1;
                            m_logger.Debug("server start!");
                            Console.WriteLine("server start!");
                        }
                    }
                }
                else if (input == "workprocess")
                {
                    Console.WriteLine("This workprocess!");
                 
                    WebHost();//供外部调用
                    m_logger.Debug("workprocess start!");
                    Console.WriteLine("workprocess start!");


                }
                input = Console.ReadLine();
            } while (true);
#endif
            #endregion

        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                Exception ex = e.ExceptionObject as Exception;
                m_logger.Error(ex, "CurrentDomain_UnhandledException");
            }
            catch { }
        }

        public static void WebHost()
        {
            try
            {
                m_webapi = Microsoft.Owin.Hosting.WebApp.Start<Startup>(url: m_webApiHost);
                m_logger.Debug("webApi Host:{0} OK!", m_webApiHost);
                Console.WriteLine("webApi Host:{0} OK!"+ m_webApiHost);
               
            }
            catch (Exception ex)
            {
                m_logger.Error(ex, "WebHost");
            }
        }


       


    }
}
