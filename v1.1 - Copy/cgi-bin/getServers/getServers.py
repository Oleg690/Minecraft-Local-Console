import sqlite3, json

print("Content-type: text/html\n")

connection = sqlite3.connect('database\\worldsData.db')
cursor = connection.cursor()

cursor.execute('SELECT worldNumber, name, version, totalPlayers FROM worlds;')
data = cursor.fetchall()

serverNumber = ''
serverName = ''
serverVersion = ''
serverPlayers = ''

array = []

for i in data:
    #i[0] = serverName
    #i[1] = serverName
    #i[2] = serverVersion
    #i[3] = serverPlayers

    array.append([i[1], i[2], i[3], i[0]])

print(json.dumps(array))