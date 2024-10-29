import os, cgi, json

def list_files_and_folders(directory_path, initial_directory):
    try:
        items = os.listdir(directory_path)
        items.sort()
        folders = [item for item in items if os.path.isdir(os.path.join(directory_path, item))]

        #print(folders)

        #if directory_path == initial_directory: -> For Removing Specific Folders
        #    folders = folders[3:]
        files = [item for item in items if os.path.isfile(os.path.join(directory_path, item))]

        #print(files)

        #if directory_path == initial_directory: -> For Removing Specific Files
        #    files = files[2:]
        return folders, files
    except Exception as e:
        return [], [str(e)]

def generate_html(directory_path, folders, files):
    title = ''
    mainFolder = ''
    mainFile = ''

    title += f"<h1 id='directoryName'>{directory_path}</h1>"

    for folder in folders:
        mainFolder += f'''<div class="divFolder" onclick="run('{folder}')">{folder}</div>'''

    for file in files:
        mainFile += f'''<div class="divFile">{file}</div>'''

    #html = [[title], [mainFolder], [mainFile], [directory_path]]
    html = [[title], [mainFolder + mainFile], [directory_path]]
    
    return html

def remove_after_last_slash(s):
    last_slash_index = s.rfind('\\')
    
    if last_slash_index != -1:
        return s[:last_slash_index]
    else:
        return s

form = cgi.FieldStorage()

lastPath = form.getfirst('lastPath', '')
folder = form.getfirst('folder', '')
worldNumber = form.getfirst('worldNumber', '')
initial_directory = os.path.join(os.getcwd(), 'minecraft_worlds')
current_path = form.getfirst('current_path', initial_directory)

html = ''


print("Content-type: text/html\n")

if folder == '..':
    if lastPath == os.path.join(os.getcwd(), 'minecraft_worlds', worldNumber):
        current_path = os.path.join(current_path, worldNumber)
        folders, files = list_files_and_folders(current_path, initial_directory)
        print(json.dumps([generate_html(current_path, folders, files), 'nothing 1']))
    else:
        current_path = remove_after_last_slash(lastPath)
        folders, files = list_files_and_folders(current_path, initial_directory)
        print(json.dumps([generate_html(current_path, folders, files), f'nothing 2']))
else:
    next_path = os.path.join(current_path, worldNumber, folder)
    if os.path.isdir(next_path):
        current_path = next_path
        folders, files = list_files_and_folders(current_path, initial_directory)
        print(json.dumps([generate_html(current_path, folders, files), f'nothing 3']))
    else:
        nextLastPath = os.path.join(lastPath, folder)
        if os.path.isdir(nextLastPath):
            current_path = nextLastPath
            folders, files = list_files_and_folders(current_path, initial_directory)
            print(json.dumps([generate_html(current_path, folders, files), f'nothing 4']))
        else:
            print(json.dumps(["Path dosen't exist", f'error']))