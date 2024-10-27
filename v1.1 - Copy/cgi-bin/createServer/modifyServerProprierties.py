import os, cgi, json, string, random
import injectDataToDB

print("Content-type: text/html\n")

def modify_list(array):
    splitedArray = array.split(',')

    for i in splitedArray:
        lineAndActionTemp.append([i])

    modified_list = []
    temp_pair = []
    
    for item in lineAndActionTemp:
        if len(temp_pair) < 2:
            temp_pair.append(item[0])
        if len(temp_pair) == 2:
            modified_list.append(temp_pair)
            temp_pair = []
    
    if temp_pair:
        modified_list.append(temp_pair)
    
    return modified_list

def findSettings(directory, lineNumber, action):
    with open(f'{directory}' + '\\server.properties', 'r') as file:
        numberOfLines = file.readlines()

        lineTxt = getSettings(numberOfLines[lineNumber - 1])
        newContent = lineTxt + action + '\n'
        numberOfLines[lineNumber - 1] = newContent

        with open(f'{directory}' + '\\server.properties', 'w') as file:
            file.writelines(numberOfLines)

def getSettings(lines):
    line = [x for x in lines]

    lineTxt = ''
    lineTxtList = []
    lineValue = ''
    lineValueList = []
    counter = 1

    for i in line[0:-1]:
        if i != '=':
            counter += 1
            lineTxtList.append(i)
        else:
            lineTxtList.append(i)
            lineValueList = line[int(counter):-1]
            break

    for i in lineTxtList:
        lineTxt += i
    for i in lineValueList:
        lineValue += i

    return lineTxt

def generatePassword(length):
    characters = string.ascii_letters + string.digits
    password = ''.join(random.choice(characters) for _ in range(length))
    return str(password)

rconPassword = generatePassword(18)

form = cgi.FieldStorage()
array = form.getvalue('array', [])
worldNumber = form.getvalue('world', '0')
name = form.getvalue('name', '')
version = form.getvalue('version', '')

lineAndActionTemp = []
directory_path_to_all_worlds = "minecraft_worlds"

directory_path_to_world = "minecraft_worlds\\" + str(worldNumber)

lineAndAction = modify_list(array)

lineAndAction.append([rconPassword, '44'])

if worldNumber != '0':
    for i in lineAndAction:
        if int(i[1]) == 32:
            slots = i[0]
            injectDataToDB.insertData(worldNumber, name, version, slots, rconPassword)
            findSettings(directory_path_to_world, int(i[1]), i[0])
        else:
            findSettings(directory_path_to_world, int(i[1]), i[0])

    print(json.dumps('server.proprierties modified succeasfuly!'))
else:
    print(json.dumps('world is 0'))