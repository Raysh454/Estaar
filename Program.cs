namespace Estaar;


class Program
{
    public static void Main() {
        var controller = new Controller();



        controller.AddRoute("/test", (HttpRequest) => {
                var response = new HttpResponse();
                response.SetBody("<h1>Hello!</h1>");
                return response;
                });



        var httpServer = new HttpServer("127.0.0.1", 8000, controller);
    }
}
