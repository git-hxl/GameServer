

using SharedLibrary;
using System.Collections.Specialized;
using System.Text;

namespace HotLibrary
{
    /// <summary>
    /// 玩家上传日志
    /// </summary>
    public class UploadLog : BaseAction
    {
        public override void OnGet(NameValueCollection nameValueCollection)
        {
            //throw new NotImplementedException();
            string userID = nameValueCollection["userid"];

            string log = nameValueCollection["log"];

            if (string.IsNullOrEmpty(userID) || string.IsNullOrEmpty(log))
            {
                return;
            }

            string logPath = $"./PlayerLogs/{DateTime.Now.ToString("yyyyMMdd")}/{userID}.log";

            string directory = Path.GetDirectoryName(logPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (FileStream stream = new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                log += "\n";
                byte[] data = Encoding.UTF8.GetBytes(log);
                stream.Write(data, 0, data.Length);
            }
        }

        public override void OnPost(string content)
        {
            //throw new NotImplementedException();
        }

        public override string OnResponse()
        {
            return "Hot 上传成功";
        }
    }
}
