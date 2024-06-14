

using System.Collections.Specialized;

namespace MasterServer
{
    public class CloseRoom : BaseAction
    {
        public override void OnGet(NameValueCollection nameValueCollection)
        {
            //throw new NotImplementedException();
        }

        public override void OnPost(string content)
        {
            //throw new NotImplementedException();
        }

        public override string OnResponse()
        {
            return "关闭房间成功";
        }
    }
}
