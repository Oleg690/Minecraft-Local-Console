from mcstatus import JavaServer
import os, cgi, json, sqlite3, psutil, time

def setup_cgi_environment():
    print('Content-type: text/html\n')
    return cgi.FieldStorage()

def get_world_number(params):
    return params.getfirst('worldNumber', '0')

def get_rcon_password(world_number):
    # Cache or store the password if possible, to avoid frequent database queries
    database_path = 'database\\worldsData.db'
    connection = sqlite3.connect(database_path, timeout=1)  # Reduce timeout
    cursor = connection.cursor()
    query = "SELECT rconPassword FROM worlds WHERE worldNumber = ?;"
    cursor.execute(query, (world_number,))
    result = cursor.fetchone()
    connection.close()
    return result[0] if result else None

def is_server_running():
    # Directly check if the Minecraft process is running (without mcstatus)
    for proc in psutil.process_iter(attrs=["pid", "name", "cmdline"]):
        if "java" in proc.info["name"] and "1.21.jar" in proc.info["cmdline"]:
            return True
    return False

def start_server(world_number):
    local_dir = f'\\minecraft_worlds\\{world_number}'
    batch_file_name = "start_minecraft_server.bat"
    current_dir = os.getcwd()
    dir_path = os.path.join(current_dir, local_dir)
    batch_file_path = os.path.join(current_dir, batch_file_name)

    try:
        os.chdir(dir_path)
        os.startfile(batch_file_path)
        return ['Success', 'Server Powered!']
    except Exception as e:
        return ['Error', f"Failed to start server: {str(e)}"]
    finally:
        os.chdir(current_dir)

def getServerStatus():
    # Skip mcstatus; directly check Minecraft process
    try:
        server = JavaServer.lookup("localhost")
        server.status()
        return "Online"
    except Exception:
        return "Offline"

def getServerLogs(directory):
    try:
        file = open(directory, 'r', encoding='utf-8')
        data = file.read()
        data = data.replace('\n', '&#13;')
        file.close()
        return data
    except Exception as e:
        return f"Error reading logs: {str(e)}"

#def get_minecraft_process(jar_name="1.21.jar"):
#    for proc in psutil.process_iter(attrs=["pid", "name", "cmdline", "create_time"]):
#        try:
#            if "java" in proc.info["name"] and jar_name in proc.info["cmdline"]:
#                return proc
#        except (psutil.NoSuchProcess, psutil.AccessDenied):
#            continue
#    return None
#
#def format_uptime(delta):
#    total_seconds = int(delta.total_seconds())
#    hours, remainder = divmod(total_seconds, 3600)
#    minutes, seconds = divmod(remainder, 60)
#    return f"{hours}h {minutes}m {seconds}s"
#
#def getUpTime(minecraft_process):
#    if minecraft_process:
#        server_start_time = datetime.datetime.fromtimestamp(minecraft_process.info['create_time'])
#        uptime = datetime.datetime.now() - server_start_time
#        return format_uptime(uptime)
#    return "0h 0m 0s"

def main():
    params = setup_cgi_environment()
    worldNumber = get_world_number(params)

    data = {}

    #data['serverStatus'] = getServerStatus()

    logDirectory = os.path.join(os.getcwd(), "minecraft_worlds", f"{worldNumber}", "logs", "latest.log")
    data['serverLogs'] = getServerLogs(logDirectory)

    #jar_name = "1.21.jar"
    ##minecraft_process = get_minecraft_process(jar_name)
    ##data['serverUpTime'] = getUpTime(minecraft_process)
    #data['serverUpTime'] = start_clock()

    return data

result = main()
print(json.dumps(result))