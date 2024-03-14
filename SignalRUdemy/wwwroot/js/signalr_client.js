$(document).ready(function () {
    const broadcastMessageToAllClientHubMethodCall = "BroadcastMessageToAllClient";
    const receiveMessageForAllClientClientMethodCall = "ReceiveMessageForAllClient";
    const receiveMessageForCallerClient = "ReceiveMessageForCallerClient";
    const receiveConnectedClientCountAllClient = "ReceiveConnectedClientCountAllClient";
    const broadcastMessageToCallerClient = "BroadcastMessageToCallerClient";
    const broadcastMessageToOtherClient = "BroadcastMessageToOtherClient";
    const receiveMessageForOtherClient = "ReceiveMessageForOtherClient";
    const broadcastMessageToIndividualClient = "BroadcastMessageToIndividualClient";
    const receiveMessageForIndividualClient = "ReceiveMessageForIndividualClient";
    const receiveMessageForGroupClient = "ReceiveMessageForGroupClient";
    const receiveTypedMessageForAllClient = "ReceiveTypedMessageForAllClient";
    const broadcastTypedMessageToAllClient = "BroadcastTypedMessageToAllClient";
    const groupA = "GroupA";
    const groupB = "GroupB";

    const connection = new signalR.HubConnectionBuilder().withUrl("/exampleTypeSafeHub").configureLogging(signalR.LogLevel.Information).build();

    function start() {
        connection.start().then(() => {
            console.log("Hub ile Bağlantı Kuruldu");
            $("#connectionId").html(`Bağlantı Id: ${connection.connectionId}`);
        });
    }

    try {
        start();
    } catch (error) {
        console.error(error);
        setTimeout(() => start(), 5000);
    }



  
    //connection.on(receiveMessageForAllClientClientMethodCall, (mesaj) => {
    //    console.log("Gelen Mesaj", mesaj);
    //    const [price, baseCurrency, cryptoCurrency, dailyPercent] = mesaj.split(" ");
    //    const targetRow = $(`#cryptoTableBody tr[data-crypto="${cryptoCurrency}"]`);
    //    if (targetRow.length > 0) {
    //        targetRow.find(".price").text(price);
    //        targetRow.find(".daily-percent").text(dailyPercent);
    //    } else {
    //        const row = `<tr data-crypto="${cryptoCurrency}">
    //                    <td>${baseCurrency}</td>
    //                    <td>${cryptoCurrency}</td>
    //                    <td class="price">${price}</td>
    //                    <td class="daily-percent">${dailyPercent}</td>
    //                </tr>`;
    //        $("#cryptoTableBody").append(row);
    //    }
    //});

    connection.on(receiveMessageForAllClientClientMethodCall, (mesaj) => {
    console.log("Gelen Mesaj", mesaj);
    const [price, baseCurrency, cryptoCurrency, dailyPercent] = mesaj.split(" ");
    const targetRow = $(`#cryptoTableBody tr[data-crypto="${cryptoCurrency}"]`);
    if (targetRow.length > 0) {
        targetRow.find(".price").text(price);
        targetRow.find(".daily-percent").text(dailyPercent);

        // Update label with daily percent value
        $("#dailyPercentLabel").text(`Günlük Değişim: ${dailyPercent}`);
    } else {
        const row = `<tr data-crypto="${cryptoCurrency}">
                    <td>${baseCurrency}</td>
                    <td>${cryptoCurrency}</td>
                    <td class="price">${price}</td>
                    <td class="daily-percent">${dailyPercent}</td>
                </tr>`;
        $("#cryptoTableBody").append(row);

        // Update label with daily percent value
        $("#dailyPercentLabel").text(`Günlük Değişim: ${dailyPercent}`);
    }
});






    connection.on(receiveMessageForOtherClient, (mesaj) => {
        console.log("(Diğerleri) Gelen Mesaj", mesaj);
    });

    connection.on(receiveMessageForCallerClient, (mesaj) => {
        console.log("(Çağıran) Gelen Mesaj", mesaj);
    });

    connection.on(receiveMessageForIndividualClient, (mesaj) => {
        console.log("(Bireysel) Gelen Mesaj", mesaj);
    });

    connection.on(receiveMessageForGroupClient, (mesaj) => {
        console.log("(Grup) Gelen Mesaj", mesaj);
    });

    const span_client_count = $("#span-connected-client-count");
    connection.on(receiveConnectedClientCountAllClient, (count) => {
        span_client_count.text(count);
        console.log("Bağlı İstemci Sayısı", count);
    });

    let currentGroupList = [];
    function refreshGroupList() {
        $("#groupList").empty();
        currentGroupList.forEach(x => {
            $("#groupList").append(`<p>${x}</p>`);
        });
    }

    $("#btn-send-message-all-client").click(function () {
        const mesaj = "Merhaba Dünya";
        connection.invoke(broadcastMessageToAllClientHubMethodCall, mesaj).catch(err => console.error("hata", err));
    });

    $("#btn-send-message-caller-client").click(function () {
        const mesaj = "Merhaba Dünya";
        connection.invoke(broadcastMessageToCallerClient, mesaj).catch(err => console.error("hata", err));
    });

    $("#btn-send-message-other-client").click(function () {
        const mesaj = "Merhaba Dünya";
        connection.invoke(broadcastMessageToOtherClient, mesaj).catch(err => console.error("hata", err));
    });

    $("#btn-send-message-individual-client").click(function () {
        const mesaj = "Merhaba Dünya";
        const connectionId = $("#text-connectionId").val();
        connection.invoke(broadcastMessageToIndividualClient, connectionId, mesaj).catch(err => console.error("hata", err));
    });

    $("#btn-groupA-add").click(function () {
        connection.invoke("AddGroup", groupA).then(() => {
            currentGroupList.push(groupA);
            refreshGroupList();
        });
    });

    $("#btn-groupA-remove").click(function () {
        connection.invoke("RemoveGroup", groupA).then(() => {
            currentGroupList = currentGroupList.filter(x => x !== groupA);
            refreshGroupList();
        });
    });

    $("#btn-groupB-add").click(function () {
        connection.invoke("AddGroup", groupB).then(() => {
            currentGroupList.push(groupB);
            refreshGroupList();
        });
    });

    $("#btn-groupB-remove").click(function () {
        connection.invoke("RemoveGroup", groupB).then(() => {
            currentGroupList = currentGroupList.filter(x => x !== groupB);
            refreshGroupList();
        });
    });

    $("#btn-send-typed-message-other-client").click(function () {
        const urun = { id: 1, ad: "kalem", fiyat: 20 };
        connection.invoke(broadcastTypedMessageToAllClient, urun).catch(err => console.error("hata", err));
        console.log("Ürün Gönderildi.");
        $("#messageLabel").text(`Mesaj: ${newMessage}`);
    });
});
