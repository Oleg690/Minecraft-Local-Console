import sqlite3

connection = sqlite3.connect('database\\worldsData.db')
cursor = connection.cursor()

tableName = 'worlds'
insertOneDefaultSQLVerificator = False
insertOneDefaultSQL = f'''insert into {tableName} (worldNumber, name, version, totalPlayers, rconPassword) values('123456789', 'Minecraft SMP', '1.21', '20', '123456789123456789');'''

def createTable(sql):
    cursor.execute(sql)
    connection.commit()

createTableSQL = f'''CREATE TABLE IF NOT EXISTS {tableName}(
    id integer primary key autoincrement,
    worldNumber text,
    name text,
    version text,
    totalPlayers text,
    rconPassword text
    )'''

createTable(createTableSQL)
print('Database created succeasfully!')

if insertOneDefaultSQLVerificator == True:
    createTable(insertOneDefaultSQL)
    print('Default data inserted succeasfully!')

connection.close()