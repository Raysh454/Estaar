namespace Estaar;


class Program
{
    public static void Main() {
        var controller = new Controller();



        controller.AddRoute("/test", (HttpRequest) => {
                string responseBody = "<ul>";
                foreach (var kv in HttpRequest.GETParameters)
                {
                    responseBody += $"<li>{kv.Key} = {kv.Value}</li>";
                }
                responseBody += "</ul>";

                return new HttpResponse(200, responseBody);
                });



        var httpServer = new HttpServer("127.0.0.1", 8000, controller);
    }
}
