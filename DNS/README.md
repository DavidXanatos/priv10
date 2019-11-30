# DNS

A DNS library written in C#. It needs .NET 4 to run. Available through NuGet.

	Install-Package DNS

# Usage

The library exposes a `Request` and `Response` classes for parsing and creating DNS messages. These can be serialzed to byte arrays.

```C#
Request request = new Request();

request.RecursionDesired = true;
request.Id = 123;

UdpClient udp = new UdpClient();
IPEndPoint google = new IPEndPoint(IPAddress.Parse("8.8.8.8"), 53);

// Connect to google's DNS server
udp.Connect(google);
udp.Send(request.ToArray(), request.Size);

byte[] buffer = udp.Receive(ref google);
Response response = Response.FromArray(buffer);

// Outputs an human readable representation
Console.WriteLine(response);
```

### Client

The libray also includes a small client and a proxy server. Using the `ClientRequest` or the `DnsClient` class it is possible to send a request to a Domain Name Server. The request is first sent using UDP, if that fails (response is truncated), the request is sent again using TCP. This behaviour can be changed by supplying an `IRequestResolver` to the client constructor.

```C#
ClientRequest request = new ClientRequest("8.8.8.8");

// Request an IPv6 record for the foo.com domain
request.Questions.Add(new Question(Domain.FromString("foo.com"), RecordType.AAAA));
request.RecursionDesired = true;

ClientResponse response = request.Resolve();

// Get all the IPs for the foo.com domain
IList<IPAddress> ips = response.AnswerRecords
	.Where(r => r.Type == RecordType.AAAA)
	.Cast<IPAddressResourceRecord>()
	.Select(r => r.IPAddress)
	.ToList();
```

The `DnsClient` class contains some conveniance methods for creating instances of `ClientRequest` and resolving domains.

```C#
// Bind to a Domain Name Server
DnsClient client = new DnsClient("8.8.8.8");

// Create request bound to 8.8.8.8
ClientRequest request = client.Create();

// Returns a list of IPs
IList<IPAddress> ips = client.Resolve("foo.com");

// Get the domain name belonging to the IP (google.com)
string domain = client.Reverse("173.194.69.100");
```

### Server

The `DnsServer` class exposes a proxy Domain Name Server (UDP only). You can intercept domain name resolution requests and route them to specified IPs. The server is multi-threaded and spawns a thread for every request. It also emits an event on every request and every successful resolution. All the events are executed in the same separate thread.

```C#
// Proxy to google's DNS
DnsServer server = new DnsServer("8.8.8.8");

// Resolve these domain to localhost
server.MasterFile.AddIPAddressResourceRecord("google.com", "127.0.0.1");
server.MasterFile.AddIPAddressResourceRecord("github.com", "127.0.0.1");

// Log every request
server.Requested += (request) => Console.WriteLine(request);
// On every successful request log the request and the response
server.Responded += (request, response) => Console.WriteLine("{0} => {1}", request, response);

// Start the server (by default it listents on port 53)
server.Listen();
```

Note that since the events are executed in a separate thread it's not possible to modify the `request` instance in the `server.Requested` callback.

# License 

**This software is licensed under "MIT"**

> Copyright (c) 2012 Mirza Kapetanovic
> 
> Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the 'Software'), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
> 
> The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
> 
> THE SOFTWARE IS PROVIDED 'AS IS', WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

