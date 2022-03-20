class Carrier {
    static init(url, onAck, onAnswer) {
        Carrier.connection = new signalR.HubConnectionBuilder().withUrl("/carrierhub").build();
        Carrier.connection.start();

        Carrier.url = url;
        Carrier.connection.on("ack",
            (data, id) => {
                Carrier.id = id;
                onAck(data);
            });
        Carrier.connection.on("answer",
            (data, id) => {
                Carrier.id = id;
                onAnswer(data);
            });
    }

    static post = function (path, json) {
        $.ajax({
            contentType: 'application/json',
            data: JSON.stringify(json),
            processData: false,
            type: 'POST',
            url: Carrier.url + path,
        });
}

    static sendAck() {
        Carrier.post(`ack/${Carrier.id}`);
    }

    static sendAnswer(answer) {
        Carrier.post(`answer/${Carrier.id}`, answer);
    }
}

export default Carrier;
