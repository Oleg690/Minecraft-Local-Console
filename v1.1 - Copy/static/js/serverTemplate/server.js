const searchParams = new URLSearchParams(window.location.search);

const allContents = document.querySelectorAll(".content");
const dropdowns = document.querySelectorAll('.dropdown');

dropdowns.forEach(dropdown => {
    const select = dropdown.querySelector('.select');
    const caret = dropdown.querySelector('.caret');
    const menu = dropdown.querySelector('.menu');
    const options = dropdown.querySelectorAll('.menu li');
    const selected = dropdown.querySelector('.selected');

    select.addEventListener('click', () => {
        select.classList.toggle('select-clicked');
        caret.classList.toggle('caret-rotate');
        menu.classList.toggle('menu-open');
    });

    options.forEach((option, index) => {
        option.addEventListener('click', () => {
            selected.innerText = option.innerText;
            //console.log(selected.innerText)
            select.classList.remove('select-clicked');
            caret.classList.remove('caret-rotate');
            menu.classList.remove('menu-open');

            options.forEach(option => {
                option.classList.remove('active');
                allContents.forEach(allContent => { allContent.classList.remove('tabShow') })
            })
            option.classList.add('active');
            allContents[index].classList.add('tabShow')
        })
    })
});

function changeColor(prop, command) {
    let list = [2, 3]
    statusCircle = document.getElementById('statusCircle');

    var root = document.querySelector(':root');

    if (prop == 1) {
        runServerCommand(command)

        document.getElementById('1').classList.remove('activeBtn')
        document.getElementById('1').disabled = true;

        root.style.setProperty('--statusCircleColor', '#87FF2C');

        list.forEach((n) => {
            document.getElementById(`${n}`).classList.add("activeBtn")
            document.getElementById(`${n}`).disabled = false;
        })
    }
    else if (prop == 2) {
        runServerCommand(command)

        document.getElementById('1').classList.add('activeBtn')
        document.getElementById('1').disabled = false;

        root.style.setProperty('--statusCircleColor', '#FF3535');

        list.forEach((n) => {
            document.getElementById(`${n}`).classList.remove("activeBtn")
            document.getElementById(`${n}`).disabled = true;
        })
    }
    else if (prop == 3) {
        runServerCommand(command)

        document.getElementById('1').classList.add('activeBtn')
        document.getElementById('1').disabled = false;

        root.style.setProperty('--statusCircleColor', '#FF3535');

        list.forEach((n) => {
            document.getElementById(`${n}`).classList.remove("activeBtn")
            document.getElementById(`${n}`).disabled = true;
        })
    }
}

function runServerCommand(command) {
    if (command == 'start') {
        send(`\\cgi-bin\\commands\\startServer.py?number=${window.value[3]}`, serverCommandResult)
    }
}

function serverCommandResult(e) {
    let result = e.target.response;
    console.log(result);
    result = JSON.parse(result);
    console.log(result);
}

function onLoadFunc() {
    send('\\cgi-bin\\getServers\\getServers.py', serverInfoResult)
}

function serverInfoResult(e) {
    serverData = e.target.response;
    serverData = JSON.parse(serverData);

    for (let i = 0; i < serverData.length; i++) {
        for (let j = 0; j < serverData[i].length; j++) {
            if (serverData[i][j] == searchParams.get('n')) {
                document.getElementById("serverName").innerText = serverData[i][0];
                window.value = serverData[i]
            }
        }
    }
}

function send(url, result) {
    let oReq = new XMLHttpRequest();

    oReq.onload = result;

    oReq.open('GET', url);
    oReq.send();
}