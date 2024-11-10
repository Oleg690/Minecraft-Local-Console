import os, cgi, json, time

print('Content-type: text/html\n')

os.startfile("1start_minecraft_server.bat")

time.sleep(5)

print(json.dumps('success'))