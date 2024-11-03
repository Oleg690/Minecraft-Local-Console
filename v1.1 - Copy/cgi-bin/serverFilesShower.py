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
        mainFolder += f'''<div class="divFolder" onclick="run('{folder}', 'to')">{folder}</div>'''

    for file in files:
        mainFile += f'''<div class="divFile">{file}</div>'''

    html = [[title], [mainFolder + mainFile], [directory_path]]
    
    return html

def remove_after_last_slash(s):
    last_slash_index = s.rfind('\\')
    
    if last_slash_index != -1:
        return s[:last_slash_index]
    else:
        return s
    d
def check(lastPath, worldNumber):
    if lastPath == str(os.path.join(os.getcwd(), 'minecraft_worlds', worldNumber)):
        return True
    else:
        return False


form = cgi.FieldStorage()

lastPath = form.getfirst('lastPath', '')
folderTo = form.getfirst('folder', '')
worldNumber = form.getfirst('worldNumber', '')
action = form.getfirst('action', '')

current_path = os.path.join(os.getcwd(), 'minecraft_worlds', worldNumber)
back_of_current_path = os.path.join(os.getcwd(), 'minecraft_worlds')

html = ''

print("Content-type: text/html\n")

if action == 'back':
    if remove_after_last_slash(lastPath) == back_of_current_path:
        print(json.dumps(['base', f'nothing 1']))
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