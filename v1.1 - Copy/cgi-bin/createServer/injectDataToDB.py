def insertData(worldNumber, name, version, totalPlayers, rconPassword):
    import sqlite3

    connection = sqlite3.connect('D:\\Mineraft Server\\v1.1 - Copy\\database\\worldsData.db')
    cursor = connection.cursor()

    sql = f"insert into worlds (worldNumber, name, version, totalPlayers, rconPassword) values('{worldNumber}', '{name}', '{version}', '{totalPlayers}', '{rconPassword}');"

    cursor.execute(sql)
    connection.commit()