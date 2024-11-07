# --------------------------------------
# Import necessary libraries
import os, cgi, json

# --------------------------------------
# Retrieve form data sent via CGI
form = cgi.FieldStorage()

# File extensions to edit (can be modified as needed)
filesExToEdit = ['properties', 'json', 'txt']

# Variables to capture form parameters
lastPath = form.getfirst('lastPath', '')  # Previous path
folderTo = form.getfirst('folderTo', '')  # Folder or file to navigate to
worldNumber = form.getfirst('worldNumber', '')  # World identifier
action = form.getfirst('action', '')  # Action to take (e.g., 'back', 'to')
folderOrFile = form.getfirst('folderOrFile', '')  # Type (folder or file)
values = form.getfirst('values', 0)  # Additional data if needed

# Define current path based on provided worldNumber
current_path = os.path.join(os.getcwd(), 'minecraft_worlds', worldNumber)
back_of_current_path = os.path.join(os.getcwd(), 'minecraft_worlds')

# Print HTTP header for HTML content
print("Content-type: text/html\n")

# --------------------------------------
# Function to list folders and files in a given directory
def list_files_and_folders(directory_path):
    try:
        if not os.path.isdir(directory_path):
            raise FileNotFoundError(f"Directory '{directory_path}' not found.")
        
        # Retrieve and sort all items in the directory
        items = os.listdir(directory_path)
        items.sort()
        
        # Separate items into folders and files
        folders = [item for item in items if os.path.isdir(os.path.join(directory_path, item))]
        files = [item for item in items if os.path.isfile(os.path.join(directory_path, item))]
        
        return folders, files
    except Exception as e:
        # Return empty folder list and error message if exception occurs
        return [], [str(e)]

# Function to generate HTML content for a directory's files and folders
def generate_html(directory_path, folders, files):
    # Header for the directory name
    title = f"<h1 id='directoryName'>{directory_path}</h1>"
    mainFolder = ''
    mainFile = ''

    # HTML for each folder (clickable to navigate into)
    for folder in folders:
        mainFolder += f'''<div class="divFolder" onclick="run('{folder}', 'to', 'folder')">{folder}</div>'''

    # HTML for each file, with special handling for editable files
    for file in files:
        if getExtensionName(file) in filesExToEdit:
            mainFile += f'''<div class="divFile" id="filesToEdit" onclick="run('{file}', 'to', 'file')">{file}</div>'''
        else:
            mainFile += f'''<div class="divFile">{file}</div>'''

    # Combine all HTML sections into a structured list
    html = [[title], [mainFolder + mainFile], [directory_path]]
    return html

# Function to retrieve text content of a file (for files that can be read)
def generate_inner_file_text(fileDir):
    try:
        if not os.path.isfile(fileDir):
            raise FileNotFoundError(f"File '{fileDir}' not found.")
        
        # Open and read the file content
        with open(fileDir, 'r', encoding='utf-8') as file:
            fileTxt = file.read()
        return fileTxt
    except Exception as e:
        # Return the error message if file cannot be read
        return str(e)

# Function to extract the extension from a filename
def getExtensionName(file):
    index_ex = file.rfind('.')
    if index_ex != -1:
        return file[index_ex + 1:]  # Returns the file extension without '.'
    return ''

# Function to remove the last folder in a directory path (for "back" action)
def remove_after_last_slash(s):
    last_slash_index = s.rfind('\\')
    return s[:last_slash_index] if last_slash_index != -1 else s

# --------------------------------------
# Main logic for handling actions and generating JSON response

try:
    if folderOrFile == 'folder':
        if action == 'back':
            # Check if the base directory is reached, respond accordingly
            if remove_after_last_slash(lastPath) == back_of_current_path:
                print(json.dumps(['Base directory reached', 'Base_Directory']))
            else:
                # Navigate one level up in directory hierarchy
                current_path = remove_after_last_slash(lastPath)
                folders, files = list_files_and_folders(current_path)
                print(json.dumps([generate_html(current_path, folders, files), 'Success']))
        elif action == 'to':
            # Navigate into specified folder if it exists
            next_path = os.path.join(lastPath, folderTo)
            if os.path.isdir(next_path):
                current_path = next_path
                folders, files = list_files_and_folders(current_path)
                print(json.dumps([generate_html(current_path, folders, files), 'Success']))
            else:
                # Return error if the specified folder does not exist
                print(json.dumps(["Path doesn't exist or is not a directory", 'Error']))
        elif action == 'firstLoad':
            # Load initial directory content
            folders, files = list_files_and_folders(current_path)
            print(json.dumps([generate_html(current_path, folders, files), 'Success']))
        else:
            # Handle invalid actions with error message
            print(json.dumps(["Invalid action specified", 'Error']))

    elif folderOrFile == 'file':
        if action == 'back':
            # Move back to the parent directory
            current_path = remove_after_last_slash(lastPath)
            if os.path.isdir(current_path):
                folders, files = list_files_and_folders(current_path)
                print(json.dumps([generate_html(current_path, folders, files), 'Success']))
            else:
                print(json.dumps(["Directory not found when going back", 'Error']))
        else:
            # Open and read specified file content
            newPath = os.path.join(current_path, folderTo)
            if os.path.isfile(newPath):
                title = f"<h1 id='directoryName'>{newPath}</h1>"
                file_content = generate_inner_file_text(newPath)
                print(json.dumps([[title, file_content, getExtensionName(newPath)], 'Success']))
            else:
                print(json.dumps(["File not found or invalid path specified", 'Error']))
    else:
        # Invalid type for folderOrFile
        print(json.dumps(["Invalid folder or file action specified", 'Error']))

# Catch and output any unhandled exceptions
except Exception as e:
    print(json.dumps([str(e), 'Error']))
