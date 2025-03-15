namespace Estaar;

using System.Text;

class HttpRequest
{
    public String? httpMethod { get; private set;}
    public String? targetPath { get; private set;}
    public String? httpVersion { get; private set;}
    public Dictionary<String, String> requestHeaders {get; private set;}
    private byte[]? requestBody { get; set; }

    public HttpRequest() {
        requestHeaders = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
    }

    public void ParseRequestLine(byte[] buffer)
    {
        String? requestLine = "";

        for (int i = 0; i < buffer.Length; ++i)
        {
            if (buffer[i] == 0xA) {
                if (i == 0) break;
                requestLine = Encoding.UTF8.GetString(buffer, 0, i).Trim();
                break;
            }
        }

        if (String.IsNullOrEmpty(requestLine))
        {
            throw new InvalidOperationException("Invalid HTTP request line.");
        }
        
        String[] requestLineSplit = requestLine.Split(' ');
        
        if (requestLineSplit.Length != 3) {
            throw new InvalidOperationException("Invalid HTTP request line.");
        }

        if (!ValidateHttpMethod(requestLineSplit[0]) || !ValidateTargetPath(requestLineSplit[1]) || !ValidateHttpVersion(requestLineSplit[2]))
        {
            throw new InvalidOperationException("Invalid HTTP request line.");
        }

        this.httpMethod = requestLineSplit[0];
        this.targetPath = requestLineSplit[1];
        this.httpVersion = requestLineSplit[2];
    }

    private bool ValidateHttpMethod(String httpMethod) 
    {
        if (AcceptedHttpMethods.Contains(httpMethod))
            return true;

        return false;
    }

    private bool ValidateTargetPath(String targetPath) 
    {
        // Must start with a '/'
        if (string.IsNullOrEmpty(targetPath) || targetPath[0] != '/')
            return false;

        // Define invalid characters (based on RFC 3986)
        char[] invalidChars = { ' ', '{', '}', '|', '\\', '^', '[', ']', '`' };

        // Check if targetPath contains any invalid character
        if (targetPath.IndexOfAny(invalidChars) != -1)
            return false;

        // Ensure it only contains valid ASCII characters
        foreach (char c in targetPath)
        {
            if (c < 32 || c == 127) // Control characters and DEL are not allowed
                return false;
        }

        return true;
    }


    private bool ValidateHttpVersion(String httpVersion) 
    {
        if (AcceptedHttpVersions.Contains(httpVersion))
            return true;

        return false;
    }

    public void ParseHeader(byte[] buffer)
    {
        for (int i = 0; i < buffer.Length; ++i)
        {
            if (buffer[i] == (byte)':')
            {
                SetHeader(
                        Encoding.UTF8.GetString(buffer, 0, i).Trim(),
                        Encoding.UTF8.GetString(buffer, i+1, buffer[buffer.Length-2] == 0xD ? buffer.Length - 3 - i : buffer.Length - 2 - i)
                        );
                return;
            }
        }
    }

    private void SetHeader(string name, string value) 
    {
        if (name == "cookie")
        {
            if (requestHeaders.ContainsKey(name))
                requestHeaders[name] += $"; {value}";
            else
                requestHeaders[name] = value;
        }
        else
        {
            if (requestHeaders.ContainsKey(name))
                requestHeaders[name] += $", {value}";
            else
                requestHeaders[name] = value;
        }
    }



    public string? GetHeader(string name)
    {
        if (requestHeaders.ContainsKey(name))
            return requestHeaders[name];

        return null;
    }

    internal void SetBody(byte[] buffer)
    {
        requestBody = buffer;
    }

    internal void SetBody(string buffer)
    {
        requestBody = Encoding.UTF8.GetBytes(buffer);
    }



    public void ParseBody(byte[] buffer) 
    {
        int startOfBody = -1;
        for (int i = 0; i < buffer.Length; ++i)
        {
            if (    i+2 < buffer.Length && (
                    buffer[i] == 0xA && buffer[i+1] == 0xA ||
                    buffer[i] == 0xA && buffer[i+1] == 0xD && buffer[i+2] == 0xA))
            {
                // Start of body
                for (int j = i; j < buffer.Length; ++j)
                {
                    if (buffer[j] != 0xA && buffer[j] != 0xD) 
                    {
                        startOfBody = j;
                    }
                }
                break;
            }
        }
        if (startOfBody != -1) {
            // There is a request body
            requestBody = new byte[buffer.Length - startOfBody];
            Array.Copy(buffer, startOfBody, requestBody, 0, buffer.Length - startOfBody);
        }
    }
   
    public void PrintHttpRequest() {
        Console.WriteLine($"{httpMethod} {targetPath} {httpVersion}");
        foreach (var header in requestHeaders)
        {
            Console.WriteLine($"{header.Key}: {header.Value}");
        }
        Console.WriteLine("");

        if (requestBody != null)
            Console.WriteLine(Encoding.UTF8.GetString(requestBody));
    }

    private static readonly HashSet<string> AcceptedHttpMethods = new()
    {
        "GET", "POST", "PUT", "HEAD", "OPTIONS", "PATCH"
    };

    private static readonly HashSet<string> AcceptedHttpVersions = new()
    {
        "HTTP/1.0", "HTTP/1.1", "HTTP/2"
    };
}

