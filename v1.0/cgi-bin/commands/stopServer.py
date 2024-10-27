from mcrcon import MCRcon
import time


mc = MCRcon('192.168.100.106', '123456789', port=25575)
mc.connect()

mc.command("say Server is stopping in...")

timer = 10

time.sleep(1)

for i in range(1,11):
    mc.command(f"say {timer}")
    timer -= 1
    time.sleep(1)

mc.command("save-all")

p = mc.command("stop")