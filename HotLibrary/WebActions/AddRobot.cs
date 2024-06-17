
using SharedLibrary;
using System.Collections.Specialized;

namespace HotLibrary
{
    public class AddRobot : BaseAction
    {
        public override void OnGet(NameValueCollection nameValueCollection)
        {
            //string useridStr = nameValueCollection["userid"];
            //string roomidStr =nameValueCollection["roomid"];
            //string roadidStr =nameValueCollection["roadid"];
            //string firstName =nameValueCollection["firstname"];
            //string lastname =nameValueCollection["lastname"];

            //string avatar =nameValueCollection["avatar"];
            //string gender =nameValueCollection["gender"];
            //string nationality =nameValueCollection["country"];
 
        }

        public override void OnPost(string content)
        {
            //throw new NotImplementedException();
        }

        public override string OnResponse()
        {
            throw new NotImplementedException();
        }
    }
}
