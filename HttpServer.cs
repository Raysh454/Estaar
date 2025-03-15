namespace Estaar;

using System.Net;
using System.Net.Sockets;
using System.IO.Pipelines;
using System.Buffers;

class HttpServer 
{
    Controller? controller;
    ClientHandler clientHandler = new ClientHandler();

    public HttpServer(String ip, int port, Controller controller)
    {
        this.controller = controller;

        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Start();

        Console.WriteLine($"Listening on: {ip}:{port}");

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Console.WriteLine($"Handling Client");
            ThreadPool.QueueUserWorkItem(state => clientHandler.HandleClient((TcpClient)state!, controller), client);

        }

    }
}

class ClientHandler
{
    HttpRequest? httpRequest;
    HttpResponse? httpResponse;
    NetworkStream? clientStream;
    Controller? controller;
    bool requestLineParsed = false;

    public async void HandleClient(TcpClient client, Controller controller)
    {
        using (client)
        using (NetworkStream clientStream = client.GetStream())
        {
            this.clientStream = clientStream;
            this.controller = controller;
            while (true)
            {
                httpRequest = new HttpRequest();
                httpResponse = new HttpResponse();
                requestLineParsed = false;

                try 
                {
                    await ProcessRequest();
                } catch (Exception Ex)
                {
                    Console.WriteLine($"Exception: {Ex.Message}");
                    Console.WriteLine(Ex.StackTrace);
                    httpResponse.SetHttpStatus(500);
                    httpResponse.SetBody("Error");
                    await clientStream.WriteAsync(httpResponse.CraftRawHttpResponse());
                    return;
                }
            }
        }
    }

    Task ProcessRequest()
    {
        Pipe pipe = new Pipe();
        Task writing = FillPipeAsync(pipe.Writer);
        Task reading = ReadPipeAsync(pipe.Reader);

        return Task.WhenAll(writing, reading);
    }

    async Task FillPipeAsync(PipeWriter writer)
    {
        const int minBufferSize = 512;

        while (true)
        {
            Memory<byte> memory = writer.GetMemory(minBufferSize);

            try
            {
                int bytesRead = await clientStream!.ReadAsync(memory);

                if (bytesRead == 0)
                {
                    break;
                }
                writer.Advance(bytesRead);
            } catch (Exception ex)
            {
                Console.WriteLine($"Caught Exception: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                break;
            }

            FlushResult result = await writer.FlushAsync();

            if (result.IsCompleted)
            {
                break;
            }
        }

        writer.Complete();
    }

    async Task ReadPipeAsync(PipeReader reader)
    {
        var headersAreProcessed = false;
        var contentLength = -1;
        var contentLengthRead = 0;
        var requestFullyParsed = false;
        var ms = new MemoryStream();

        while (true)
        {
            ReadResult result = await reader.ReadAsync();
            ReadOnlySequence<byte> buffer = result.Buffer;

            // Process Headers
            SequencePosition? position = buffer.Start;
            do
            {
                // Moves the buffer to the end of the headers
                headersAreProcessed = ProcessRequestHeaders(ref position, ref buffer);
            } while (!headersAreProcessed && !position.Equals(buffer.End));
            

            // Process Body
            if (headersAreProcessed)
            {
                ProcessRequestBody(ref ms, ref buffer, ref contentLengthRead);
            }

            requestFullyParsed = (contentLengthRead == contentLength) || (contentLength == -1 && headersAreProcessed);

            if (contentLengthRead == contentLength)
            {
                httpRequest!.SetBody(ms.ToArray());
            }

            if (requestFullyParsed)
            {
                httpRequest!.ParseGetParameters();
                ProcessParsedRequest();
                if (ConnectionClose()) // Break if Connection Header is set to close.
                    break;
                ResetReadPipeAsyncVariables(out headersAreProcessed, out contentLength, out contentLengthRead, out requestFullyParsed, ref ms); // Cleanup
            }

            if (result.IsCompleted)
            {
                break;
            }
            reader.AdvanceTo(buffer.Start, buffer.End);
        }
        reader.Complete();
    }

    private bool ConnectionClose()
    {
        var connection = httpRequest!.GetHeader("Connection");
        if (connection != null && connection.Equals("Close", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        return false;
    }

    private void ProcessRequestBody(ref MemoryStream ms, ref ReadOnlySequence<byte> buffer, ref int contentLengthRead)
    {

        int.TryParse(httpRequest!.GetHeader("Content-Length"), out var contentLength);
        if (contentLength != -1)
        {
            byte[] byteBuffer = buffer.Slice(0, buffer.Length).ToArray();
            ms.Write(byteBuffer, 0, byteBuffer.Length);
            contentLengthRead += byteBuffer.Length;
            buffer = buffer.Slice(buffer.End);
        }
    }

    private async void ProcessParsedRequest()
    {
        httpRequest!.PrintHttpRequest();
        HttpResponse response = controller!.HandleHttpRequest(httpRequest!);
        await clientStream!.WriteAsync(response.CraftRawHttpResponse());
    }

    private void ResetReadPipeAsyncVariables(out bool headersAreProcessed, out int contentLength, out int contentLengthRead, out bool requestFullyParsed, ref MemoryStream ms)
    {
        headersAreProcessed = false;
        contentLength = -1;
        contentLengthRead = 0;
        requestFullyParsed = false;
        requestLineParsed = false;
        this.httpRequest = new HttpRequest();
        this.httpResponse = new HttpResponse();
        ms.SetLength(0); 
    }


    private bool ProcessRequestHeaders(ref SequencePosition? position, ref ReadOnlySequence<byte> buffer)
    {
        do
        {
            position = buffer.PositionOf((byte)'\n');
            
            if (position != null)
            {
                byte[] byteBuffer = buffer.Slice(0, buffer.GetPosition(1, position.Value)).ToArray();
                
                if (byteBuffer[0] == 0xD && byteBuffer[1] == 0xA)
                {
                    buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
                    return true;
                }

                ProcessRequestHeader(byteBuffer);
                
                buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
            }
        } while (position != null);
        return false;
    }

    private void ProcessRequestHeader(byte[] buffer)
    {
        try
        {
            if (!requestLineParsed)
            {
                httpRequest!.ParseRequestLine(buffer);
                requestLineParsed = true;
            } else
            {
                httpRequest!.ParseHeader(buffer); 
            }
        } catch (Exception Ex)
        {
            Console.WriteLine($"Exception: {Ex.Message}");
            Console.WriteLine(Ex.StackTrace);
            throw new InvalidOperationException();
        }
    }
}
