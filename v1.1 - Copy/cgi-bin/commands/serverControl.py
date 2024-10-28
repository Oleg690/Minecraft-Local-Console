from mcrcon import MCRcon
import time, cgi, json, sqlite3

print('Content-type: text/html\n')

params = cgi.FieldStorage()
action = params.getfirst('action', '')
worldNumber = params.getfirst('number', '')

connection = sqlite3.connect('database\\worldsData.db')
cursor = connection.cursor()

cursor.execute(f'SELECT rconPassword FROM worlds where worldNumber = {worldNumber};')
password = cursor.fetchall()

mc = MCRcon('127.0.0.1', f'{password}', port=25575) #        ↓
#                                                       For testing
#mc = MCRcon('0.0.0.0', f'{password}', port=25575)           ↑

#mc = MCRcon('192.168.100.106', f'{password}', port=25575) → working
mc.connect()

def runCommand():
    if action == 'restarting':
        mc.command("say Server will restart in...")
        timer = 5
        timer2 = timer
        time.sleep(1)
        for i in range(1, timer2 + 1):
            mc.command(f"say {timer}")
            timer -= 1
            time.sleep(1)
        mc.command("save-all")
        mc.command("stop")
        print(json.dumps(['success', 'restart']))
    elif action == 'stop':
        mc.command("say Server will stopp in...")
        timer = 5
        timer2 = timer
        time.sleep(1)
        for i in range(1, timer2 + 1):
            mc.command(f"say {timer}")
            timer -= 1
            time.sleep(1)
        mc.command("save-all")
        mc.command("stop")
        print(json.dumps(['success', 'none']))
    else:
        result = mc.command(f"{action}")
        mc.command(f"say { result}")

if action != '':
    runCommand()
else:
    mc.command(f"say Enter an existing command!")