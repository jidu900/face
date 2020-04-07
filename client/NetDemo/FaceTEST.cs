using NetDemo;
using NETSDKHelper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace demo_csharp
{
    public partial class FaceTEST : Form
    {
        public static readonly log4net.ILog loginfo = log4net.LogManager.GetLogger("loginfo");
        public IntPtr m_lpDevHandle;
        public IntPtr m_lpDevHandlestrem;      
        private SynchronizationContext context;     
        DataTable kqDataTable = new DataTable();
        string ocrapi = ConfigurationManager.AppSettings["ocrapi"];
        string cameraip = ConfigurationManager.AppSettings["CameraIP"];
        string username = ConfigurationManager.AppSettings["Camerausername"];
        string Camerapassword = ConfigurationManager.AppSettings["Camerapassword"];
        string locport = ConfigurationManager.AppSettings["locport"];
        string place = ConfigurationManager.AppSettings["place"];
        string score = ConfigurationManager.AppSettings["score"];
        string dscore = ConfigurationManager.AppSettings["dscore"];
        string groupid = ConfigurationManager.AppSettings["groupid"];
        string facepath = ConfigurationManager.AppSettings["facepath"];
        string kaoqinnum = ConfigurationManager.AppSettings["kaoqinnum"];
        string weixinurl = ConfigurationManager.AppSettings["weixinurl"];
        string locip = ConfigurationManager.AppSettings["locip"];
        string stime1 = ConfigurationManager.AppSettings["stime1"];
        string stime2 = ConfigurationManager.AppSettings["stime2"];
        string etime1 = ConfigurationManager.AppSettings["etime1"];
        string etime2 = ConfigurationManager.AppSettings["etime2"];
        int throwway = Convert.ToInt32(ConfigurationManager.AppSettings["throw"]);
        int thresholdMinutes= Convert.ToInt32(ConfigurationManager.AppSettings["thresholdMinutes"]);       
        private static Dictionary<object, NETDEVSDK.NETDEV_FaceSnapshotCallBack_PF> _sdkCallFaceSnapShot = new Dictionary<object, NETDEVSDK.NETDEV_FaceSnapshotCallBack_PF>();
        public SemaphoreSlim semaphoreSlim = new SemaphoreSlim(Convert.ToInt32(ConfigurationManager.AppSettings["semaphoreSlim"]));//并发限制为10
        /// <summary>
        /// 处理图像队列里的数据，完成后加入OCR处理队列
        /// </summary>
       

        public struct OCRBak
        {
            public string IMG;
            public string Json;
            public string BDtime;
        }
       

        /// <summary>
        /// 图像待识别处理队列
        /// </summary>
        public ConcurrentQueue<string> IMG_Queue = new ConcurrentQueue<string>();
        /// <summary>
        /// 数据处理队列
        /// </summary>
        public ConcurrentQueue<string> IMG_Queue1 = new ConcurrentQueue<string>();

        /// <summary>
        /// 考勤缓存字典
        /// </summary>
        public Dictionary<string, DateTime> kq_dictionary = new Dictionary<string, DateTime>();

        public Dictionary<string, DateTime> wx_dictionary = new Dictionary<string, DateTime>();

        public BlockingCollection<OCRBak> blockingCollection = new BlockingCollection<OCRBak>(new ConcurrentQueue<OCRBak>());

        public AutoResetEvent autoReset = new AutoResetEvent(false);

        public Thread baidu_mainThread = null;
        public Thread result_mainThread = null;
        private static Dictionary<object, NETDEVSDK.NETDEV_ExceptionCallBack_PF> _sdkExcepCallBackFuncList = new Dictionary<object, NETDEVSDK.NETDEV_ExceptionCallBack_PF>();


        public FaceTEST()
        {
            InitializeComponent();
          
            context = SynchronizationContext.Current;
            _sdkCallFaceSnapShot.Add(this, this.FaceSnapshotCallBack);
            _sdkExcepCallBackFuncList.Add(this, this.ExceptionCallBack);
            login(cameraip, username, Camerapassword,0);
            Play();

            baidu_mainThread = new Thread(t => {
                IMGQueueHandle();
            });
            baidu_mainThread.IsBackground = true;
            baidu_mainThread.Start();

            result_mainThread = new Thread(t => {
                OCRQueueHandle();
            });
            result_mainThread.IsBackground = true;
            result_mainThread.Start();
   


        }

        private void ExceptionCallBack(IntPtr lpUserID, Int32 dwType, IntPtr stAlarmInfo, IntPtr lpExpHandle, IntPtr lpUserData)
        {
            if ((int)NETDEV_EXCEPTION_TYPE_E.NETDEV_EXCEPTION_EXCHANGE == dwType)
            {
                loginfo.Info("摄像头收到离线告警");
                //做摄像头重连
                loginfo.Info("摄像头离线重新连接");
            }
        }


        /// <summary>
        /// 判断时间段
        /// </summary>
        /// <param name="timeStr"></param>
        /// <returns></returns>
        public bool getTimeSpan(string timeStr, string stime, string etime)
        {
            //判断当前时间是否在工作时间段内
            string _strWorkingDayAM = stime;//工作时间上午08:30
            string _strWorkingDayPM = etime;
            TimeSpan dspWorkingDayAM = DateTime.Parse(_strWorkingDayAM).TimeOfDay;
            TimeSpan dspWorkingDayPM = DateTime.Parse(_strWorkingDayPM).TimeOfDay;
            DateTime t1 = Convert.ToDateTime(timeStr);

            TimeSpan dspNow = t1.TimeOfDay;
            if (dspNow > dspWorkingDayAM && dspNow < dspWorkingDayPM)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取摄像头图片
        /// </summary>
        /// <param name="lpUserID"></param>
        /// <param name="pstFaceSnapShotData"></param>
        /// <param name="lpUserParam"></param>
        private void FaceSnapshotCallBack(IntPtr lpUserID, ref NETDEV_TMS_FACE_SNAPSHOT_PIC_INFO_S pstFaceSnapShotData, IntPtr lpUserParam)
        {
            try
            {
                bool stimeresult = getTimeSpan(DateTime.Now.ToString(), stime1, etime1);
                bool etimeresult = getTimeSpan(DateTime.Now.ToString(), stime2, etime2);
                if (stimeresult == true || etimeresult == true)
                {
                    if (pstFaceSnapShotData.udwFaceId != 0)
                    {
                         String strNowTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                        DateTime time = DateTime.Now;          
                        string path = time.ToString("yyyyMMddHHmmssfff");
                        string path1 = time.ToString("yyyyMMdd");
                        String sPath1 = @facepath + path1 + @"\";
                        int faceid = pstFaceSnapShotData.udwFaceId;
                        loginfo.Info("faceid:" + faceid+","+ path);
                       
                        if (!Directory.Exists(sPath1))
                        {
                            Directory.CreateDirectory(sPath1);
                        }
                        string strFileName = @sPath1 + path + ".jpg";
                  
                        byte[] array = new byte[pstFaceSnapShotData.udwPicBuffLen];
                        NETDEVSDK.MemCopy(array, pstFaceSnapShotData.pcPicBuff, pstFaceSnapShotData.udwPicBuffLen);
                        FileStream fs = new FileStream(strFileName, FileMode.Create);
                        //将byte数组写入文件中
                        fs.Write(array, 0, array.Length);
                        //所有流类型都要关闭流，否则会出现内存泄露问题
                        fs.Close();
                        loginfo.Info(strFileName + "处理开始："+ IMG_Queue1.Count);
                        if (IMG_Queue1.Count>= throwway)
                        {
                            loginfo.Info("照片处理达到上限" + IMG_Queue1.Count);                            
                            return;
                        }
                        string jitime = new string(pstFaceSnapShotData.szPassTime).Trim();
                        jitime = jitime.Substring(0, 17);
                    
                        IMG_Queue1.Enqueue(jitime + "|" + faceid);
                        IMG_Queue.Enqueue(strFileName);
                        loginfo.Info(strFileName + "加入队列");
                        autoReset.Set();
                    }
                }
                else
                {
                     Updatemess("", "", "非考勤时间", "");
                }
            }
            catch (Exception ex)
            {
                Updatemess("", "", "摄像头异常", "FaceSnapshotCallBack");
                loginfo.Error("FaceSnapshotCallBack", ex);
              
            }
            
               
            
        }
        /// <summary>
        /// 摄像头登录
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="i"></param>
        public void login(string ip,string username,string password,int i)
        {
            if (i==0)
            {
                int iRet = NETDEVSDK.NETDEV_Init();
            }
            
            IntPtr pstDevInfo = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NETDEV_DEVICE_INFO_S)));

            m_lpDevHandle = NETDEVSDK.NETDEV_Login(ip, (Int16)80, username, password, pstDevInfo);
            if (m_lpDevHandle == IntPtr.Zero)
            {
                loginfo.Info("login failed,the error is [" + NETDEVSDK.NETDEV_GetLastError().ToString() + "]");
            }
            else
            {
                IntPtr ptrFaceSnapshotCB = Marshal.GetFunctionPointerForDelegate(_sdkCallFaceSnapShot[this]);
                NETDEVSDK.NETDEV_SetFaceSnapshotCallBack(m_lpDevHandle, ptrFaceSnapshotCB, IntPtr.Zero);
                IntPtr ptrExcepCB = Marshal.GetFunctionPointerForDelegate(_sdkExcepCallBackFuncList[this]);
                NETDEVSDK.NETDEV_SetExceptionCallBack(ptrExcepCB, IntPtr.Zero);
                loginfo.Info("登录成功");

            }
        }

        /// <summary>
        /// 视频播放
        /// </summary>
        public void Play()
        {

            NETDEV_PREVIEWINFO_S stNetInfo = new NETDEV_PREVIEWINFO_S();

            stNetInfo.dwChannelID = int.Parse("1");
            stNetInfo.hPlayWnd = pictureBox1.Handle;
            // stNetInfo.dwStreamType = (Int32)NETDEV_LIVE_STREAM_INDEX_E.NETDEV_LIVE_STREAM_INDEX_MAIN;
            stNetInfo.dwStreamType = (Int32)NETDEV_LIVE_STREAM_INDEX_E.NETDEV_LIVE_STREAM_INDEX_AUX;
            stNetInfo.dwLinkMode = (Int32)NETDEV_PROTOCAL_E.NETDEV_TRANSPROTOCAL_RTPTCP;//only support

            m_lpDevHandlestrem= NETDEVSDK.NETDEV_RealPlay(m_lpDevHandle, ref stNetInfo, IntPtr.Zero, IntPtr.Zero);
            NETDEVSDK.NETDEV_SetIVAShowParam(7);
            NETDEVSDK.NETDEV_SetIVAEnable(m_lpDevHandlestrem, 1);
       
        }


        public void IMGQueueHandle()
        {
            while (true)
            {
                string img_result = "";
                loginfo.Info("进入IMGQueueHandle");
                if (IMG_Queue.TryDequeue(out img_result))
                {
                    loginfo.Info("IMGQueueHandle并发等待开始"+ img_result+ IMG_Queue.Count);
                    semaphoreSlim.Wait();
                    loginfo.Info("IMGQueueHandle并发等待结束" + img_result+ IMG_Queue.Count);               
                    ThreadPool.SetMinThreads(1000, 1000);
                    ThreadPool.QueueUserWorkItem(t => {
                        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                        watch.Start();  //开始监视代码运行时间
                        loginfo.Info("QueueUserWorkItem"+ img_result);
                        string sbdtime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        //string json = Baidu_Ocr(img_result);
                        string json = Baidu_Re(img_result);
                        string ebdtime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        if (!string.IsNullOrEmpty(json))
                        {
                            OCRBak item = new OCRBak();
                            item.IMG = img_result;
                            item.Json = json;
                            item.BDtime = sbdtime + "|" + ebdtime;
                            blockingCollection.Add(item);
                        }
                        else
                        {
                            string img_result1 = "";
                            if (IMG_Queue1.TryDequeue(out img_result1))
                            {
                                loginfo.Info("IMGQueueHandle,IMG_Queue1：出队成功" + img_result1 + IMG_Queue1.Count);
                            }
                        }
                        watch.Stop();  //停止监视
                        TimeSpan timespan = watch.Elapsed;  //获取当前实例测量得出的总时间
                        loginfo.Info("QueueUserWorkItem 时间：" + img_result + "," + timespan.TotalMilliseconds);
                        semaphoreSlim.Release();
                    });
                    loginfo.Info("IMGQueueHandle并发线程创建结束" + img_result+ IMG_Queue.Count);
                }
                else
                {
                    loginfo.Info($"IMGQueueHandle 出队失败，{IMG_Queue.Count}等待开始"+ img_result);
                    autoReset.WaitOne();
                    loginfo.Info($"IMGQueueHandle 出队失败，{IMG_Queue.Count}等待结束" + img_result);
                }
            }
        }
        /// <summary>
        /// OCR结果处理
        /// </summary>
        public void OCRQueueHandle()
        {
            while (true)
            {
                OCRBak item = blockingCollection.Take();
                Ocr_PD(item.Json, item.IMG,item.BDtime);
            }
        }

        


      
      

        public string Baidu_Re(string par)
        {
            try
            {
                string str = par.ToString();
                var image1 = File.ReadAllBytes(str);
                loginfo.Info("开始调用百度识别：" + str);
                string image2 = Convert.ToBase64String(image1);                
                string json = JsonConvert.SerializeObject(new { image1 = image2, groupid = groupid, id= str,place= place });                
                string result5 = WebApiHelper.Post<ResultMsg>(ocrapi, json, "", WebApiHelper.UploadContentType.Json);
                loginfo.Info("百度识别返回：" + str);
                return result5.ToString();
            }
            catch (Exception ex)
            {                
                //Updatemess("", "", "接口超时", par);
                string img_result = "";
                if (IMG_Queue1.TryDequeue(out img_result))
                {
                    loginfo.Info("Baidu_Re,IMG_Queue1：出队成功" + img_result + IMG_Queue1.Count);
                }
                loginfo.Error("Baidu_Ocr:", ex);
                return "";
                
            }
            
        }

        /// <summary>
        /// 识别数据处理
        /// </summary>
        /// <param name="re"></param>
        /// <param name="str"></param>
        private void Ocr_PD(string re, string str,string bdtime)
        {
            
            try
            {
                string img_result = "";
                loginfo.Info(str + "Ocr_PD开始:"+ str);
                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                watch.Start();  //开始监视代码运行时间
                JObject jo = (JObject)JsonConvert.DeserializeObject(re.ToString());
                if (jo.Root["error_msg"].ToString() == "SUCCESS")
                {
                    string x = jo.Root["result"].ToString();
                    JObject jo1 = (JObject)JsonConvert.DeserializeObject(x.ToString());
                    string info = jo1.Root["user_list"].ToString();
                    JArray jArray = (JArray)JsonConvert.DeserializeObject(info);
                    string scores = jArray.First["score"].ToString();
                    string user_info = jArray.First["user_info"].ToString();
                    if (Convert.ToDouble(scores) > Convert.ToInt32(score))
                    {                      

                        if (kq_dictionary.Keys.Contains(user_info))//不存在直接考勤，存在判断时间，避免短时间内重复入录
                        {
                            if (DateTime.Now.Subtract(kq_dictionary[user_info]).TotalMinutes > thresholdMinutes)//防止重复
                            {
                                kaoqin(str, scores, user_info, bdtime);
                            }
                            else
                            {
                                Updatemess(user_info, "考勤成功", "", str);
                          
                                loginfo.Info(user_info + "识别成功界面显示完成");
                             
                                if (IMG_Queue1.TryDequeue(out img_result))
                                {
                                    loginfo.Info("Ocr_PD,IMG_Queue1：出队成功" + img_result + IMG_Queue1.Count);
                                }
                                else
                                {
                                    loginfo.Info($"IMG_Queue1 出队失败，{IMG_Queue1.Count}等待开始" + img_result);
                                    autoReset.WaitOne();
                                    loginfo.Info($"IMG_Queue1 出队失败，{IMG_Queue1.Count}等待结束" + img_result);
                                }
                            }
                        }
                        else
                        {
                            kaoqin(str, scores, user_info, bdtime);

                        }

                    }                  
                    else
                    {
                        DateTime time = DateTime.Now;
                        string path1 = time.ToString("yyyyMMdd");
                        String sPath1 = facepath + path1 + @"\" + "陌生人" + @"\";
                       
                        if (scores.Length <= 4)
                        {
                            Updatemess("", "", "识别分数" + scores + "过低重新采集", str);

                            loginfo.Info(str + "识别分数" + scores + "过低重新采集界面显示完成");
                            str = CopyStr(sPath1, str, time, scores.Substring(0, 2));
                        }
                        else
                        {
                            Updatemess("", "", "识别分数" + scores.Substring(0, 4) + "过低重新采集", str);

                            loginfo.Info(str + "识别分数" + scores.Substring(0, 4) + "过低重新采集界面显示完成");
                            str = CopyStr(sPath1, str, time, scores.Substring(0, 2));
                        }
                   
                        if (IMG_Queue1.TryDequeue(out img_result))
                        {
                            loginfo.Info("Ocr_PD,IMG_Queue1：出队成功" + img_result + IMG_Queue1.Count);
                        }
                        else
                        {
                            loginfo.Info($"IMG_Queue1 出队失败，{IMG_Queue1.Count}等待开始" + img_result);
                            autoReset.WaitOne();
                            loginfo.Info($"IMG_Queue1 出队失败，{IMG_Queue1.Count}等待结束" + img_result);
                        }

                    }

                   

                }
                else
                {
                    Updatemess("", "", "没有检测到人脸", str);
                    loginfo.Info(str + "没有检测到人脸界面显示完成");
                    
                    if (IMG_Queue1.TryDequeue(out img_result))
                    {
                        loginfo.Info("Ocr_PD,IMG_Queue1：出队成功" + img_result + IMG_Queue1.Count);
                    }
                    else
                    {
                        loginfo.Info($"IMG_Queue1 出队失败，{IMG_Queue1.Count}等待开始" + img_result);
                        autoReset.WaitOne();
                        loginfo.Info($"IMG_Queue1 出队失败，{IMG_Queue1.Count}等待结束" + img_result);
                    }
                }

                watch.Stop();  //停止监视
                TimeSpan timespan = watch.Elapsed;  //获取当前实例测量得出的总时间
                loginfo.Info("Ocr_PD结束：" + timespan.TotalMilliseconds);
               
                if (IMG_Queue1.TryDequeue(out img_result))
                {
                    loginfo.Info("Ocr_PD,IMG_Queue1：出队成功" + img_result + IMG_Queue1.Count);
                }
                else
                {
                    loginfo.Info($"IMG_Queue1 出队失败，{IMG_Queue1.Count}等待开始" + img_result);
                    autoReset.WaitOne();
                    loginfo.Info($"IMG_Queue1 出队失败，{IMG_Queue1.Count}等待结束" + img_result);
                }
            }
            catch (Exception ex)
            {

                Updatemess("", "", "，系统异常", str);
                loginfo.Error("Ocr_PD",ex);
                string img_result = "";
                if (IMG_Queue1.TryDequeue(out img_result))
                {
                    loginfo.Info("Ocr_PD,IMG_Queue1：出队成功" + img_result + IMG_Queue1.Count);
                }
                else
                {
                    loginfo.Info($"IMG_Queue1 出队失败，{IMG_Queue1.Count}等待开始" + img_result);
                    autoReset.WaitOne();
                    loginfo.Info($"IMG_Queue1 出队失败，{IMG_Queue1.Count}等待结束" + img_result);
                }
            
            }
            

        }



        private string CopyStr(string sPath1, string str, DateTime time,string socre)
        {
            try
            {
                loginfo.Info(str + "图片拷贝开始");
                if (!Directory.Exists(sPath1))
                {
                    Directory.CreateDirectory(sPath1);
                }

                File.Copy(str, sPath1 + time.ToString("yyyyMMddHHmmssfff")+"_"+ socre + ".jpg", true);
                loginfo.Info(str + "图片复制完成");
            }
            catch (Exception ex)
            {

                loginfo.Error("CopyStr", ex);
                return "";
            }
            loginfo.Info(str + "图片拷贝结束");
            return sPath1 + time.ToString("yyyyMMddHHmmssfff") + ".jpg";
        }


        public void kaoqin(string str, string scores, string user_info,string bdtime)
        {
            string img_result = "";
            try
            {
                loginfo.Info(str + "kaoqin开始");
                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                watch.Start();  //开始监视代码运行时间
                DateTime time = DateTime.Now;
                string path1 = time.ToString("yyyyMMdd");
                String sPath1 = facepath + path1 + @"\" + user_info + @"\";
                str = CopyStr(sPath1, str, time, scores.Substring(0,2));
                    
                Updatemess(user_info, "考勤成功", "", str);
               
                if (kq_dictionary.Keys.Contains(user_info))
                {
                    kq_dictionary[user_info] = DateTime.Now;//记录考勤时间
                }
                else
                {
                    kq_dictionary.Add(user_info, DateTime.Now);//记录考勤时间
                }

                loginfo.Info(str + "，考勤成功界面显示完成:" + "," + user_info);
                watch.Stop();  //停止监视
                TimeSpan timespan = watch.Elapsed;  //获取当前实例测量得出的总时间

                loginfo.Info("kaoqin结束：" + timespan.TotalMilliseconds+ str+"队列" + IMG_Queue1.Count);
                
                if (IMG_Queue1.TryDequeue(out img_result))
                {
                 
                    loginfo.Info("kaoqin,IMG_Queue1：出队成功"  + IMG_Queue1.Count);
                   
                }
                else
                {
                    loginfo.Info($"kaoqin 出队失败，{IMG_Queue1.Count}等待开始" + img_result);
                    autoReset.WaitOne();
                    loginfo.Info($"kaoqin 出队失败，{IMG_Queue1.Count}等待结束" + img_result);
                }
               }
            catch (Exception ex)
            {
          
                Updatemess("", "", "考勤异常", str);
                loginfo.Error("kaoqin",ex);
                if (IMG_Queue1.TryDequeue(out img_result))
                {
                    loginfo.Info("kaoqin,IMG_Queue1：出队成功" + img_result + IMG_Queue1.Count);
                }
                else
                {
                    loginfo.Info($"IMG_Queue1 出队失败，{IMG_Queue1.Count}等待开始" + img_result);
                    autoReset.WaitOne();
                    loginfo.Info($"IMG_Queue1 出队失败，{IMG_Queue1.Count}等待结束" + img_result);
                }
            }
           
            
        }
        /// <summary>
        /// 微信推送
        /// </summary>
        /// <param name="mess"></param>
        private void sendwexin(object mess)
        {
            loginfo.Info("微信推送开始");
            string user_info_1 = mess.ToString();
  
            loginfo.Info(user_info_1+"微信推送结束");
           
        }


        /// <summary>
        /// 微信推送网站
        /// </summary>
        /// <param name="mess"></param>
        private void sendwexin1(object mess)
        {
           // loginfo.Info("微信推送网站开始");
           // string message = mess.ToString();
           // string[] message1 = message.Split(',');
           // string user_info_1 = message1[0];
           // string url = message1[1];
           //// weixin.SendMsg_QY("", user_info_1, "<a href =\""+weixinurl+"?"+url +"\">人脸分数低，请点击本文字进行人脸标记</a>");
           //// MessageBox.Show(weixinurl + "?" + url);
           // loginfo.Info(weixinurl + "?" + url + "微信推送网站结束");
           // if (wx_dictionary.Keys.Contains(user_info_1))
           // {
           //     wx_dictionary[user_info_1] = DateTime.Now;//记录推送时间
           // }
           // else
           // {
           //     wx_dictionary.Add(user_info_1, DateTime.Now);//记录推送时间
           // }
        }

        /// <summary>
        /// 更新界面
        /// </summary>
        /// <param name="txt1"></param>
        /// <param name="txt2"></param>
        /// <param name="worngmess"></param>
        private void Updatemess(string txt1, string txt2, string worngmess, string str)
        {
            loginfo.Info("Updatemess开始");
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();  //开始监视代码运行时间
            context.Send(t => {
                wornglab.Text = worngmess;
                mess.Text = txt1;
                mess1.Text = txt2;
               // pictureBox1.Image = Image.FromFile(str);
            }, null);
            watch.Stop();  //停止监视
            TimeSpan timespan = watch.Elapsed;  //获取当前实例测量得出的总时间

            loginfo.Info("Updatemess结束：" + timespan.TotalMilliseconds);
        }



      

        private void FaceTEST_FormClosing(object sender, FormClosingEventArgs e)
        {

           NETDEVSDK.NETDEV_StopRealPlay(m_lpDevHandlestrem);
            NETDEVSDK.NETDEV_Logout(m_lpDevHandle);
            NETDEVSDK.NETDEV_Cleanup();
            System.Environment.Exit(0);
        }

 
    }
}
