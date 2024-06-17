
using SharedLibrary;
using System.Collections.Specialized;

namespace HotLibrary
{
    internal class HotLoad : BaseAction
    {
        public override void OnGet(NameValueCollection nameValueCollection)
        {
            MasterServer.MasterServer.Instance.HotLoad(nameValueCollection["version"]);
        }

        public override void OnPost(string content)
        {
            throw new NotImplementedException();
        }

        public override string OnResponse()
        {
            return "OK";
        }
    }
}
