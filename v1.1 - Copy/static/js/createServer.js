// Scripts for menu's to work in the server options

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

            options.forEach(option => {
                option.classList.remove('active');
            })
            option.classList.add('active');
        })
    })
});

// -------------- Functions for server creating --------------

function createWorld() {
    document.querySelector('.createServerBtn').disabled = true;
    send(`../cgi-bin/createServer/createWorld.py`, createWorldResult)
}

function createWorldResult(e) {
    let result = e.target.response;
    result = JSON.parse(result)

    //console.log("createWorldResult: ", result[0])

    //console.log("World Number: ", result[1])

    send(`../cgi-bin/createServer/createServerFiles.py?world=${result[1]}&version=${result[2]}`, createFilesResult)
}

function createFilesResult(e) {
    let result = e.target.response;
    result = JSON.parse(result)

    //console.log("Result: ", result[0])

    //console.log("World Number: ", result[1])
    //console.log("World Version: ", result[2])

    // This if to create the array with all the user settings

    let slots = document.getElementById('slots').value; // input value
    let gamemode = document.getElementById('gamemode').innerText; // span
    let difficulty = document.getElementById('difficulty').innerText; // span
    let whitelist = document.getElementById('whitelist-input'); // input checked
    let cracked = document.getElementById('cracked-input'); // input checked
    let pvp = document.getElementById('pvp-input'); // input checked
    let commandblocks = document.getElementById('commandblocks-input'); // input checked
    let fly = document.getElementById('fly-input'); // input checked
    let animals = document.getElementById('animals-input'); // input checked
    let monster = document.getElementById('monster-input'); // input checked
    let villagers = document.getElementById('villagers-input'); // input checked
    let nether = document.getElementById('nether-input'); // input checked
    let force_gamemode = document.getElementById('force-gamemode-input'); // input checked

    let checkboxes = document.querySelectorAll('.checkbox-input');

    let reversedServerOptionsArray = [[difficulty, '9'], [gamemode, '20'], [slots, '32']]

    checkboxes.forEach(checkbox => {
        if (checkbox.value == '37') {
            if (checkbox.checked == 1) {
                reversedServerOptionsArray.unshift(['false', checkbox.value])
            } else {
                reversedServerOptionsArray.unshift(['true', checkbox.value])
            }
        } else {
            if (checkbox.checked == 1) {
                reversedServerOptionsArray.unshift(['true', checkbox.value])
            } else {
                reversedServerOptionsArray.unshift(['false', checkbox.value])
            }
        }

    })

    let spawn_protection = document.getElementById('spawn-protection').value; // input value

    reversedServerOptionsArray.unshift([spawn_protection, '58']);

    let serverOptionsArray = reversedServerOptionsArray.reverse()

    let worldNumber = result[1];
    let worldName = document.querySelector('#serverNameInput').value;
    let worldVersion = result[2];

    // ------------------------------------------------------------------------------

    send(`../cgi-bin/createServer/modifyServerProprierties.py?array=${serverOptionsArray}&world=${worldNumber}&name=${worldName}&version=${worldVersion}`, propriertiesResult)
}

function propriertiesResult(e) {
    let result = e.target.response;
    result = JSON.parse(result)

    //console.log("propriertiesResult: ", result)

    window.location.href = '/templates/servers.html'
}

// Function to send GET requests

function send(url, result) {
    let oReq = new XMLHttpRequest();

    oReq.onload = result;

    oReq.open('GET', url);
    oReq.send();
}