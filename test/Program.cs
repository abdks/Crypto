// See https://aka.ms/new-console-template for more information

using Microsoft.AspNetCore.SignalR.Client;
using SignalRClientConsoleApp;

Console.WriteLine("SignalR Console Client");

var connection = new HubConnectionBuilder().WithUrl("https://localhost:7239/exampleTypeSafeHub").Build();

connection.StartAsync().ContinueWith((result) =>
{
    Console.WriteLine(result.IsCompletedSuccessfully ? "Connected" : "Connection failed");
});

connection.On<Product>("ReceiveTypedMessageForAllClient",
    (product) => { Console.WriteLine($"Received message: {product.Id}-{product.Name}-{product.Price}"); });

while (true)
{
    var key = Console.ReadLine();

    if (key == "exit") break;

    // Konsoldan girilen mesajı bir değişkene atayın
    var newMessage = key;

    // Mesajı konsola yazdırın
    Console.WriteLine($"Sending message: {newMessage}");

    // Mesajı SignalR üzerinden gönderin
    await connection.InvokeAsync("BroadcastMessageToAllClient", newMessage);

    // Etiketi güncelleyin
    Console.WriteLine($"Updating label: {newMessage}");
}
