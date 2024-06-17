
using System.Collections.Specialized;

namespace SharedLibrary
{
    public abstract class BaseAction
    {
        public abstract void OnPost(string content);

        public abstract void OnGet(NameValueCollection nameValueCollection);

        public abstract string OnResponse();
    }
}
