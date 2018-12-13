
using log4net;
using log4net.Config;
using log4net.Repository;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml;

namespace netudt.Log

{
    public sealed class FlashLogger
    {
        /// <summary>
        /// 记录消息Queue
        /// </summary>
        private readonly ConcurrentQueue<FlashLogMessage> queue;

        /// <summary>
        /// 信号
        /// </summary>
        private readonly ManualResetEvent resetEvent;

        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILog logger;

       /// <summary>
       /// 定义的日期格式
       /// </summary>
        private Dictionary<string, List<string>>  dicPrefix =new Dictionary<string, List<string>>();

        /// <summary>
        /// 保存的日期
        /// </summary>
        public static  int minDay = 7;

        /// <summary>
        /// 日志
        /// </summary>
        private static readonly FlashLogger flashLog = new FlashLogger();

        /// <summary>
        /// 日志文件配置
        /// </summary>
        public static string logConfigFile = "";

      /// <summary>
      /// 每次写入文件的日志数据量
      /// </summary>
        public static int maxNum = 100;
        private FlashLogger()
        {
            if (string.IsNullOrEmpty(logConfigFile))
            {
                FileInfo configFile = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "log4net.config"));
                if (!configFile.Exists)
                {
                    throw new Exception("未配置log4net配置文件！");
                }
                else
                {
                    logConfigFile = configFile.FullName;
                }
            }
            else
            {
                FileInfo logfile = new FileInfo(logConfigFile);
                if (!logfile.Exists)
                {
                    throw new Exception("未配置log4net配置文件！");
                }
            }
            // 设置日志配置文件路径
            ILoggerRepository repository = LogManager.CreateRepository("FlashRepository");
            XmlConfigurator.ConfigureAndWatch(repository,new FileInfo(logConfigFile));
             queue = new ConcurrentQueue<FlashLogMessage>();
            resetEvent = new ManualResetEvent(false);
            logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
            Register();
            Delete();
            load(logConfigFile);
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        /// <param name="file"></param>
        private void load(string file)
        {
            //读取文件
           string content= File.ReadAllText(file);
            XmlDocument log4netConfig = new XmlDocument();
            log4netConfig.LoadXml(content.ToLower());
            FindXMLNode(log4netConfig.DocumentElement);

        }

       

        /// <summary>
        /// 读取配置
        /// </summary>
        /// <param name="node"></param>
        private void FindXMLNode(XmlNode node)
        {
           XmlNodeList nodeList= node.SelectNodes("datepattern");
            XmlNode logDir = node.SelectSingleNode("file");
            if(nodeList!=null&&nodeList.Count>0)
            {
                List<string> list = null;
                string dir = logDir.Attributes["value"] == null ? "" : logDir.Attributes["value"].Value;
                if(!string.IsNullOrEmpty(dir))
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(dir);
                    dir = directoryInfo.Name;
                }
                if (!dicPrefix.TryGetValue(dir, out list))
                {
                    list = new List<string>();
                    dicPrefix[dir] = list;
                }
                foreach(XmlNode xmlNode in nodeList)
                {
                    string value = "";
                    value = xmlNode.InnerText;
                    if (!string.IsNullOrEmpty(value))
                    {
                      
                        list.Add(value);
                    }
                    value = xmlNode.Attributes["value"].Value;
                    if (!string.IsNullOrEmpty(value))
                    {
                        list.Add(value);
                    }
                    
                }
                return;
            }
           foreach(XmlNode child in node.ChildNodes)
            {
                 if(child.HasChildNodes)
                {
                    FindXMLNode(child);
                }
            }
        }

      

        /// <summary>
        /// 实现单例
        /// </summary>
        /// <returns></returns>
        public static FlashLogger Instance()
        {
            return flashLog;
        }

        /// <summary>
        /// 另一个线程记录日志，只在程序初始化时调用一次
        /// </summary>
        private void Register()
        {
            Thread t = new Thread(new ThreadStart(WriteLog));
            t.IsBackground = false;
            t.Name = "writeLog";
            t.Start();
        }

        private void Delete()
        {
            int waitTime =0;
            Thread del = new Thread(new ThreadStart(() =>
            {
                while (true)
                {
                    waitTime = minDay * 24 * 60 * 60 * 1000;
                    Thread.Sleep(waitTime);
                    foreach(KeyValuePair<string,List<string>> kv in dicPrefix)
                    {
                        foreach (string file in kv.Value)
                        {

                            //
                            string fileFomat = file.Replace("'", "");
                            GetLogDir(kv.Key, fileFomat);
                           
                        }
                    }
                }
            }));
            del.Name = "deleteLog";
            del.IsBackground = true;
            del.Start();
        }
        private void GetLogDir(string rootDir,string format)
        {
            string logFile = "";
            string[] logPath = format.Split(Path.DirectorySeparatorChar);
            if(logPath.Length>0)
            {
                logFile = logPath[logPath.Length - 1];
            }
            else
            {
                return;
            }
            FileInfo fileInfo = new FileInfo(logFile);
            string fileformat = logFile.Replace(fileInfo.Extension, "");
            if (!string.IsNullOrEmpty(rootDir))
            {
                //
                string[] files= Directory.GetFiles(rootDir,"*" +fileInfo.Extension);
                //
                if(files.Length>0)
                {
                    //有文件
                   string curTime = DateTime.Now.ToString(fileformat);//按照文件格式处理
                    foreach (string f in files)
                    {
                        FileInfo file = new FileInfo(f);
                        if (file.Name.CompareTo(curTime) < 0)
                        {
                            //删除
                            file.Delete();
                        }
                    }
                }
            }

            //逐级遍历
            List<string> curList = new List<string>(30);
            List<string> nextList = new List<string>(30);
            curList.Add(rootDir);
            List<string> deleDir = new List<string>();
            foreach (string pathFomart in logPath)
            {
                if(string.IsNullOrEmpty(pathFomart))
                {
                    continue;
                }
                if (pathFomart == logFile)
                {
                    //已经到了文件这一级
                    foreach (string dir in curList)
                    {
                        string[] files = Directory.GetFiles(dir, "*" + fileInfo.Extension);
                        int num = 0;
                        if (files.Length > 0)
                        {
                            //有文件
                            string curTime = DateTime.Now.ToString(fileformat);//按照文件格式处理
                            foreach (string f in files)
                            {
                                FileInfo file = new FileInfo(f);
                                if (file.Name.CompareTo(curTime) < 0)
                                {
                                    //删除
                                    file.Delete();
                                    num++;
                                }
                            }
                        }
                        if(files.Length==num)
                        {
                            deleDir.Add(dir);
                           
                        }
                    }
                }
                else
                {
                    string dirFomart = DateTime.Now.AddDays(-minDay).ToString(pathFomart);
                    //遍历子目录
                    foreach (string dir in curList)
                    {
                        string[] subdir = Directory.GetDirectories(dir);
                        if (subdir.Length > 0)
                        {
                            //说明有目录
                            foreach (string del in subdir)
                            {
                                if (del.CompareTo(dirFomart) < 0)
                                {
                                    //删除目录
                                    deleDir.Add(del);
                                   
                                }
                                else
                                {
                                    nextList.Add(del);//继续检查的目录
                                }
                            }

                        }
                    }
                    //
                    curList.Clear();
                    curList.AddRange(nextList);
                    nextList.Clear();
                }
            }
            DeleteDir(deleDir);
        }
        private void DeleteDir(List<string> delList)
        {
            if(delList.Count==0)
            {
                return;
            }
          for(int i=delList.Count-1;i>-1;i--)
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(delList[i]);
                if (directoryInfo.Exists)
                {
                    try
                    {
                        directoryInfo.Delete();
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }
      
        /// <summary>
        /// 从队列中写日志至磁盘
        /// </summary>
        private void WriteLog()
        {
            while (true)
            {
                // 等待信号通知,阻塞
                resetEvent.WaitOne();
                FlashLogMessage msg;
                int num = 0;
                // 判断是否有内容需要如磁盘 从列队中获取内容，并删除列队中的内容
                while (queue.Count > 0 && queue.TryDequeue(out msg))
                {
                    // 判断日志等级，然后写日志
                    switch (msg.Level)
                    {
                        case FlashLogLevel.Debug:
                            logger.Debug(msg.Message, msg.Exception);
                            break;
                        case FlashLogLevel.Info:
                            logger.Info(msg.Message, msg.Exception);
                            break;
                        case FlashLogLevel.Error:
                            logger.Error(msg.Message, msg.Exception);
                            break;
                        case FlashLogLevel.Warn:
                            logger.Warn(msg.Message, msg.Exception);
                            break;
                        case FlashLogLevel.Fatal:
                            logger.Fatal(msg.Message, msg.Exception);
                            break;
                    }
                    if(num++%maxNum==0)
                    {
                        Thread.Sleep(500);
                    }
                }

                // 重新设置信号
                resetEvent.Reset();
            }
        }


        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="message">日志文本</param>
        /// <param name="level">等级</param>
        /// <param name="ex">Exception</param>
        public void EnqueueMessage(string message, FlashLogLevel level, Exception ex = null)
        {
            if ((level == FlashLogLevel.Debug && logger.IsDebugEnabled)
             || (level == FlashLogLevel.Error && logger.IsErrorEnabled)
             || (level == FlashLogLevel.Fatal && logger.IsFatalEnabled)
             || (level == FlashLogLevel.Info && logger.IsInfoEnabled)
             || (level == FlashLogLevel.Warn && logger.IsWarnEnabled))
            {
                queue.Enqueue(new FlashLogMessage
                {
                    Message = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss,fff") + "]\r\n" + message,
                    Level = level,
                    Exception = ex
                });

                // 通知线程往磁盘中写日志
                resetEvent.Set();
            }
        }

        public static void Debug(string msg, Exception ex = null)
        {
            Instance().EnqueueMessage(msg, FlashLogLevel.Debug, ex);
        }

        public static void Error(string msg, Exception ex = null)
        {
            Instance().EnqueueMessage(msg, FlashLogLevel.Error, ex);
        }

        public static void Fatal(string msg, Exception ex = null)
        {
            Instance().EnqueueMessage(msg, FlashLogLevel.Fatal, ex);
        }

        public static void Info(string msg, Exception ex = null)
        {
            Instance().EnqueueMessage(msg, FlashLogLevel.Info, ex);
        }

        public static void Warn(string msg, Exception ex = null)
        {
            Instance().EnqueueMessage(msg, FlashLogLevel.Warn, ex);
        }

    }
}