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
        alert('Please insert a world name');
        return; // Prevent further actions if input is empty
    } else if (!validNamePattern.test(worldName)) {
        alert('World name contains invalid characters. Please use only letters, numbers, spaces, hyphens, or underscores.');
        return; // Prevent further actions if input contains special characters
    } else {
        // The input is valid
        document.querySelector('.createServerBtn').disabled = true;
        send(`../cgi-bin/createServer/createWorld.py`, createWorldResult);
    }
}

// Server Files Creation Function
function createWorldResult(e) {
    let result = JSON.parse(e.target.response);

    document.getElementById('body').style.overflow = 'hidden';
    document.querySelector('.background').style.transform = `translateY(${window.scrollY}px)`
    show(document.querySelector('#background'));
    runProcentage(44);

    send(`../cgi-bin/createServer/createServerFiles.py?world=${result[1]}&version=${result[2]}`, createFilesResult);
}

// Modify server.proprierties Function
function createFilesResult(e) {
    let result = JSON.parse(e.target.response);

    runProcentage(73);

    let serverOptions = gatherServerOptions();

    let worldNumber = result[1];
    let worldVersion = result[2];

    let worldName = document.querySelector('#serverNameInput').value.trim();
    send(`../cgi-bin/createServer/modifyServerProprierties.py?array=${serverOptions}&world=${worldNumber}&name=${worldName}&version=${worldVersion}`, propriertiesResult);
}

function propriertiesResult(e) {
    let result = JSON.parse(e.target.response);

    runProcentage(100);
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

// Server Create Progress-Bar

let width = 0;

function runProcentage(procentage) {
    var element = document.querySelector('.progress-done')
    var progress = document.querySelector('#par')
    var main = setInterval(frame, 50)

    function frame() {
        if (width >= procentage) {
            clearInterval(main);
            if (width >= 100) {
                delay(1000).then(() => window.location.href = '/templates/servers.html');
            }
        } else {
            width++;
            element.style.width = width + '%';
            progress.innerText = width + '%';
        }
    }
}

function send(url, result) {
    let oReq = new XMLHttpRequest();
    oReq.onload = result;
    oReq.open('GET', url);
    oReq.send();
}

function delay(time) {
    return new Promise(resolve => setTimeout(resolve, time));
}

function hide(element) {
    var op = 1;  // initial opacity
    var timer = setInterval(function () {
        if (op <= 0.1) {
            clearInterval(timer);
            element.style.display = 'none';
        }
        element.style.opacity = op;
        element.style.filter = 'alpha(opacity=' + op * 100 + ")";
        op -= op * 0.1;
    }, 50);
}

function show(element) {
    var op = 0.1;  // initial opacity
    element.style.display = 'block';
    var timer = setInterval(function () {
        if (op >= 1) {
            clearInterval(timer);
        }
        element.style.opacity = op;
        element.style.filter = 'alpha(opacity=' + op * 100 + ")";
        op += op * 0.1;
    }, 10);
}