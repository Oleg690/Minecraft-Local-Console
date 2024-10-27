import cgi, sqlite3, json

print("Content-type: text/html\n")

connection = sqlite3.connect('database\\worldsData.db')
cursor = connection.cursor()

cursor.execute('SELECT name, version, totalPlayers FROM worlds;')
data = cursor.fetchall()

serverName = ''
serverVersion = ''
serverPlayers = ''

array = []

for i in data:
    #i[0] = serverName
    #i[1] = serverVersion
    #i[2] = serverPlayers

    array.append([i[0], i[1], i[2]])

print(json.dumps(array))