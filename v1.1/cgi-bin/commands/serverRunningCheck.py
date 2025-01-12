from mcstatus import JavaServer
import json, psutil

print('Content-type: text/html\n')

# Method 1
def getServerStatus():
    try:
        server = JavaServer.lookup("localhost")
        server.status()
        return json.dumps(True)
    except Exception:
        return json.dumps(False)

# Method 2
def is_server_running():
    for proc in psutil.process_iter(attrs=["pid", "name", "cmdline"]):
        if "java" in proc.info["name"] and "1.21.jar" in proc.info["cmdline"]:
            return json.dumps(True)
    return json.dumps(False)

print(getServerStatus())     # --> Method 1
#print(is_server_running())  # --> Method 2