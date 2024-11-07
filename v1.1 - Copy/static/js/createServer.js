// Menu Dropdown Functionality
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

    options.forEach(option => {
        option.addEventListener('click', () => {
            selected.innerText = option.innerText;
            select.classList.remove('select-clicked');
            caret.classList.remove('caret-rotate');
            menu.classList.remove('menu-open');

            options.forEach(opt => opt.classList.remove('active'));
            option.classList.add('active');
        });
    });
});

// Server Creation Function
function createWorld() {
    let worldName = document.querySelector('#serverNameInput').value.trim();

    // Regular expression to allow only alphanumeric characters, spaces, hyphens, and underscores
    const validNamePattern = /^[a-zA-Z0-9 _-]+$/;

    if (!worldName) {
        console.log('Please insert a world name');
        return; // Prevent further actions if input is empty
    } else if (!validNamePattern.test(worldName)) {
        console.log('World name contains invalid characters. Please use only letters, numbers, spaces, hyphens, or underscores.');
        return; // Prevent further actions if input contains special characters
    } else {
        // The input is valid
        console.log('Valid world name:', worldName);

        document.querySelector('.createServerBtn').disabled = true;
        send(`../cgi-bin/createServer/createWorld.py`, createWorldResult);
    }
}

// Server Files Creation Function
function createWorldResult(e) {
    let result = JSON.parse(e.target.response);

    send(`../cgi-bin/createServer/createServerFiles.py?world=${result[1]}&version=${result[2]}`, createFilesResult);
}

// Modify server.proprierties Function
function createFilesResult(e) {
    let result = JSON.parse(e.target.response);
    
    let serverOptions = gatherServerOptions();
    
    let worldNumber = result[1];
    let worldVersion = result[2];

    let worldName = document.querySelector('#serverNameInput').value.trim();
    send(`../cgi-bin/createServer/modifyServerProprierties.py?array=${serverOptions}&world=${worldNumber}&name=${worldName}&version=${worldVersion}`, propriertiesResult);
}

function propriertiesResult(e) {
    let result = JSON.parse(e.target.response);
    window.location.href = '/templates/servers.html';
}

// Collect Server Options
function gatherServerOptions() {
    let checkboxes = document.querySelectorAll('.checkbox-input');
    let reversedServerOptionsArray = [
        [document.getElementById('difficulty').innerText, '9'],
        [document.getElementById('gamemode').innerText, '20'],
        [document.getElementById('slots').value, '32']
    ];

    checkboxes.forEach(checkbox => {
        let value = checkbox.checked ? 'true' : 'false';
        if (checkbox.value == '37') {
            reversedServerOptionsArray.unshift([value, checkbox.value]);
        } else {
            reversedServerOptionsArray.unshift([value, checkbox.value]);
        }
    });

    let spawnProtection = document.getElementById('spawn-protection').value;
    reversedServerOptionsArray.unshift([spawnProtection, '58']);

    return reversedServerOptionsArray.reverse();
}

// Send GET request function
function send(url, result) {
    let oReq = new XMLHttpRequest();
    oReq.onload = result;
    oReq.open('GET', url);
    oReq.send();
}

// Server Create Animation