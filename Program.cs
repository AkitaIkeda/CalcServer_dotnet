using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;


namespace server_cs
{
    class Program
    {
        static HttpListener listener;
        static Task listenTask;
        static string[] pref = new string[]{
            "http://localhost:8080/",
        };

        static void InitListener(){
            listener = new HttpListener();
            foreach (var s in pref)
                listener.Prefixes.Add(s);
        }
        static async Task ListenTask(){
            listener.Start();
            while(true){
                var result = await listener.GetContextAsync();
                ProcessReq(result);
            }
        }
        static async Task ProcessReq(object state){
            var context = (HttpListenerContext) state;
            var req = context.Request;
            var resp = context.Response;

            if(req.HttpMethod == "GET"){
                await SendStringResponse(resp, "Formula server", 200);
                return;
            }

            if(req.HttpMethod == "POST"){
                var options = new JsonSerializerOptions{
                    IgnoreNullValues = true,
                };
                JsonDocument d;
                try
                {
                    d = await JsonDocument.ParseAsync(req.InputStream);
                }
                catch (System.Exception)
                {
                    await SendStringResponse(resp, "Can't Parse to JsonDocument", 400);
                    return;
                }
                JsonElement dataTypeElem;
                string dataType = "string";
                if(d.RootElement.TryGetProperty("data-type", out dataTypeElem))
                    dataType = dataTypeElem.GetString();

                JsonElement bodyElem;
                if(!d.RootElement.TryGetProperty("data-body", out bodyElem) || bodyElem.GetString() == ""){
                    d.Dispose();
                    await SendStringResponse(resp, "Json must has 'data-body'", 400);
                    return;
                }
                Formula formula;
                if(dataType == "formula")
                    try
                    {
                        formula = new Formula(JsonSerializer.Deserialize<Formula._FormulaPOCO>(bodyElem.GetString(), options));
                    }
                    catch (System.Exception)
                    {
                        d.Dispose(); 
                        await SendStringResponse(resp, "Can't Deserialize", 400);
                        return;
                    }
                else if (dataType == "string")
                    try
                    {
                        formula = Formula.strToFormula(bodyElem.GetString());
                    }
                    catch (System.Exception)
                    {
                        d.Dispose(); 
                        await SendStringResponse(resp, "Can't convert to Formula", 400);
                        return;
                    }
                else {
                    d.Dispose(); 
                    await SendStringResponse(resp, string.Format("Can't convert {0} to Formula", dataType), 400);
                    return;
                } 
                d.Dispose(); 
                try
                {
                    await SendStringResponse(resp, "{\"answer\":"+formula.Calc()+"}", 200);
                }
                catch (System.Exception)
                {
                    await SendStringResponse(resp, "Can't calc", 500);
                }
            }
        }
        static async Task SendStringResponse(HttpListenerResponse resp, string t, int StatusCode){
            var data = Encoding.UTF8.GetBytes(t);
            resp.ContentEncoding = Encoding.UTF8;
            resp.ContentLength64 = data.LongLength;
            resp.ContentType = "text/plain";
            resp.StatusCode = StatusCode;
            await resp.OutputStream.WriteAsync(data, 0, data.Length);
            resp.Close();
        }

        static async Task Main(string[] args)
        {
            InitListener();
            listenTask = ListenTask();
            await listenTask;
        }
    }
}
