// Define two Ace Editor themes
const theme1 = "ace/theme/tomorrow_night_eighties";
const theme2 = "ace/theme/idle_fingers";

// Retrieve URL parameter to get the world number
const searchParams = new URLSearchParams(window.location.search);
const worldNumber = searchParams.get('n');

// Select HTML elements for managing tabs and dropdown menus
const allContents = document.querySelectorAll(".content");
const dropdowns = document.querySelectorAll('.dropdown');

// Variables to store HTML elements for folder and file display areas
let lastPath = '';
let divForFolder = document.querySelector('#mainDivForFolders');
let divForInnerFile = document.querySelector('.mainDivForFilesAndBtn');

// ---------------------------------------
let clock_running = localStorage.getItem('clock_running')

// Function to load server info and files on page load
function onLoadFunc() {
    send('\\cgi-bin\\getServers\\getServers.py', serverInfoResult);

    send(`\\cgi-bin\\fileHandler\\serverFilesHandler.py?worldNumber=${worldNumber}&action=firstLoad&folderOrFile=folder`, serverFilesResult);
}

// Setup dropdown menu functionality
dropdowns.forEach(dropdown => {
    const select = dropdown.querySelector('.select');
    const caret = dropdown.querySelector('.caret');
    const menu = dropdown.querySelector('.menu');
    const options = dropdown.querySelectorAll('.menu li');
    const selected = dropdown.querySelector('.selected');

    // Toggle dropdown menu on select click
    select.addEventListener('click', () => {
        select.classList.toggle('select-clicked');
        caret.classList.toggle('caret-rotate');
        menu.classList.toggle('menu-open');
    });

    // Change selected tab and display associated content on option click
    options.forEach((option, index) => {
        option.addEventListener('click', () => {
            selected.innerText = option.innerText;

            if (selected.innerText == 'Files') {
                send(`\\cgi-bin\\fileHandler\\serverFilesHandler.py?worldNumber=${worldNumber}&action=firstLoad&folderOrFile=folder`, serverFilesResult);
            } else if (selected.innerText == 'Console') {
                fetch(`\\cgi-bin\\commands\\getServerInfo.py?worldNumber=${worldNumber}`)
                    .then(response => response.json())
                    .then(data => {
                        //console.log("Data received:", data);
                        updateConsole(data)
                    })
                    .catch(error => console.error("Error fetching data:", error));
            }

            select.classList.remove('select-clicked');
            caret.classList.remove('caret-rotate');
            menu.classList.remove('menu-open');

            options.forEach(option => option.classList.remove('active'));
            allContents.forEach(allContent => allContent.classList.remove('tabShow'));

            option.classList.add('active');
            allContents[index].classList.add('tabShow');
        });
    });
});

// Function to change status circle color and manage button states
function changeColor(prop, command) {
    const list = [2, 3];
    //const statusCircle = document.getElementById('statusCircle'); <-- statusCircle element, but not used bellow!
    const root = document.querySelector(':root');

    // Handle different color change scenarios
    if (prop == 1) {
        runServerCommand(command);
        document.getElementById('1').classList.remove('activeBtn');
        document.getElementById('1').disabled = true;
        root.style.setProperty('--statusCircleColor', '#87FF2C');

        list.forEach(n => {
            document.getElementById(`${n}`).classList.add("activeBtn");
            document.getElementById(`${n}`).disabled = false;
        });
    } else if (prop == 2 || prop == 3) {

        runServerCommand(command);
        document.getElementById('1').classList.add('activeBtn');
        document.getElementById('1').disabled = false;
        root.style.setProperty('--statusCircleColor', '#FF3535');

        list.forEach(n => {
            document.getElementById(`${n}`).classList.remove("activeBtn");
            document.getElementById(`${n}`).disabled = true;
        });
    }
}

// Send command to start the server
function runServerCommand(command) {
    if (command === 'start') {
        send(`\\cgi-bin\\commands\\startServer.py?number=${window.value[3]}`, serverCommandResult);
    }
}

// Handle server command response and spawn a popup with results
function serverCommandResult(e) {
    let result = JSON.parse(e.target.response);
    spawnPopup(result[0], result[1]);
}

// Function to send a request for folder or file actions
function run(folderTo, action, folderOrFile) {
    let lastPath = document.getElementById("directoryName").innerText;

    let url = `\\cgi-bin\\fileHandler\\serverFilesHandler.py?lastPath=${lastPath}&folderTo=${folderTo}&worldNumber=${worldNumber}&action=${action}&folderOrFile=${folderOrFile}`;
    send(url, folderOrFile === 'folder' ? serverFilesResult : handleInnerFileResult);
}

// Navigate back in the folder structure
function back(folderTo, action, folderOrFile) {
    let lastPath = document.getElementById("directoryName").innerText;
    send(`\\cgi-bin\\fileHandler\\serverFilesHandler.py?lastPath=${lastPath}&folderTo=${folderTo}&worldNumber=${worldNumber}&action=${action}&folderOrFile=${folderOrFile}`, serverFilesResult);
    document.querySelector('.undoBtn').setAttribute("onClick", "run('', 'back', 'folder')");
}

// Save file content using fetch with POST request
function saveFile() {
    let lastPath = document.getElementById("directoryName").innerText;
    let editorValues = ace.edit(mainDivForFiles).getValue();

    fetch('\\cgi-bin\\fileHandler\\insertIntoFile.py', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ lastPath, worldNumber, values: editorValues })
    })
        .then(response => response.json())
        .then(data => spawnPopup('Success', data[1]))
        .catch(error => spawnPopup('Error', error[1]));
}

// Handle server files response and update the display
function serverFilesResult(e) {
    let result = JSON.parse(e.target.response);

    if (result[1] === 'Error') {
        spawnPopup('Error', result[0]);
    } else if (result[1] !== 'Base_Directory') {
        document.querySelector('#par').innerHTML = result[0][0];
        document.querySelector('#mainDivForFolders').innerHTML = result[0][1];
    }

    hideEditTab();
}

// Manage editor behavior based on file type, and show edit tab if file is editable
function handleInnerFileResult(e) {
    let result = JSON.parse(e.target.response);
    let title = result[0][0];
    let innerFile = result[0][1];
    let fileEx = result[0][2];
    let filesExToEdit = [['properties', 'properties'], ['json', 'json'], ['txt', 'properties']];

    filesExToEdit.some(([extension, mode]) => {
        if (fileEx === extension) {
            updateEditor(mode, innerFile);
            return true;
        }
        return false;
    });

    if (result[1] === 'error') spawnPopup('Error', result[0]);

    document.getElementById('par').innerHTML = title;
    document.querySelector('.undoBtn').setAttribute("onClick", "back('', 'back', 'file')");
    showEditTab();
}

// Show the edit tab and hide the folder view
function showEditTab() {
    divForInnerFile.style.display = 'grid';
    document.getElementById('saveFileBtn').style.display = 'block';
    divForFolder.style.display = 'none';
}

// Hide the edit tab and show the folder view
function hideEditTab() {
    divForInnerFile.style.display = 'none';
    document.getElementById('saveFileBtn').style.display = 'none';
    divForFolder.style.display = 'block';
}

// Handle server information response and update server details
function serverInfoResult(e) {
    let serverData = JSON.parse(e.target.response);

    for (let server of serverData) {
        if (server.includes(worldNumber)) {
            document.getElementById("serverName").innerText = server[0];
            window.value = server;
        }
    }
}

const clockDiv = document.getElementById('timer');
const stopButton = document.getElementById('stop');
let intervalId = null;
stopButton.addEventListener('click', stopClock);

function formatTime(seconds) {
    const hrs = String(Math.floor(seconds / 3600)).padStart(2, '0');
    const mins = String(Math.floor((seconds % 3600) / 60)).padStart(2, '0');
    const secs = String(seconds % 60).padStart(2, '0');
    return `${hrs}h ${mins}m ${secs}s`;
}

function updateClock(startTime) {
    const elapsedTime = Math.floor(Date.now() / 1000 - startTime);
    clockDiv.textContent = formatTime(elapsedTime);
    console.log('Clock Updated!')
}

function startClock() {

}

function stopClock() {
    localStorage.setItem("clock_running", false)
    
}

function updateClock(start) {

}

function updateConsole(data) {
    serverStatus = data["serverStatus"]
    serverLogs = data["serverLogs"]
    serverUpTime = data["serverUpTime"]

    //console.log(serverUpTime['start_time'])

    document.querySelector('#textareaConsole').value = serverLogs

    if (document.getElementById('1').disabled == true || serverIsRunning) {
        if (localStorage.getItem("clock_running") == true) {
            intervalId = setInterval(() => updateClock(startTime), 1000);
        } else {
            startClock()
        }
    } else {
        stopClock()
    }
}

// --------------------------------------------------------------------

function serverIsRunning() {
    fetch('\\cgi-bin\\commands\\serverRunningCheck.py')
        .then(response => response.json())
        .then(data => {
            if (data == true) {
                return true
            } else {
                return false
            }
        })
        .catch(error => console.error("Error fetching data:", error));
}

// --------------------------------------------------------------------

// Function to spawn a popup message with given text and description
function spawnPopup(infoCardText, infoPopupDescription) {
    let allPopupsDiv = document.querySelector('#infoPopups');
    let allInfoPopups = document.querySelectorAll('.infoPopup');
    let infoCardColor = (infoCardText === 'Error') ? 'red' : (infoCardText === 'Info') ? 'blue' : 'green';

    allPopupsDiv.innerHTML += `
        <div class="infoPopup" id="${allInfoPopups.length + 1}0">
            <div class="color ${infoCardColor}">
                <div class="infoCardText">${infoCardText}</div>
                <div class="x-icon">
                    <ion-icon name="close-outline" onclick="deleteDiv(${allInfoPopups.length + 1})"></ion-icon>
                </div>
            </div>
            <p class="infoPopupDescription">${infoPopupDescription}</p>
        </div>`;
}

// Delete popup by fading it out
function deleteDiv(e) {
    $(`#${e}0`).delay(0).fadeOut(500);
}

// Update the Ace editor with specified file type and values
function updateEditor(fileType, values) {
    let editor = ace.edit(mainDivForFiles);
    editor.setTheme(theme1);
    editor.session.setMode(`ace/mode/${fileType}`);
    editor.setFontSize(15);
    editor.setValue(`${values}`, 1);
}

// Send updated editor content to save in file
function sendUpdateEditorToFile() {
    let valuesToSend = ace.edit(editor).getValue();
    let fileTo = document.getElementById("directoryName").innerText;
    send(`\\cgi-bin\\fileHandler\\modifyFile.py?worldNumber=${worldNumber}&values=${valuesToSend}&fileTo=${fileTo}`, serverFilesResult);
}

// Wrapper function to handle sending HTTP requests
function send(url, result) {
    let oReq = new XMLHttpRequest();
    oReq.onload = result;
    oReq.open('GET', url);
    oReq.send();
}
