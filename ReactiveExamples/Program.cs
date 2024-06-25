using System.Net.Sockets;
using System.Reactive.Linq;

IObservable<string> GetObservableTcpClient(CancellationToken cancellationToken = default)
{
    var withoutRetry = Observable.Create<string>(async (obs, innerCancel) =>
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, innerCancel);
        var mergedCancellationToken = cts.Token;

        while (!mergedCancellationToken.IsCancellationRequested)
        {
            using TcpClient tcpClient = new();
            await tcpClient.ConnectAsync("153.44.253.27", 5631, mergedCancellationToken);
            await using var networkStream = tcpClient.GetStream();
            using StreamReader streamReader = new(networkStream);

            var retryAttempt = 0;

            while (tcpClient.Connected)
            {
                while (networkStream.DataAvailable && !mergedCancellationToken.IsCancellationRequested)
                {
                    var line = await streamReader.ReadLineAsync();
                    if (line is not null)
                        obs.OnNext(line);

                    retryAttempt = 0;
                }

                if (mergedCancellationToken.IsCancellationRequested || retryAttempt == 100)
                    break;

                await Task.Delay(TimeSpan.FromSeconds(1), mergedCancellationToken);

                retryAttempt++;
            }
        }
    });

    return withoutRetry.Retry();
}

// Classic subscription
//GetObservableTcpClient().Subscribe(x => Console.WriteLine("Sub1" + x));

// Multiple subscriptions using RefCount
// https://introtorx.com/chapters/publishing-operators
var rc = GetObservableTcpClient().Publish().RefCount();

rc.Subscribe(x => Console.WriteLine($"Sub1: {x} ({DateTime.Now})"));
rc.Subscribe(x => Console.WriteLine($"Sub2: {x} ({DateTime.Now})"));

Thread.Sleep(6000);

Console.WriteLine();
Console.WriteLine("Adding more subscribers");
Console.WriteLine();

rc.Subscribe(x => Console.WriteLine($"Sub3: {x} ({DateTime.Now})"));
rc.Subscribe(x => Console.WriteLine($"Sub4: {x} ({DateTime.Now})"));

Console.ReadKey();
