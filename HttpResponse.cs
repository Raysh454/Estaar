namespace Estaar;

using System.Text;

class HttpResponse
{
    public int httpStatusCode {get; private set;} = 200;
    public string httpStatusMessage {get; private set;} = "OK";
    public string httpVersion {get; private set;} = "HTTP/1.1";
    public Dictionary<String, String> responseHeaders  {get; private set;}
    public byte[]? responseBody {get; private set;}

    public HttpResponse() {
        responseHeaders = new Dictionary<String, String>();
    }

    public void SetHeaders((String name, String value)[] headers) 
    {
        foreach (var header in headers)
        {
            if (!this.responseHeaders.ContainsKey(header.name))
            {
                this.responseHeaders.Add(header.name, header.value);
            } else
            {
                this.responseHeaders[header.name] = header.value;
            }
        }
    }

    public void SetBody(String body)
    {
        responseBody = Encoding.UTF8.GetBytes(body);
        if (!responseHeaders.ContainsKey("Content-Length"))
            responseHeaders.Add("Content-Length", responseBody.Length.ToString());
        responseHeaders["Content-Length"] = responseBody.Length.ToString();

    }

    public void SetBody(byte[] body)
    {
        responseBody = body;
        if (!responseHeaders.ContainsKey("Content-Length"))
            responseHeaders.Add("Content-Length", responseBody.Length.ToString());
        responseHeaders["Content-Length"] = responseBody.Length.ToString();
    }

    public void SetHttpVersion(String version)
    {
        String[] AcceptedVersions = ["1.0", "1.1", "2"];
        
        if (AcceptedVersions.Contains(version))
            httpVersion = $"HTTP/{version}";
    }

    public void PrintHttpResponse() 
    {
        Console.WriteLine($"{httpVersion} {httpStatusCode} {httpStatusMessage}");
        foreach (var header in responseHeaders)
        {
            Console.WriteLine($"{header.Key}: {header.Value}");
        }
        Console.WriteLine("");

        if (responseBody != null)
            Console.WriteLine(Encoding.UTF8.GetString(responseBody));
    }

    public void SetHttpStatus(int statusCode)
    {
        var statusMessages = new Dictionary<int, string>
        {
            { 200, "OK" },
            { 201, "Created" },
            { 204, "No Content" },
            { 301, "Moved Permanently" },
            { 302, "Found" },
            { 400, "Bad Request" },
            { 401, "Unauthorized" },
            { 403, "Forbidden" },
            { 404, "Not Found" },
            { 405, "Method Not Allowed" },
            { 408, "Request Timeout" },
            { 500, "Internal Server Error" },
            { 501, "Not Implemented" },
            { 502, "Bad Gateway" },
            { 503, "Service Unavailable" }
        };

        if (statusMessages.ContainsKey(statusCode))
        {
            httpStatusCode = statusCode;
            httpStatusMessage = statusMessages[statusCode];
        }
        else
        {
            // Default to 500 if the status code is unknown
            httpStatusCode = 500;
            httpStatusMessage = "Internal Server Error";
        }
    }

    public byte[] CraftRawHttpResponse()
    {
        byte[] httpResponse;
        using (MemoryStream ms = new MemoryStream())
        {
            ms.Write(Encoding.UTF8.GetBytes($"{httpVersion} {httpStatusCode} {httpStatusMessage}\r\n"));
            foreach (var header in responseHeaders)
            {
                ms.Write((Encoding.UTF8.GetBytes($"{header.Key}: {header.Value}\r\n")));
            }
            ms.Write(Encoding.UTF8.GetBytes("\r\n"));
            if (responseBody != null)
            {
                ms.Write(responseBody);
            }
            ms.Write(Encoding.UTF8.GetBytes("\r\n\r\n"));
            httpResponse = ms.ToArray(); 
        }
        return httpResponse;
    }
}

