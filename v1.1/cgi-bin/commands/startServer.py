from mcstatus import JavaServer
import os, cgi, json, sqlite3

def setup_cgi_environment():
    print('Content-type: text/html\n')
    return cgi.FieldStorage()

def get_world_number(params):
    return params.getfirst('number', '0')

def get_rcon_password(world_number):
    database_path = 'database\\worldsData.db'
    connection = sqlite3.connect(database_path)
    cursor = connection.cursor()

    query = "SELECT rconPassword FROM worlds WHERE worldNumber = ?;"
    cursor.execute(query, (world_number,))
    result = cursor.fetchone()
    connection.close()

    return result[0] if result else None

def is_server_running():
    try:
        server = JavaServer.lookup("0.0.0.0")
        server.status()
        return True
    except Exception:
        return False

def start_server(world_number):
    local_dir = f'\\minecraft_worlds\\{world_number}'
    batch_file_name = "start_minecraft_server.bat"
    current_dir = os.getcwd()

    dir_path = current_dir + '\\' + local_dir
    batch_file_path = os.path.join(current_dir, batch_file_name)

    try:
        os.chdir(dir_path)
        os.startfile(batch_file_path)
        return ['Success', 'Server Powered!']
    except Exception as e:
        return ['Error', f"Failed to start server: {str(e)}"]
    finally:
        os.chdir(current_dir)

def main():
    params = setup_cgi_environment()
    world_number = get_world_number(params)

    if world_number == '0':
        print(json.dumps(['Error', 'World number wrong!']))
        return

    if is_server_running():
        print(json.dumps(['Info', 'Server Already Running!']))
    else:
        result = start_server(world_number)
        print(json.dumps(result))

main()