from mcrcon import MCRcon
import time, cgi, json, sqlite3
from startServer import start_server

def connectToServer(worldNumber):
    global mc
    connection = sqlite3.connect('database\\worldsData.db')
    cursor = connection.cursor()

    cursor.execute(f'SELECT rconPassword FROM worlds where worldNumber = {worldNumber};')
    password = cursor.fetchall()

    mc = MCRcon('0.0.0.0', f'{password}', port=25575)
    mc.connect()

def countdown(awaitTime, action):
    print(f"say Server will {action} in...")
    timerTemp = awaitTime
    for i in range(1, awaitTime + 1):
        print(f"say {timerTemp}")
        timerTemp -= 1
        time.sleep(1)

def runCommand(action, worldNumber):
    connectToServer(worldNumber)

    if action != 'stop' or action != 'restart':
        result = mc.command(f"/{action}")
        mc.command(f"say { result}")
    else:
        if action == 'stop':
            countdown(5, action)
            mc.command('save-all')
            mc.command('stop')
        elif action == 'restart':
            countdown(5, action)
            mc.command('save-all')
            mc.command('stop')
            start_server(worldNumber)
        else:
            print(f"Enter a valid action")