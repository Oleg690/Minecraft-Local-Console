from mcrcon import MCRcon
import time, cgi, json

print('Content-type: text/html\n')

params = cgi.FieldStorage()
action = params.getfirst('action', '')

mc = MCRcon('192.168.100.106', '123456789', port=25575)
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