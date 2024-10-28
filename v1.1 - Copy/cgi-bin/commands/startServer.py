from mcstatus import JavaServer
import mcstatus
from mcrcon import MCRcon
import os, cgi, json, time, sqlite3

print('Content-type: text/html\n')

params = cgi.FieldStorage()
worldNumber = params.getfirst('number', '0')

connection = sqlite3.connect('database\\worldsData.db')
cursor = connection.cursor()

cursor.execute(f'SELECT rconPassword FROM worlds where worldNumber = {worldNumber};')
password = cursor.fetchall()

localDir = f'\\minecraft_worlds\\{worldNumber}'

batch_file_name = "start_minecraft_server.bat"

def is_server_running():
    try:
        server = JavaServer.lookup("localhost")
        server.status()
        return True
    except Exception:
        return False

if worldNumber != '0':
    curent_dir = os.getcwd()
    dir = curent_dir + '\\' + localDir
    batch_file = curent_dir + '\\' + batch_file_name

    if is_server_running():
        print(json.dumps('Server Already Running!'))
    else:
        os.chdir(dir)
        os.startfile(f"{batch_file}")
        print(json.dumps('Server Powered!'))
else:
    print(json.dumps('World number wrong!'))
