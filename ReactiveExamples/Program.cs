using System.Net.Sockets;
using System.Reactive.Linq;

IObservable<string> GetObservableTcpCLient(CancellationToken cancellationToken = default)
{
    var withoutRetry = Observable.Create<string>(async (obs, innerCancel) =>
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, innerCancel);
        var mergedCancellationToken = cts.Token;

        while (!mergedCancellationToken.IsCancellationRequested)
        {
            using TcpClient tcpClient = new();
            await tcpClient.ConnectAsync("153.44.253.27", 5631, mergedCancellationToken);
            await using NetworkStream networkStream = tcpClient.GetStream();
            using StreamReader streamReader = new(networkStream);

            int retryAttempt = 0;

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

GetObservableTcpCLient().Subscribe(x => Console.WriteLine("First" + x));

Console.WriteLine("Press any key to cancel");
Console.ReadKey();
