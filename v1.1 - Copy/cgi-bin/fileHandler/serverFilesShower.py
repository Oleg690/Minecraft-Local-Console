import os, cgi, json

def list_files_and_folders(directory_path,):
    try:
        items = os.listdir(directory_path)
        items.sort()
        folders = [item for item in items if os.path.isdir(os.path.join(directory_path, item))]

        #print(folders)

        files = [item for item in items if os.path.isfile(os.path.join(directory_path, item))]

        #print(files)

        return folders, files
    except Exception as e:
        return [], [str(e)]

def generate_html(directory_path, folders, files):
    title = ''
    mainFolder = ''
    mainFile = ''

    title += f"<h1 id='directoryName'>{directory_path}</h1>"

    for folder in folders:
        mainFolder += f'''<div class="divFolder" onclick="run('{folder}', 'to', 'folder')">{folder}</div>'''

    for file in files:
        if getExtensionName(file) in filesExToEdit:
            mainFile += f'''<div class="divFile" id="filesToEdit" onclick="run('{file}', 'to', 'file')">{file}</div>'''
        else:
            mainFile += f'''<div class="divFile">{file}</div>'''

    html = [[title], [mainFolder + mainFile], [directory_path]]
    
    return html

def generate_inner_file_text(fileDir):
    file = open(fileDir, 'r', encoding='utf-8')

    fileTxt = file.read()

    fileTxt = fileTxt.replace(' ', '&nbsp')
    fileTxt = fileTxt.replace('\n', '<br>')

    file.close()

    return fileTxt

def getExtensionName(file):
    index_ex = file.rfind('.')
    file_ex = file[index_ex + 1:]

    return file_ex

def remove_after_last_slash(s):
    last_slash_index = s.rfind('\\')
    
    if last_slash_index != -1:
        return s[:last_slash_index]
    else:
        return s

form = cgi.FieldStorage()

filesExToEdit = ['properties', 'json', 'txt']

lastPath = form.getfirst('lastPath', '')
folderTo = form.getfirst('folderTo', '')
worldNumber = form.getfirst('worldNumber', '')
action = form.getfirst('action', '')
folderOrFile = form.getfirst('folderOrFile', '')

current_path = os.path.join(os.getcwd(), 'minecraft_worlds', worldNumber)
back_of_current_path = os.path.join(os.getcwd(), 'minecraft_worlds')

html = ''

print("Content-type: text/html\n")

if folderOrFile == 'folder':
    if action == 'back':
        if remove_after_last_slash(lastPath) == back_of_current_path:
            print(json.dumps(['base', f'']))
        else:
            current_path = remove_after_last_slash(lastPath)
            folders, files = list_files_and_folders(current_path)
            print(json.dumps([generate_html(current_path, folders, files), f'']))
    elif action == 'to':
        next_path = os.path.join(current_path, folderTo)
        next_path = next_path[0:-1]
        if os.path.isdir(next_path):
            current_path = next_path
            folders, files = list_files_and_folders(current_path)
            print(json.dumps([generate_html(current_path, folders, files), f'']))
        else:
            nextLastPath = os.path.join(lastPath, folderTo)
            if os.path.isdir(nextLastPath):
                current_path = nextLastPath
                folders, files = list_files_and_folders(current_path)
                print(json.dumps([generate_html(current_path, folders, files), f'']))
            else:
                print(json.dumps(["Path dosen't exist, error on code side", f'error']))
    else:
        print(json.dumps(["Action is wrong, error on code side", f'error']))
elif folderOrFile == 'file':
    if action != 'back':
        newPath = os.path.join(current_path, folderTo)
        title = f"<h1 id='directoryName'>{newPath}</h1>"
        print(json.dumps([[title, f"{generate_inner_file_text(newPath)}", getExtensionName(newPath)], f'nothing 5']))
    else:
        current_path = remove_after_last_slash(lastPath)
        if os.path.isdir(current_path):
            folders, files = list_files_and_folders(current_path)
            print(json.dumps([generate_html(current_path, folders, files), f'nothing 6']))
        else:
            print(json.dumps([f"wrong directory, {lastPath}", f'error']))