import os, cgi, json

def list_files_and_folders(directory_path):
    try:
        items = os.listdir(directory_path)
        items.sort()
        folders = [item for item in items if os.path.isdir(os.path.join(directory_path, item))]
        if directory_path == 'D:\\Server - Copy Minecraft':
            folders = folders[3:]
        files = [item for item in items if os.path.isfile(os.path.join(directory_path, item))]
        if directory_path == 'D:\\Server - Copy Minecraft':
            files = files[2:]
        return folders, files
    except Exception as e:
        return [], [str(e)]

def generate_html(directory_path, folders, files):
    title = ''
    mainFolder = ''
    mainFile = ''

    title += f"<h1>{directory_path}</h1>"

    for folder in folders:
        mainFolder += f'''<div class="divFolder" onclick="run('{folder}')">{folder}</div>'''

    for file in files:
        mainFile += f'''<div class="divFile">{file}</div>'''

    html = [[title], [mainFolder], [mainFile], [directory_path]]
    
    return html

form = cgi.FieldStorage()
initial_directory = 'D:\\Server - Copy Minecraft'
current_path = form.getvalue('current_path', initial_directory)
folder = form.getvalue('folder', '')
folders, files = list_files_and_folders(current_path)
html = generate_html(current_path, folders, files)

def remove_after_last_slash(s):
    last_slash_index = s.rfind('\\')
    
    if last_slash_index != -1:
        return s[:last_slash_index]
    else:
        return s

if folder:
    if folder == '..':
        if current_path == 'D:\\Server - Copy Minecraft':
            folders, files = list_files_and_folders(current_path)
            html = generate_html(current_path, folders, files)
        else:
            current_path = remove_after_last_slash(current_path)
            folders, files = list_files_and_folders(current_path)
            html = generate_html(current_path, folders, files)
    else:
        next_path = os.path.join(current_path, folder)
        if os.path.isdir(next_path):
            current_path = next_path
            folders, files = list_files_and_folders(current_path)
            html = generate_html(current_path, folders, files)

print("Content-type: text/html\n")

#print(html)
print(json.dumps(html))