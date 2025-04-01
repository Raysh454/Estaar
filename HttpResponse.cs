namespace Estaar;

using System.Text;
using System.Net;
using HeyRed.Mime;

class HttpResponse
{
    public int httpStatusCode {get; private set;} = 200;
    public string httpStatusMessage {get; private set;} = "OK";
    public string httpVersion {get; private set;} = "HTTP/1.1";
    public Dictionary<String, String> responseHeaders  {get; private set;}
    public byte[]? responseBody {get; private set;}

    public HttpResponse() {
        responseHeaders = new Dictionary<String, String>();
        responseHeaders.Add("Content-Type", "text/html");
    }

    public HttpResponse(int httpStatusCode, String requestBody)
    {
        responseHeaders = new Dictionary<String, String>();
        responseHeaders.Add("Content-Type", "text/html");
        this.httpStatusCode = httpStatusCode;
        SetBody(requestBody);
    }

    public void SetHeaders((String name, String value)[] headers) 
    {
        foreach (var header in headers)
        {
            SetHeader(header.name, header.value);
        }
    }

    public void SetHeader(string name, string value)
    {
        if (!this.responseHeaders.ContainsKey(name))
        {
            this.responseHeaders.Add(name, value);
        } else
        {
            this.responseHeaders[name] = value;
        }
    }

    public void SetBody(String body)
    {
        responseBody = Encoding.UTF8.GetBytes(body);
        SetHeader("Content-Length", responseBody.Length.ToString());
    }

    public void SetBody(byte[] body)
    {
        responseBody = body;
        SetHeader("Content-Length", responseBody.Length.ToString());
    }

    public void AppendToBody(string body)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        byte[] newBody = new byte[bodyBytes.Length + (responseBody != null ? responseBody.Length : 0)];
       
        if (responseBody != null)
            Array.Copy(responseBody, newBody, responseBody.Length);

        Array.Copy(bodyBytes, newBody, bodyBytes.Length);

        responseBody = newBody;
        SetHeader("Content-Length", responseBody.Length.ToString());
    }

    public void AppendToBody(byte[] body)
    {
        byte[] newBody = new byte[body.Length + (responseBody != null ? responseBody.Length : 0)];
       
        if (responseBody != null)
            Array.Copy(responseBody, newBody, responseBody.Length);

        Array.Copy(body, newBody, body.Length);

        responseBody = newBody;
        SetHeader("Content-Length", responseBody.Length.ToString());
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
        httpStatusCode = statusCode;
        httpStatusMessage = ((HttpStatusCode)statusCode).ToString();
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

    public void ServeFile(string filepath)
    {
        using(var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
        {
            var contentType = MimeTypesMap.GetMimeType(filepath);
            SetHeader("Content-Type", contentType);

            var buffer = new byte[1024];
            int bytesRead = 0;

            while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
            {
                AppendToBody(buffer[..bytesRead]);
            }
        }
    }
}

