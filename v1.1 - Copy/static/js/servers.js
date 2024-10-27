function getServers() {
    send('\\cgi-bin\\getServers\\getServers.py', getServersResult)
}

function getServersResult(e) {
    let result = e.target.response;
    result = JSON.parse(result);

    let html = '';

    for (let i = 0; i < result.length; i++) {
        html += `<div class='serverCard' onclick='openServer(${result[i][3]})'>
                    <div class='serverCardImage'>
                        <img src='/static/img/serverCardBackground.png' alt='error'>
                    </div>
                    <div class='serverCardName'>
                        <p>${result[i][0]}</p>
                    </div>
                    <div class="serverCardDetailsAll">
                        <div class="serverCardDetails">
                            <p id="serverCardDetailsTxtVersion">Version: ${result[i][1]}</p>
                            <p id="serverCardDetailsTxtPlayers">Total Players: ${result[i][2]}</p>
                        </div>
                        <div class="serverCardOnOrOff">
                            <p id="serverCardOnOrOffTxt" class="serverCardOnOrOffTxt offTxt">off</p>
                        </div>
                    </div>
                </div>`;
    }

    html += `   <div class="createCard">
                    <div class="createCardText">
                        <p>Create Server</p>
                    </div>
                    <div class="createCardButton">
                        <button onclick="window.location.href='createServer.html'">
                            <ion-icon name="add-outline"></ion-icon>
                        </button>
                    </div>
                </div>`

    document.getElementById('serverCards').innerHTML += html;
}

function openServer(serverNumber){
    console.log(serverNumber)
}

function send(url, result) {
    let oReq = new XMLHttpRequest();

    oReq.onload = result;

    oReq.open('GET', url);
    oReq.send();
}