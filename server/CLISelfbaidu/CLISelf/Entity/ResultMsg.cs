using System.Collections.Generic;

namespace CLISelf.Entity
{
    public class ResultMsg
    {
        /// <summary>
        /// 是否通过
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// 消息信息
        /// </summary>
        public string Msg { get; set; }
        
    }
    public class ReflectModel
    {
        /// <summary>
        　　/// Controller名称,通过反射Desction属性获得
        　　/// </summary>
        public string ControllerDesction { set; get; }

        /// <summary>
        　　/// 控制器名称
        　　/// </summary>
        public string ControllerName { set; get; }

        /// <summary>
        　　/// action介绍
        　　/// </summary>
        public List<ReflectActionModel> ActionDesction { set; get; }
    }
    public class ReflectActionModel
    {
        /// 接口请求方式 
        /// </summary> 
        public string ActionType { set; get; }
        /// <summary>
        /// 请求地址
        /// </summary>
        public string ActionLink { set; get; }

        /// <summary> 
        /// 接口请求的数据
        /// </summary> 
        public List<Data> ActionData { set; get; }

        /// 接口名称 
        /// </summary> 
        public string ActionName { set; get; }
    }
    public class Data
    {
        /// <summary>
        /// 数据字段名
        /// </summary>
        public string name { set; get; }
        /// <summary>
        /// 字段简介
        /// </summary>
        public string des { set; get; }
    }
}
