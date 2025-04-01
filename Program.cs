namespace Estaar;


class Program
{
    public static void Main() {
        var controller = new Controller();



        controller.AddRoute("/test", (HttpRequest) => {
                var httpResponse = new HttpResponse();
                httpResponse.SetHttpStatus(200);
                httpResponse.ServeFile("test.html");
                return httpResponse;
                });



        var httpServer = new HttpServer("127.0.0.1", 8000, controller);
    }
}
