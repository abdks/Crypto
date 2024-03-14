using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Quartz;
using Quartz.Impl;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using test;

namespace TestConsoleApp
{
    class Program
    {
        static async Task Main()
        {
            Console.WriteLine("SignalR Console Client");

            var connection = new HubConnectionBuilder().WithUrl("https://localhost:7239/exampleTypeSafeHub").Build();

            await connection.StartAsync();
            Console.WriteLine(connection.State.ToString());

            IScheduler scheduler = await new StdSchedulerFactory().GetScheduler();
            await scheduler.Start();

            string[] baseCurrencies = { "TRY", "EUR" };
            string[] cryptoCurrencies = { "BTC", "SOL", "ETH" };

            // Her bir kripto para birimi ve para cinsi için bir iş oluştur
            foreach (var baseCurrency in baseCurrencies)
            {
                foreach (var cryptoCurrency in cryptoCurrencies)
                {
                    var jobData = new JobDataMap();
                    jobData.Add("Connection", connection);
                    jobData.Add("BaseCurrency", baseCurrency);
                    jobData.Add("CryptoCurrency", cryptoCurrency);

                    IJobDetail job = JobBuilder.Create<ApiCallJob>()
                        .UsingJobData(jobData)
                        .Build();

                    ITrigger trigger = TriggerBuilder.Create()
                        .WithIdentity($"trigger_{baseCurrency}_{cryptoCurrency}", "group1")
                        .StartNow()
                        .WithSimpleSchedule(x => x
                            .WithIntervalInSeconds(20)
                            .RepeatForever())
                        .Build();

                    await scheduler.ScheduleJob(job, trigger);
                }
            }

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
            var (price, dailyPercent, timestamp) = await PerformApiCall(baseCurrency, cryptoCurrency);

            Console.WriteLine($"Para Birimi: {baseCurrency}");
            Console.WriteLine($"Kripto Para: {cryptoCurrency}");
            Console.WriteLine($"Fiyat: {price}");
            Console.WriteLine($"Günlük Değişim: {dailyPercent}");
            Console.WriteLine($"Zaman: {timestamp:dd/MM/yyyy HH:mm}");

            // Mesajı SignalR üzerinden gönderin
            await connection.InvokeAsync("BroadcastMessageToAllClient", $"{price} {baseCurrency} {cryptoCurrency} {dailyPercent}");

            // Veritabanına kayıt ekleme
            AddDataToDatabase(baseCurrency, cryptoCurrency, price, timestamp,dailyPercent);
        }

        private async Task<(double, double, DateTime)> PerformApiCall(string baseCurrency, string cryptoCurrency)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"https://api.btcturk.com/api/v2/ticker?pairSymbol={cryptoCurrency}{baseCurrency}");
                response.EnsureSuccessStatusCode();

                var body = await response.Content.ReadAsStringAsync();
                var resultObject = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResult>(body);

                // API'den gelen veriyi kullanarak fiyat, günlük değişim ve tarih bilgilerini al
                var price = resultObject.data[0].last; // LAST değerini kullan
                var dailyPercent = resultObject.data[0].dailyPercent; // Günlük değişim değerini kullan
                var timestamp = DateTime.Now; // API'den doğrudan tarih bilgisi gelmiyorsa, şu anki zamanı kullanabilirsiniz.

                return (price, dailyPercent, timestamp);
            }
        }

        private void AddDataToDatabase(string baseCurrency, string cryptoCurrency, double price, DateTime timestamp,double dailyPercent)
        {
            using (var context = new AppDbContext())
            {
                var cryptoCurrencyData = new CryptoCurrency
                {
                    KriptoPara = cryptoCurrency,
                    ParaCinsi = baseCurrency,
                    Deger = (decimal)price,
                    Tarih = timestamp,
                    Gunluk = (decimal)dailyPercent
                };

                context.CryptoCurrencies.Add(cryptoCurrencyData);
                context.SaveChanges();
            }
        }
    }

    public class ApiResult
    {
        public List<Data> data { get; set; }
    }

    public class Data
    {
        public double last { get; set; }
        public double dailyPercent { get; set; }
    }
}
