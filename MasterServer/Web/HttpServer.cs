
using Serilog;
using SharedLibrary;
using System.Net;
using System.Text;

namespace MasterServer
{
    public class HttpServer
    {
        public static HttpServer Instance { get; private set; } = new HttpServer();

        private HttpListener httpListener;

        private string prefixe = "http://127.0.0.1:8080/";

        public HttpServer()
        {
            httpListener = new HttpListener();

            RegisterPrefixes();
        }

        public void RegisterPrefixes()
        {
            httpListener.Prefixes.Add(prefixe);
        }

        public void Start()
        {
            try
            {
                httpListener.Start();

                httpListener.BeginGetContext(GetContextCallBack, httpListener);

                Log.Information("启动HttpServer");
            }
            catch (Exception ex)
            {
                Log.Error("启动HttpServer失败!!!");

                Log.Error(ex.ToString());
            }

        }

        private void GetContextCallBack(IAsyncResult asyncResult)
        {
            try
            {
                HttpListener? httpListener = asyncResult.AsyncState as HttpListener;
                if (httpListener != null && httpListener.IsListening)
                {
                    HttpListenerContext context = httpListener.EndGetContext(asyncResult);

                    httpListener.BeginGetContext(GetContextCallBack, httpListener);

                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;

                    if (request.Url == null)
                    {
                        Send404(response);
                        return;
                    }

                    BaseAction? baseAction = HotManager.Instance.GetHttpAction(request.Url.LocalPath);

                    if (baseAction == null)
                    {
                        Send404(response);
                        return;
                    }

                    try
                    {
                        switch (request.HttpMethod)
                        {
                            case "POST":
                                Stream stream = context.Request.InputStream;
                                StreamReader streamReader = new StreamReader(stream, Encoding.UTF8);
                                string content = streamReader.ReadToEnd();
                                baseAction.OnPost(content);
                                break;
                            case "GET":
                                var data = request.QueryString;
                                baseAction.OnGet(data);
                                break;
                        }


                        response.StatusCode = (int)HttpStatusCode.OK;
                        response.ContentType = "application/json;charset=UTF-8";
                        response.ContentEncoding = Encoding.UTF8;
                        response.AddHeader("Content-Type", "application/json;charset=UTF-8");

                        using (StreamWriter writer = new StreamWriter(response.OutputStream, Encoding.UTF8))
                        {
                            writer.Write(baseAction.OnResponse());
                            writer.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());

                        Send404(response);
                    }


                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }


        private void Send404(HttpListenerResponse response)
        {
            response.StatusCode = (int)HttpStatusCode.NotFound;
            response.ContentType = "application/json;charset=UTF-8";
            response.ContentEncoding = Encoding.UTF8;
            response.AddHeader("Content-Type", "application/json;charset=UTF-8");

            response.Close();
        }

    }
}
