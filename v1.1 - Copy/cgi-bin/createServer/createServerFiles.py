import os, subprocess, json, cgi

print("Content-type: text/html\n")

def newEula(directory):
    file = open(directory + '\\eula.txt', 'w', encoding='utf-8')
    newEula = 'eula=true'
    file.write(newEula)
    file.close()


def runServer(batch_file):
    subprocess.run(batch_file, shell=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

def checkEula(directory):
    try:
        with open(os.path.join(directory, 'eula.txt'), 'r', encoding='utf-8') as file:
            return file.read().strip() == 'eula=true'
    except FileNotFoundError:
        return False

def start_server(directory, batch_file_name):
    current_dir = os.getcwd()

    directory = os.path.join(current_dir, directory)
    batch_file_name = os.path.join(current_dir, batch_file_name)

    os.chdir(directory)

    if os.path.exists(directory + '\\eula.txt'):
        if checkEula(directory) == False:
            newEula(directory)
    else:
        runServer(batch_file_name)
        newEula(directory)

    os.environ.clear()

form = cgi.FieldStorage()
world = form.getvalue('world', '0')

worldVersion = form.getvalue('version', '0')

directory_path_to_all_worlds = "minecraft_worlds"
directory_path = "minecraft_worlds\\" + str(world)
batch_file_name = "start_minecraft_server.bat"

start_server(directory_path, batch_file_name)
#print(start_server(directory_path, batch_file_name))

print(json.dumps(['World files created!', world, worldVersion]))