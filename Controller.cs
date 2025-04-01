namespace Estaar;

class Controller
{
    Dictionary<String, Func<HttpRequest, HttpResponse>> routes;

    public Controller() 
    {
        routes = new Dictionary<string, Func<HttpRequest, HttpResponse>>();
    }

    public void AddRoute(String routeName, Func<HttpRequest, HttpResponse> callbackFunction)
    {
        if (routes.ContainsKey(routeName))
        {
            throw new InvalidOperationException($"Route {routeName} Already Exists.");
        }

        routes.Add(routeName, callbackFunction);
    }

    public HttpResponse HandleHttpRequest(HttpRequest request)
    {
        HttpResponse response = new HttpResponse();
        if (request.targetPath == null)
        {
            response.SetHttpStatus(404);
            response.SetBody("<h1> 404 Not Found </h1>");
            return response;
        }

        // TODO: Add dynamic routing eg: /user/{id}
        // TODO: Ignore # as well
        // TODO: Some kind of templating

        var targetPath = request.targetPath.Split('?')[0];
        if (routes.ContainsKey(targetPath))
        {
            return routes[targetPath](request);
        }

        // TODO: Create a better way to send 404s globally
        response.SetHttpStatus(404);
        response.SetBody("<h1> 404 Not Found </h1>");
        return response;
    }
}
