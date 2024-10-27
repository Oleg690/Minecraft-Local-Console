import sqlite3

connection = sqlite3.connect('D:\\Mineraft Server\\v1.1 - Copy\\database\\worldsData.db')
cursor = connection.cursor()

def createTable(sql):
    cursor.execute(sql)
    connection.commit()

createTableSQL = '''CREATE TABLE IF NOT EXISTS worlds(
    id integer primary key autoincrement,
    worldNumber text,
    name text,
    version text,
    totalPlayers text,
    rconPassword text
    )'''

insertOneDefaultSQLVerificator = False
insertOneDefaultSQL = '''insert into worlds (worldNumber, name, version, totalPlayers, rconPassword) values('123456789', 'Minecraft SMP', '1.21', '20', '123456789123456789');'''

createTable(createTableSQL)
print('Database created succeasfully!')

if insertOneDefaultSQLVerificator == True:
    createTable(insertOneDefaultSQL)
    print('Default data inserted succeasfully!')



connection.close()