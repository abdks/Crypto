
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Quartz;
using Quartz.Impl;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("SignalR Console Client");

        var connection = new HubConnectionBuilder().WithUrl("https://localhost:7239/exampleTypeSafeHub").Build();

        connection.StartAsync().ContinueWith((result) =>
        {
            Console.WriteLine(result.IsCompletedSuccessfully ? "Connected" : "Connection failed");
        });

        IScheduler scheduler = await new StdSchedulerFactory().GetScheduler();
        await scheduler.Start();

        Console.WriteLine("Hangi para birimiyle çevirmek istiyorsunuz? (TRY, USD, EUR, etc.): ");
        string baseCurrency = Console.ReadLine();

        Console.WriteLine("Hangi kripto paranın değerini öğrenmek istiyorsunuz? (SOL, BTC, ETH, etc.): ");
        string cryptoCurrency = Console.ReadLine();

        var jobData = new JobDataMap();
        jobData.Add("Connection", connection);
        jobData.Add("BaseCurrency", baseCurrency);
        jobData.Add("CryptoCurrency", cryptoCurrency);

        // Zamanlanmış görevi başlat
        IJobDetail job = JobBuilder.Create<ApiCallJob>()
            .UsingJobData(jobData)
            .Build();

        ITrigger trigger = TriggerBuilder.Create()
            .WithIdentity("trigger1", "group1")
            .StartNow()
            .WithSimpleSchedule(x => x
                .WithIntervalInSeconds(30)
                .RepeatForever())
            .Build();

        await scheduler.ScheduleJob(job, trigger);

        Console.WriteLine("Quartz.NET çalışıyor... Çıkmak için bir tuşa basın.");
        Console.ReadLine();

        await scheduler.Shutdown();
    }
}

public class ApiCallJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        string baseCurrency = context.JobDetail.JobDataMap.GetString("BaseCurrency");
        string cryptoCurrency = context.JobDetail.JobDataMap.GetString("CryptoCurrency");
        var connection = (HubConnection)context.JobDetail.JobDataMap["Connection"];

        // API çağrısı işlemi burada gerçekleştirilecek
        var (price, timestamp) = await PerformApiCall(baseCurrency, cryptoCurrency);

        Console.WriteLine($"Hangi para: {baseCurrency}");
        Console.WriteLine($"Hangi kripto: {cryptoCurrency}");
        Console.WriteLine($"Cevap: {price} TL");
        Console.WriteLine($"Zaman: {timestamp:dd/MM/yyyy HH:mm}");

        // Mesajı SignalR üzerinden gönderin
        await connection.InvokeAsync("BroadcastMessageToAllClient", $"{price} TL");
    }

    private static async Task<(double, DateTime)> PerformApiCall(string baseCurrency, string cryptoCurrency)
    {
        using (var client = new HttpClient())
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://crypto-market-prices.p.rapidapi.com/exchanges/binance/{cryptoCurrency}?base={baseCurrency}"),
                Headers =
                {
                    { "X-RapidAPI-Key", "87f0cc9cedmshbb8f8d8375be303p11b150jsne42a6adcab3b" },
                    { "X-RapidAPI-Host", "crypto-market-prices.p.rapidapi.com" },
                },
            };

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            var resultObject = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResult>(body);

            // API'den gelen veriyi kullanarak fiyat ve tarih bilgilerini al
            var price = resultObject.data.price;
            var timestamp = DateTime.Now; // API'den doğrudan tarih bilgisi gelmiyorsa, şu anki zamanı kullanabilirsiniz.

            return (price, timestamp);
        }
    }
}

public class ApiResult
{
    public Data data { get; set; }
}

public class Data
{
    public double price { get; set; }
}



