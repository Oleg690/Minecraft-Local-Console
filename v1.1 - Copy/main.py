import os, subprocess, cgi

def newEula(directory):
    file = open(directory + '\\eula.txt', 'w', encoding='utf-8')
    newEula = 'eula=true'
    file.write(newEula)
    file.close()


def runServer(batch_file):
    subprocess.run(batch_file, shell=True)

def checkEula(directory):
    file = open(directory + '\\eula.txt', 'r', encoding='utf-8')
    eulaContext = file.read()

    if eulaContext != 'eula=true':
        return False
    else:
        return True

def start_server(directory, batch_file_name):
    current_dir = os.getcwd()

    directory = current_dir + "\\" + directory
    batch_file_name = current_dir + "\\" + batch_file_name

    os.chdir(directory)
    #newEula(directory)

    if os.path.exists(directory + '\\eula.txt'):
        if checkEula(directory) == True:
            runServer(batch_file_name)
        else:
            newEula(directory)
            runServer(batch_file_name)
    else:
        runServer(batch_file_name)
        newEula(directory)
        runServer(batch_file_name)

    os.environ.clear()

world = 2
directory_path = "minecraft_worlds\\" + str(world)
batch_file_name = "start_minecraft_server.bat"
start_server(directory_path, batch_file_name)

#print(checkEula('minecraft_worlds\\2'))
#newEula('minecraft_worlds\\2')