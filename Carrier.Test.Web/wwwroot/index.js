import Carrier from '/lib/carrier.js';

var info = document.getElementById('info');
var btnAck = document.getElementById('btnAck');
var btnAns = document.getElementById('btnAns');

var show = e => {
    e.classList.remove('hidden');
    e.classList.add('visible');
}

var hide = e => {
    e.classList.remove('visible');
    e.classList.add('hidden');
}

var showAck = data => {
    info.innerHTML = data;
    btnAck.innerText = `Ack from ${Carrier.id}`;
    show(btnAck);
    show(info);
}

var sendAck = () => {
    Carrier.sendAck();
    hideAll();
}

var showAns = data => {
    info.innerHTML = data;
    btnAns.innerText = `Answer from ${Carrier.id}`;
    show(btnAns);
    show(info);
}

var sendAns = () => {
    var answer = {
        Id: Carrier.id,
        TrueAnswer: 42
    };
    Carrier.sendAnswer(answer);
    hideAll();
}

var hideAll = function () {
    hide(btnAck);
    hide(btnAns);
    hide(info);
}

Carrier.init("http://localhost:5183/",
    data => {
        showAck(data);
    },
    data => {
        showAns(data);
    });

document.getElementById('btnAck').addEventListener('click', sendAck);
document.getElementById('btnAns').addEventListener('click', sendAns);
