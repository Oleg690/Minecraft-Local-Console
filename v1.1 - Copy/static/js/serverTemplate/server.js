const searchParams = new URLSearchParams(window.location.search);
const worldNumber = searchParams.get('n')

const allContents = document.querySelectorAll(".content");
const dropdowns = document.querySelectorAll('.dropdown');

let lastPath = '';

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
    result = JSON.parse(result);

    let infoCardColor = result[0];
    let infoPopupDescription = result[1];

    spawnPopup(infoCardColor, getTextForColor(infoCardColor), infoPopupDescription)
}

function run(folder, action) {
    lastPath = document.getElementById("directoryName").innerText;

    //console.log('Folder: ', folder)
    //console.log('Action: ', action)
    send(`\\cgi-bin\\serverFilesShower.py?lastPath=${lastPath}&folder=${folder}&worldNumber=${worldNumber}&action=${action}`, serverFilesResult);
}

function onLoadFunc() {
    send('\\cgi-bin\\getServers\\getServers.py', serverInfoResult)

    send(`\\cgi-bin\\serverFilesShower.py?worldNumber=${worldNumber}&action=to`, serverFilesResult);
}

function serverFilesResult(e) {
    result = e.target.response;
    result = JSON.parse(result);

    //console.log("HTML: ", result);
    //console.log("HTML[0]: ", result[0]);
    //console.log("HTML[1]: ", result[1]);

    if (result[1] == 'error'){
        console.log('error')
        spawnPopup('red', 'Error', result[0])
    }else if (result[0] != 'base') {
        document.querySelector('#par').innerHTML = result[0][0];
        document.querySelector('#mainDiv').innerHTML = result[0][1];
    }
}

function serverInfoResult(e) {
    serverData = e.target.response;
    serverData = JSON.parse(serverData);

    for (let i = 0; i < serverData.length; i++) {
        for (let j = 0; j < serverData[i].length; j++) {
            if (serverData[i][j] == worldNumber) {
                document.getElementById("serverName").innerText = serverData[i][0];
                window.value = serverData[i]
            }
        }
    }
}

function getTextForColor(infoCardColor) {
    if (infoCardColor == 'blue') {
        infoCardText = 'Info'
    } else if (infoCardColor == 'green') {
        infoCardText = 'Success'
    } else if (infoCardColor == 'red') {
        infoCardText = 'Error'
    }

    return infoCardText
}

function spawnPopup(infoCardColor, infoCardText, infoPopupDescription) {
    allPopupsDiv = document.querySelector('#infoPopups');
    allInfoPopups = document.querySelectorAll('.infoPopup');

    allPopupsDiv.innerHTML += ` <div class="infoPopup" id="${allInfoPopups.length + 1}0">
                                    <div class="color ${infoCardColor}" id="color">
                                        <div class="infoCardText" id="infoCardText">${infoCardText}</div>
                                        <div class="x-icon">
                                            <ion-icon name="close-outline" id="close-outline" onclick="deleteDiv(${allInfoPopups.length + 1})"></ion-icon>
                                        </div>
                                    </div>
                                    <p class="infoPopupDescription" id="infoPopupDescription">${infoPopupDescription}</p>
                                </div>`
}

function deleteDiv(e) {
    $(`#${e}0`).delay(0).fadeOut(500);
}

function send(url, result) {
    let oReq = new XMLHttpRequest();

    oReq.onload = result;

    oReq.open('GET', url);
    oReq.send();
}