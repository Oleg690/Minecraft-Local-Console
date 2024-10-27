import os, subprocess

def newEula(directory):
    file = open(directory + '\\eula.txt', 'r+', encoding='utf-8')
    eulaContext = file.read()

    newEula = eulaContext[:-6] + 'true\n'

    file.seek(0)
    file.write(newEula)

    file.close()

def runServer(batch_file):
    subprocess.run(batch_file, shell=True)

def checkEula(directory):
    file = open(directory + '\\eula.txt', 'r', encoding='utf-8')
    eulaContext = file.read()

    if eulaContext[-3] == 's':
        return False
    elif eulaContext[-3] == 'u':
        return True

def start_server(directory, batch_file_name):
    current_dir = os.getcwd()

    directory = current_dir + "\\" + directory
    batch_file_name = current_dir + "\\" + batch_file_name

    os.chdir(directory)

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
directory_path = "data\\" + str(world)
batch_file_name = "start_minecraft_server.bat"
start_server(directory_path, batch_file_name)