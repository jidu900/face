using CLISelf.Entity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace CLISelf.Controllers
{
    public class HomeController : ApiController
    {
        private static NLog.Logger m_logger = LogManager.GetCurrentClassLogger();
      
        string ApiKey = ConfigurationManager.AppSettings["Api Key"];
        string SecretKey = ConfigurationManager.AppSettings["Secret Key"];
        int timeout = Convert.ToInt32(ConfigurationManager.AppSettings["timeout"]);
        [HttpGet]
        public string Index(int iii,string sss)
        {
            return "hello world";
        }

        [HttpGet]
        public object Info()
        {
            List<ReflectModel> controls = new List<ReflectModel>();
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            System.Collections.Generic.List<Type> typeList = new List<Type>();
            var types = asm.GetTypes().Where(type => typeof(IHttpController).IsAssignableFrom(type));
            foreach (Type type in types)
            {
                string s = type.FullName.ToLower();
                typeList.Add(type);
            }
            typeList.Sort(delegate (Type type1, Type type2) { return type1.FullName.CompareTo(type2.FullName); });
            foreach (Type type in typeList)
            {
                ReflectModel rm = new ReflectModel();
                rm.ActionDesction = new List<ReflectActionModel>();

                string controller = type.Name.Replace("Controller", "");
                rm.ControllerName = controller;

                System.Reflection.MethodInfo[] memthods = type.GetMethods(
                    System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.DeclaredOnly);
                foreach (var m in memthods)
                {
                    if (m.DeclaringType.Attributes.HasFlag(System.Reflection.TypeAttributes.Public) != true)
                        continue;
                    ReflectActionModel ram = new ReflectActionModel();
                    string action = m.Name;
                    if (action.Contains("<") || action.Contains(">")) continue;

                    ram.ActionLink = controller + "/" + action;
                    ParameterInfo[] ps = m.GetParameters();

                    //获取data参数信息
                    List<Data> DataList = new List<Data>();
                    foreach (var par in ps) {
                        DataList.Add(new Data { name = par.Name, des=par.ParameterType.ToString() });
                    }
                    ram.ActionData = DataList;
                    rm.ActionDesction.Add(ram);
                }
                
                controls.Add(rm);
            }
            return controls;
        }



       


        [HttpPost]
    
        public String  FaceServices(dynamic obj)
            
        {

            try
            {
                
                string id = obj.id.ToString();
                string place = obj.place.ToString();
                string image1 = obj.image1.ToString();
                string groupid = obj.groupid.ToString();

                m_logger.Info("FaceServices调用开始:" + id + "," + place);
                var client = new Baidu.Aip.Face.Face(ApiKey, SecretKey);
                client.Timeout = timeout;
                var result = client.Search(image1, "BASE64", groupid);
                m_logger.Info(id + "," + place + "FaceServices调用结束：" + result.ToString());
                return result.ToString();
            }
            catch (Exception ex)
            {
                m_logger.Error("FaceServices"+ex);
                return "";
            }
            
             



        }


        
        




        



    }
}
