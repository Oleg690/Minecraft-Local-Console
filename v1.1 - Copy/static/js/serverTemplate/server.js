const theme1 = "ace/theme/tomorrow_night_eighties"
const theme2 = "ace/theme/idle_fingers"

const searchParams = new URLSearchParams(window.location.search);
const worldNumber = searchParams.get('n')

const allContents = document.querySelectorAll(".content");
const dropdowns = document.querySelectorAll('.dropdown');

let lastPath = '';

let divForFolder = document.querySelector('#mainDivForFolders')
let divForInnerFile = document.querySelector('.mainDivForFilesAndBtn')

function onLoadFunc() {
    send('\\cgi-bin\\getServers\\getServers.py', serverInfoResult)

    send(`\\cgi-bin\\fileHandler\\serverFilesHandler.py?worldNumber=${worldNumber}&action=firstLoad&folderOrFile=folder`, serverFilesResult);
}

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

    let infoCardText = result[0];
    let infoPopupDescription = result[1];

    spawnPopup(infoCardText, infoPopupDescription)
}

function run(folderTo, action, folderOrFile) {
    let lastPath = document.getElementById("directoryName").innerText;

    if (folderOrFile == 'folder') {
        send(`\\cgi-bin\\fileHandler\\serverFilesHandler.py?lastPath=${lastPath}&folderTo=${folderTo}&worldNumber=${worldNumber}&action=${action}&folderOrFile=${folderOrFile}`, serverFilesResult);
    } else if (folderOrFile == 'file') {
        send(`\\cgi-bin\\fileHandler\\serverFilesHandler.py?&folderTo=${folderTo}&worldNumber=${worldNumber}&folderOrFile=file`, handleInnerFileResult);
    }
}

function back(folderTo, action, folderOrFile) {
    let lastPath = document.getElementById("directoryName").innerText;

    send(`\\cgi-bin\\fileHandler\\serverFilesHandler.py?lastPath=${lastPath}&folderTo=${folderTo}&worldNumber=${worldNumber}&action=${action}&folderOrFile=${folderOrFile}`, serverFilesResult);

    document.querySelector('.undoBtn').setAttribute("onClick", "run('', 'back', 'folder')");
}

function saveFile() {
    let lastPath = document.getElementById("directoryName").innerText;
    let editor = ace.edit(mainDivForFiles)
    let editorValues = editor.getValue()
    console.log("Values from JS: ", editorValues)

    //send(`\\cgi-bin\\fileHandler\\insertIntoFile.py?lastPath=${lastPath}&worldNumber=${worldNumber}&values=${editorValues}`, saveFileHandler);

    fetch('\\cgi-bin\\fileHandler\\insertIntoFile.py', {
        method: 'POST',  // Use PUT for updating
        headers: {
            'Content-Type': 'application/json',  // Set the content type to JSON
        },
        body: JSON.stringify({
            lastPath: lastPath,
            worldNumber: worldNumber,
            values: editorValues  // Your data to update
        })
    })
    .then(response => response.json())
    .then(data => spawnPopup('Success', data[1]))
    .catch((error) => spawnPopup('Error', error[1]));
}

function serverFilesResult(e) {
    result = e.target.response;
    result = JSON.parse(result);

    //console.log("HTML[0]: ", result[0]);
    //console.log("HTML[1]: ", result[1]);

    if (result[1] == 'Error') {
        spawnPopup('Error', result[0])
    } else if (result[1] != 'Base_Directory') {
        document.querySelector('#par').innerHTML = result[0][0];
        document.querySelector('#mainDivForFolders').innerHTML = result[0][1];
    }

    hideEditTab()
}

function handleInnerFileResult(e) {
    result = e.target.response;
    result = JSON.parse(result);

    let title = result[0][0]
    let innerFile = result[0][1]
    let fileEx = result[0][2]

    let filesExToEdit = [['properties', 'properties'], ['json', 'json'], ['txt', 'properties']]

    for (let i = 0; i > -1; i++) {
        if (fileEx == filesExToEdit[i][0]) {
            updateEditor(filesExToEdit[i][1], innerFile)
            break
        }
    }

    if (result[1] == 'error') {
        spawnPopup('Error', result[0])
    }

    document.getElementById('par').innerHTML = title;

    document.querySelector('.undoBtn').setAttribute("onClick", "back('', 'back', 'file')");
    showEditTab()
}

function saveFileHandler(e){
    result = e.target.response;
    result = JSON.parse(result);

    console.log(result)

    //spawnPopup(result[1], result[0])
}

function showEditTab() {
    divForInnerFile.style.display = 'grid'
    document.getElementById('saveFileBtn').style.display = 'block';
    divForFolder.style.display = 'none'
}

function hideEditTab() {
    divForInnerFile.style.display = 'none'
    document.getElementById('saveFileBtn').style.display = 'none';
    divForFolder.style.display = 'block'
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

function spawnPopup(infoCardText, infoPopupDescription) {
    allPopupsDiv = document.querySelector('#infoPopups');
    allInfoPopups = document.querySelectorAll('.infoPopup');
    let infoCardColor = '';

    if (infoCardText == 'Error') {
        infoCardColor = 'red'
    } else if (infoCardText == 'Info') {
        infoCardColor = 'blue'
    } else if (infoCardText == 'Success') {
        infoCardColor = 'green'
    }

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

function updateEditor(fileType, values) {
    let editor = ace.edit(mainDivForFiles)

    editor.setTheme(theme1)
    editor.session.setMode(`ace/mode/${fileType}`)
    editor.setFontSize(15)

    editor.setValue(`${values}`, 1)
}

function sendUpdateEditorToFile() {
    let valuesToSend = ace.edit(editor).getValue()
    let fileTo = document.getElementById("directoryName").innerText;

    send(`\\cgi-bin\\fileHandler\\modifyFile.py?worldNumber=${worldNumber}&values=${valuesToSend}&fileTo=${fileTo}`, serverFilesResult)
}

function send(url, result) {
    let oReq = new XMLHttpRequest();

    oReq.onload = result;

    oReq.open('GET', url);
    oReq.send();
}