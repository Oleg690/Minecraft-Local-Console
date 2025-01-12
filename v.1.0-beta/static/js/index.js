const tabs = document.querySelectorAll(".sideBarSection");
const allContents = document.querySelectorAll(".content");

const input = document.getElementById('action')
const par = document.getElementById('par')
const div1 = document.getElementById('mainDiv')
let currentPath = 'D:\Server - Copy Minecraft';

let consoleInputCommand = document.getElementById("consoleInputCommand")

let serverStatus = document.getElementById("status")
serverStatus.innerHTML = ': Stopped'
let ServerStatusGlobal = 'Stopped'

let statusCircle = document.getElementById("statusCircle")

statusCircle.style.background = '#ff0000'

const textarea = document.getElementById('textarea')

tabs.forEach((tab, index) => {
    tab.addEventListener('click', () => {
        tabs.forEach(tab => { tab.classList.remove('active') })
        tab.classList.add('active')

        allContents.forEach(allContent => { allContent.classList.remove('tabShow') })
        allContents[index].classList.add('tabShow')
    })
})

let list = [2, 3]

function changeColor(e) {
    if (e == 1) {
        allOverAgain()
    }
    else if (e == 2) {
        stopServer('stop')

        document.getElementById('1').classList.add('activeBtn')
        document.getElementById('1').disabled = false;

        statusCircle.style.background = '#ff0000'

        list.forEach((n) => {
            document.getElementById(`${n}`).classList.remove("activeBtn")
            document.getElementById(`${n}`).disabled = true;
        })
    }
    else if (e == 3) {
        stopServer('restarting')

        document.getElementById('1').classList.add('activeBtn')
        document.getElementById('1').disabled = false;

        statusCircle.style.background = '#ff0000'

        list.forEach((n) => {
            document.getElementById(`${n}`).classList.remove("activeBtn")
            document.getElementById(`${n}`).disabled = true;
        })
    }
}

function allOverAgain() {
    start()

    document.getElementById('1').classList.remove('activeBtn')
    document.getElementById('1').disabled = true;

    statusCircle.style.background = '#34ff82'

    list.forEach((n) => {
        document.getElementById(`${n}`).classList.add("activeBtn")
        document.getElementById(`${n}`).disabled = false;
    })
}

function run(props) {
    if (props != undefined) {
        let url = '/cgi-bin/main.py?folder=' + props + '&current_path=' + currentPath;
        send(url)
        getLogs()
    }
    else {
        let url = '/cgi-bin/main.py';
        send(url)
        getLogs()
    }
}

function back(props) {
    let url = '/cgi-bin/main.py?folder=' + props + '&current_path=' + currentPath;
    send(url)
}

function send(url) {
    let oReq = new XMLHttpRequest();

    oReq.onload = runResult;

    oReq.open('GET', url);
    oReq.send();
}

function runResult(e) {
    let result = e.target.response;
    result = JSON.parse(result)
    // console.log(result)
    let title = result[0]
    let mainFolder = result[1]
    let mainFile = result[2]

    let start = 3;
    let end = result.length;

    let subCurrentPathSplit = result.slice(start, end);

    if (currentPath != subCurrentPathSplit.join("")) {
        currentPath = subCurrentPathSplit.join("")
    }

    par.innerHTML = title;
    div1.innerHTML = mainFolder;
    div1.innerHTML += mainFile;
}

function sendCommand() {
    consoleCommand = consoleInputCommand.value;

    consoleInputCommand.value = '';

    url = '/cgi-bin/commands/serverControl.py?action=' + consoleCommand

    sendNormal(url)
}

function sendNormal(url) {
    let oReq = new XMLHttpRequest();

    oReq.open('GET', url);
    oReq.send();
}

function stopServer(action) {
    serverStatus.innerHTML = ': Stopping'
    ServerStatusGlobal = 'Stopping'
    let url = '/cgi-bin/commands/serverControl.py?action=' + action
    let oReq = new XMLHttpRequest();

    oReq.onload = runVerifyStopServer;

    oReq.open('GET', url);
    oReq.send();
}

function runVerifyStopServer(e) {
    let result = e.target.response;
    result = JSON.parse(result)
    //console.log(result)
    if (result[0] == 'success') {
        serverStatus.innerHTML = ': Stopped'
        ServerStatusGlobal = 'Stopped'
        if (result[1] == 'restart') {
            allOverAgain()
        }
    } else {
        serverStatus.innerHTML = ': Error'
        ServerStatusGlobal = 'Error'
    }
}

function start() {
    let url = '/cgi-bin/minecraft_server.py'
    let oReq = new XMLHttpRequest();

    serverStatus.innerHTML = ': Starting'
    ServerStatusGlobal = 'Starting'

    oReq.onload = runResultForServer;

    oReq.open('GET', url);
    oReq.send();

    //console.log(url)

    setInterval(function () {
        if (ServerStatusGlobal != 'Stopped') {
            getLogs()
        }
    }, 2000);
}

function runResultForServer(e) {
    let result = e.target.response;
    //console.log(result)
    result = JSON.parse(result)
    if (result == 'success') {
        serverStatus.innerHTML = ': Running'
        ServerStatusGlobal = 'Running'
    } else {
        serverStatus.innerHTML = ': Error'
        ServerStatusGlobal = 'Error'
    }
}

function getLogs() {
    let url = '/cgi-bin/readLog.py';
    let oReq = new XMLHttpRequest();

    oReq.onload = runResultForConsoleLogs;

    oReq.open('GET', url);
    oReq.send();
}

function runResultForConsoleLogs(e) {
    let result = e.target.response;
    //console.log(result)
    result = JSON.parse(result)

    textarea.value = result

    //console.log('Output: ', result[0][0])
    //console.error('Errors: ', result[0][1])
}