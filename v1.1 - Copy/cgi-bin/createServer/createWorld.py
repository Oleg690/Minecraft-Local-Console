import os, json, shutil, random

print("Content-type: text/html\n")

worldVersion = '1.21'

source_file_path = f"versions\\{worldVersion}.jar"

directory_path_to_all_worlds = "minecraft_worlds"

worldNumber = ''

while True:
    for i in range(1, 10):
        worldNumber += str(random.randint(1, 9))
    
    destination_folder = "minecraft_worlds\\" + str(worldNumber)

    if os.path.exists(destination_folder):
        worldNumber = ''
        continue
    else:
        break

os.makedirs(destination_folder, exist_ok=True)

shutil.copy(source_file_path, destination_folder)

print(json.dumps(['World created succeasfully!', worldNumber, worldVersion]))